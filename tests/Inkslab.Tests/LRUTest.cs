using Inkslab.Collections;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Inkslab.Tests
{
    /// <summary>
    /// <see cref="Lru{TKey, TValue}"/> 算法测试。
    /// </summary>
    public class LRUTests
    {
        private class DisposableProbe : IDisposable
        {
            private int _disposed;

            public int DisposeCount => _disposed;

            public void Dispose() => Interlocked.Increment(ref _disposed);
        }

        #region 边界测试

        /// <summary>
        /// 容量为 0 时应抛出参数越界异常。
        /// </summary>
        [Fact]
        public void CapacityZero_ShouldThrow_ForSingleValueLru()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new Lru<int>(0));
        }

        /// <summary>
        /// 负数容量应抛出参数越界异常。
        /// </summary>
        [Fact]
        public void CapacityNegative_ShouldThrow()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new Lru<int>(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new Lru<int, int>(-1, x => x));
        }

        /// <summary>
        /// factory 为 null 时应抛出 ArgumentNullException。
        /// </summary>
        [Fact]
        public void FactoryNull_ShouldThrow()
        {
            Assert.Throws<ArgumentNullException>(() => new Lru<int, int>(10, null));
        }

        /// <summary>
        /// 容量为 1 的极端边界，频繁淘汰。
        /// </summary>
        [Fact]
        public void Capacity1_ShouldEvictOnEveryNewKey()
        {
            var lru = new Lru<string, int>(1, s => s.GetHashCode());

            var v1 = lru.Get("A");
            Assert.Equal(1, lru.Count);

            var v2 = lru.Get("B");
            Assert.True(lru.Count <= 1);

            // 上一个应该已被淘汰
            Assert.False(lru.TryGet("A", out _));
        }

        /// <summary>
        /// 容量为 2 的 LRU 功能验证。
        /// </summary>
        [Fact]
        public void Test_BasicLruEviction()
        {
            var cache = new Lru<string, int>(capacity: 2, strings => strings.GetHashCode());

            cache.Put("A", 1);
            cache.Put("B", 2);

            // 访问 A，使 B 成为最久未使用
            Assert.True(cache.TryGet("A", out _));

            // 触发淘汰，应淘汰 B
            cache.Put("C", 3);

            Assert.False(cache.TryGet("B", out _));
            Assert.True(cache.TryGet("A", out var va));
            Assert.Equal(1, va);
            Assert.True(cache.TryGet("C", out var vc));
            Assert.Equal(3, vc);
        }

        /// <summary>
        /// Put 更新同键时，不应增加 Count。
        /// </summary>
        [Fact]
        public void Put_SameKey_ShouldUpdateValueNotCount()
        {
            var cache = new Lru<int, string>(4, k => k.ToString());

            cache.Put(1, "first");
            Assert.Equal(1, cache.Count);

            cache.Put(1, "second");
            Assert.Equal(1, cache.Count);

            Assert.True(cache.TryGet(1, out var val));
            Assert.Equal("second", val);
        }

        /// <summary>
        /// Put 重复值不增加 Count (Lru{T})。
        /// </summary>
        [Fact]
        public void PutSingleType_DuplicateValue_ShouldNotIncrease()
        {
            var lru = new Lru<int>(10);

            lru.Put(1, out _);
            lru.Put(1, out _);
            lru.Put(1, out _);

            Assert.Equal(1, lru.Count);
        }

        /// <summary>
        /// 工厂异常不应导致已有数据被误淘汰。
        /// </summary>
        [Fact]
        public void Get_WhenFactoryThrows_ShouldNotEvictExistingItems()
        {
            var lru = new Lru<int, int>(2, key =>
            {
                if (key == 3)
                {
                    throw new InvalidOperationException("factory failed");
                }

                return key * 10;
            });

            Assert.Equal(10, lru.Get(1));
            Assert.Equal(20, lru.Get(2));

            Assert.Throws<InvalidOperationException>(() => lru.Get(3));

            Assert.True(lru.TryGet(1, out var v1));
            Assert.True(lru.TryGet(2, out var v2));
            Assert.Equal(10, v1);
            Assert.Equal(20, v2);
            Assert.Equal(2, lru.Count);
        }

        /// <summary>
        /// 覆盖写入相同键时应释放旧值，防止资源泄漏。
        /// </summary>
        [Fact]
        public void Put_UpdateExistingKey_ShouldDisposeOldValue()
        {
            var lru = new Lru<int, DisposableProbe>(4, _ => new DisposableProbe());

            var oldValue = new DisposableProbe();
            var newValue = new DisposableProbe();

            lru.Put(1, oldValue);
            lru.Put(1, newValue);

            Assert.Equal(1, oldValue.DisposeCount);
            Assert.Equal(0, newValue.DisposeCount);
        }

        /// <summary>
        /// TryGet 不存在的键应返回 false。
        /// </summary>
        [Fact]
        public void TryGet_NonExistentKey_ReturnsFalse()
        {
            var lru = new Lru<int, int>(10, x => x);

            Assert.False(lru.TryGet(999, out var val));
            Assert.Equal(default, val);
        }

        /// <summary>
        /// 满容量后每次新键 Put 都应淘汰最旧项。
        /// </summary>
        [Fact]
        public void Put_FullCapacity_ShouldEvictLeastRecentlyUsed()
        {
            var lru = new Lru<int>(3);

            lru.Put(1, out _);
            lru.Put(2, out _);
            lru.Put(3, out _);

            // 淘汰 1（最旧）
            bool evicted = lru.Put(4, out var obsolete);
            Assert.True(evicted);
            Assert.Equal(3, lru.Count);
        }

        #endregion

        #region 并发线程安全测试

        /// <summary>
        /// Lru{T} 线程安全测试。
        /// </summary>
        [Fact]
        public async Task TestThreadSafetyOneAsync()
        {
            long totalMilliseconds = 0;

            int length = 1000;
            int capacity = length / 10;
            var lru = new Lru<int>(capacity);

            var tasks = new List<Task>(50);

            for (int i = 0; i < 50; i++)
            {
                tasks.Add(Task.Run(() =>
                {
                    Stopwatch stopwatch = new Stopwatch();

                    for (int j = 0; j < length; j++)
                    {
                        stopwatch.Start();

                        lru.Put(j, out _);

                        stopwatch.Stop();

                        Assert.True(lru.Count <= capacity);
                    }

                    stopwatch.Stop();

                    Interlocked.Add(ref totalMilliseconds, stopwatch.ElapsedMilliseconds);
                }));
            }

            await Task.WhenAll(tasks);

            Assert.True(lru.Count <= capacity);

            Debug.WriteLine($"Lru<T> 线程安全：{50 * length} 次操作，{totalMilliseconds}ms");
        }

        /// <summary>
        /// Lru{TKey, TValue} 线程安全测试。
        /// </summary>
        [Fact]
        public async Task TestThreadSafetyAsync()
        {
            long totalMilliseconds = 0;

            int length = 1000;
            int capacity = length / 10;
            var lru = new Lru<int, int>(capacity, x => x * x);

            var tasks = new List<Task>(50);

            for (int i = 0; i < 50; i++)
            {
                tasks.Add(Task.Run(() =>
                {
                    Stopwatch stopwatch = new Stopwatch();

                    for (int j = 0; j < length; j++)
                    {
                        stopwatch.Start();
                        var v = lru.Get(j);
                        stopwatch.Stop();

                        Assert.Equal(j * j, v);

                        Assert.True(lru.Count <= capacity);
                    }

                    stopwatch.Stop();

                    Interlocked.Add(ref totalMilliseconds, stopwatch.ElapsedMilliseconds);
                }));
            }

            await Task.WhenAll(tasks);

            Assert.True(lru.Count <= capacity);

            Debug.WriteLine($"Lru<K,V> 线程安全：{50 * length} 次操作，{totalMilliseconds}ms");
        }

        /// <summary>
        /// 高并发混合读写测试：50% Get + 50% Put 交替执行。
        /// </summary>
        [Fact]
        public async Task ConcurrentMixedReadWrite_ShouldNotCorruptAsync()
        {
            int capacity = 200;
            var lru = new Lru<int, int>(capacity, x => x);

            var tasks = new List<Task>(40);

            for (int i = 0; i < 20; i++)
            {
                tasks.Add(Task.Run(() =>
                {
                    for (int j = 0; j < 2000; j++)
                    {
                        lru.Get(j % 500);
                    }
                }));

                tasks.Add(Task.Run(() =>
                {
                    for (int j = 0; j < 2000; j++)
                    {
                        lru.Put(j % 500, j);
                    }
                }));
            }

            await Task.WhenAll(tasks);

            Assert.True(lru.Count <= capacity);
        }

        /// <summary>
        /// 不同分片的工厂应可并发执行。
        /// </summary>
        [Fact]
        public async Task Get_FactoryShouldRunConcurrently_ForDifferentKeysAsync()
        {
            int running = 0;
            int maxRunning = 0;

            var lru = new Lru<int, int>(64, key =>
            {
                var current = Interlocked.Increment(ref running);

                int snapshot;
                while ((snapshot = maxRunning) < current)
                {
                    Interlocked.CompareExchange(ref maxRunning, current, snapshot);
                }

                Thread.Sleep(50);
                Interlocked.Decrement(ref running);

                return key;
            });

            var tasks = new List<Task<int>>(16);

            for (int i = 0; i < 16; i++)
            {
                int key = i;
                tasks.Add(Task.Run(() => lru.Get(key)));
            }

            await Task.WhenAll(tasks);

            Assert.True(maxRunning > 1);
            Assert.Equal(16, lru.Count);
        }

        /// <summary>
        /// 同一键高并发未命中时，工厂不重复调用（分片锁内执行）。
        /// </summary>
        [Fact]
        public async Task Get_SameKeyConcurrent_ShouldNotDuplicateFactoryAsync()
        {
            int factoryCalls = 0;

            var lru = new Lru<int, int>(32, key =>
            {
                Interlocked.Increment(ref factoryCalls);
                Thread.Sleep(30);
                return key * key;
            });

            var tasks = new List<Task>(24);

            for (int i = 0; i < 24; i++)
            {
                tasks.Add(Task.Run(() => { lru.Get(7); }));
            }

            await Task.WhenAll(tasks);

            Assert.Equal(1, factoryCalls);
            Assert.Equal(49, lru.Get(7));
        }

        /// <summary>
        /// 同一键高并发未命中时，最终仅保留一个缓存值，重复创建值应被释放。
        /// </summary>
        [Fact]
        public async Task Get_SameKeyConcurrent_ShouldDisposeDuplicatedCreatedValuesAsync()
        {
            int factoryCalls = 0;
            var createdValues = new ConcurrentBag<DisposableProbe>();

            var lru = new Lru<int, DisposableProbe>(32, _ =>
            {
                var value = new DisposableProbe();
                createdValues.Add(value);
                Interlocked.Increment(ref factoryCalls);

                Thread.Sleep(30);

                return value;
            });

            var tasks = new List<Task>(24);

            for (int i = 0; i < 24; i++)
            {
                tasks.Add(Task.Run(() =>
                {
                    var _ = lru.Get(7);
                }));
            }

            await Task.WhenAll(tasks);

            Assert.Equal(1, lru.Count);
            Assert.Equal(1, factoryCalls);

            var totalDisposed = createdValues.Sum(x => x.DisposeCount);
            Assert.Equal(0, totalDisposed);
        }

        #endregion

        #region 性能压测

        /// <summary>
        /// 性能测试：百万次操作。
        /// </summary>
        [Fact]
        public void TestPerformance()
        {
            int capacity = 1000;

            Stopwatch stopwatch = new Stopwatch();

            var lru = new Lru<int, int>(capacity, x => x * x);

            for (int i = 0; i < capacity; i++)
            {
                for (int j = 0; j < capacity; j++)
                {
                    stopwatch.Start();
                    var v = lru.Get(j);
                    stopwatch.Stop();

                    Assert.Equal(j * j, v);

                    Assert.True(lru.Count <= capacity);
                }
            }

            Debug.WriteLine($"Lru 性能测试：{capacity * capacity} 次操作，{stopwatch.ElapsedMilliseconds}ms");
        }

        /// <summary>
        /// 热点数据性能测试：容量不足时频繁淘汰+命中。
        /// </summary>
        [Fact]
        public void TestHot()
        {
            int capacity = 1000;

            Stopwatch stopwatch = new Stopwatch();

            var lru = new Lru<int, int>(capacity / 2, x => x * x);

            for (int i = 0; i < capacity; i++)
            {
                for (int j = 0; j < capacity; j++)
                {
                    stopwatch.Start();
                    var v = lru.Get(j);
                    stopwatch.Stop();

                    Assert.Equal(j * j, v);

                    Assert.True(lru.Count <= capacity);
                }
            }

            Debug.WriteLine($"Lru 热点数据测试：{capacity * capacity} 次操作，{stopwatch.ElapsedMilliseconds}ms");
        }

        /// <summary>
        /// 并发性能压测：大容量 + 高线程数。
        /// </summary>
        [Fact]
        public async Task ConcurrentPerformanceBenchmarkAsync()
        {
            int capacity = 5000;
            int threadCount = Environment.ProcessorCount * 2;
            int opsPerThread = 10000;

            var lru = new Lru<int, int>(capacity, x => x * x);
            var sw = Stopwatch.StartNew();

            var tasks = new List<Task>(threadCount);

            for (int t = 0; t < threadCount; t++)
            {
                int offset = t * 100;

                tasks.Add(Task.Run(() =>
                {
                    for (int j = 0; j < opsPerThread; j++)
                    {
                        lru.Get((offset + j) % (capacity * 2));
                    }
                }));
            }

            await Task.WhenAll(tasks);
            sw.Stop();

            Assert.True(lru.Count <= capacity);

            Debug.WriteLine($"Lru 并发压测：{threadCount} 线程 × {opsPerThread} 次 = {threadCount * opsPerThread} 总操作，{sw.ElapsedMilliseconds}ms");
        }

        #endregion
    }
}
