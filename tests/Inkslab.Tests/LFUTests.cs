using Inkslab.Collections;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Inkslab.Tests
{
    /// <summary>
    /// <see cref="Lfu{TKey, TValue}"/> 算法测试。
    /// </summary>
    public class LFUTests
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
        public void CapacityZero_ShouldThrow_ForSingleValueLfu()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new Lfu<int>(0));
        }

        /// <summary>
        /// 负数容量应抛出异常。
        /// </summary>
        [Fact]
        public void CapacityNegative_ShouldThrow()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new Lfu<int>(-1));
            Assert.Throws<ArgumentException>(() => new Lfu<int, int>(-1, x => x));
        }

        /// <summary>
        /// 容量为 1 的极端边界，频繁淘汰。
        /// </summary>
        [Fact]
        public void Capacity1_ShouldEvictOnEveryNewKey()
        {
            var lfu = new Lfu<string, int>(1, s => s.GetHashCode());

            var v1 = lfu.Get("A");
            Assert.Equal(1, lfu.Count);

            var v2 = lfu.Get("B");
            Assert.True(lfu.Count <= 1);

            // A 应该已被淘汰
            Assert.False(lfu.TryGet("A", out _));
        }

        /// <summary>
        /// 功能测试：LFU 应淘汰频率最低的项。
        /// </summary>
        [Fact]
        public void Test_BasicLfuEviction()
        {
            var cache = new Lfu<string, int>(capacity: 2, strings => strings.GetHashCode());

            cache.Put("A", 1);
            cache.Put("B", 2);

            // 提升 A 频率
            cache.TryGet("A", out _);  // A频率=2
            cache.TryGet("A", out _);  // A频率=3

            // 触发淘汰，应淘汰频率低的 B
            cache.Put("C", 3);

            Assert.False(cache.TryGet("B", out _));
            Assert.True(cache.TryGet("A", out _));
            Assert.True(cache.TryGet("C", out _));
        }

        /// <summary>
        /// Put 更新同键时，不应增加 Count。
        /// </summary>
        [Fact]
        public void Put_SameKey_ShouldUpdateValueNotCount()
        {
            var cache = new Lfu<int, string>(4, k => k.ToString());

            cache.Put(1, "first");
            Assert.Equal(1, cache.Count);

            cache.Put(1, "second");
            Assert.Equal(1, cache.Count);

            Assert.True(cache.TryGet(1, out var val));
            Assert.Equal("second", val);
        }

        /// <summary>
        /// Put 重复值不增加 Count (Lfu{T})。
        /// </summary>
        [Fact]
        public void PutSingleType_DuplicateValue_ShouldNotIncrease()
        {
            var lfu = new Lfu<int>(10);

            lfu.Put(1, out _);
            lfu.Put(1, out _);
            lfu.Put(1, out _);

            Assert.Equal(1, lfu.Count);
        }

        /// <summary>
        /// 工厂异常不应导致缓存中已有数据被误淘汰。
        /// </summary>
        [Fact]
        public void Get_WhenFactoryThrows_ShouldNotEvictExistingItems()
        {
            var lfu = new Lfu<int, int>(2, key =>
            {
                if (key == 3)
                {
                    throw new InvalidOperationException("factory failed");
                }

                return key * 10;
            });

            Assert.Equal(10, lfu.Get(1));
            Assert.Equal(20, lfu.Get(2));

            Assert.Throws<InvalidOperationException>(() => lfu.Get(3));

            Assert.True(lfu.TryGet(1, out var v1));
            Assert.True(lfu.TryGet(2, out var v2));
            Assert.Equal(10, v1);
            Assert.Equal(20, v2);
            Assert.Equal(2, lfu.Count);
        }

        /// <summary>
        /// 覆盖写入相同键时应释放旧值，防止资源泄漏。
        /// </summary>
        [Fact]
        public void Put_UpdateExistingKey_ShouldDisposeOldValue()
        {
            var lfu = new Lfu<int, DisposableProbe>(2, _ => new DisposableProbe());

            var oldValue = new DisposableProbe();
            var newValue = new DisposableProbe();

            lfu.Put(1, oldValue);
            lfu.Put(1, newValue);

            Assert.Equal(1, oldValue.DisposeCount);
            Assert.Equal(0, newValue.DisposeCount);
        }

        /// <summary>
        /// TryGet 不存在的键应返回 false。
        /// </summary>
        [Fact]
        public void TryGet_NonExistentKey_ReturnsFalse()
        {
            var lfu = new Lfu<int, int>(10, x => x);

            Assert.False(lfu.TryGet(999, out var val));
            Assert.Equal(default, val);
        }

        /// <summary>
        /// 满容量后新键 Put 应有淘汰事件 (Lfu{T})。
        /// </summary>
        [Fact]
        public void PutSingleType_FullCapacity_ShouldEvict()
        {
            var lfu = new Lfu<int>(3);

            lfu.Put(1, out _);
            lfu.Put(2, out _);
            lfu.Put(3, out _);

            bool evicted = lfu.Put(4, out var obsolete);
            Assert.True(evicted);
            Assert.Equal(3, lfu.Count);
        }

        /// <summary>
        /// 节点频率达到 int.MaxValue 时再次访问不应抛异常。
        /// </summary>
        [Fact]
        public void TryGet_WhenFrequencyAtIntMaxValue_ShouldStillWork()
        {
            var lfu = new Lfu<int, int>(2, key => key);

            Assert.Equal(1, lfu.Get(1));

            // 通过反射遍历分片找到包含 key=1 的节点
            var shardsField = typeof(Lfu<int, int>).GetField("_shards", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(shardsField);

            var shards = (Array)shardsField!.GetValue(lfu)!;
            object targetNode = null;

            for (int i = 0; i < shards.Length; i++)
            {
                var shard = shards.GetValue(i)!;
                var cachingsField = shard.GetType().GetField("_cachings", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.NotNull(cachingsField);

                var cachings = cachingsField!.GetValue(shard)!;
                var tryGetValueMethod = cachings.GetType().GetMethod("TryGetValue");
                Assert.NotNull(tryGetValueMethod);

                var args = new object[] { 1, null };
                var found = (bool)tryGetValueMethod!.Invoke(cachings, args)!;

                if (found)
                {
                    targetNode = args[1];
                    break;
                }
            }

            Assert.NotNull(targetNode);

            var frequencyProperty = targetNode!.GetType().GetProperty("Frequency", BindingFlags.Instance | BindingFlags.Public);
            Assert.NotNull(frequencyProperty);

            frequencyProperty!.SetValue(targetNode, int.MaxValue);

            Assert.True(lfu.TryGet(1, out var value));
            Assert.Equal(1, value);
        }

        #endregion

        #region 并发线程安全测试

        /// <summary>
        /// Lfu{T} 线程安全测试。
        /// </summary>
        [Fact]
        public async Task TestThreadSafetyOneAsync()
        {
            long totalMilliseconds = 0;

            int length = 1000;
            int capacity = length / 10;
            var lfu = new Lfu<int>(capacity);

            var tasks = new List<Task>(50);

            for (int i = 0; i < 50; i++)
            {
                tasks.Add(Task.Run(() =>
                {
                    Stopwatch stopwatch = new Stopwatch();

                    for (int j = 0; j < length; j++)
                    {
                        stopwatch.Start();

                        lfu.Put(j, out _);

                        stopwatch.Stop();

                        Assert.True(lfu.Count <= capacity);
                    }

                    stopwatch.Stop();

                    Interlocked.Add(ref totalMilliseconds, stopwatch.ElapsedMilliseconds);
                }));
            }

            await Task.WhenAll(tasks);

            Assert.True(lfu.Count <= capacity);

            Debug.WriteLine($"Lfu<T> 线程安全：{50 * length} 次操作，{totalMilliseconds}ms");
        }

        /// <summary>
        /// Lfu{TKey, TValue} 线程安全测试。
        /// </summary>
        [Fact]
        public async Task TestThreadSafetyAsync()
        {
            long totalMilliseconds = 0;

            int length = 1000;
            int capacity = length / 10;
            var lfu = new Lfu<int, int>(capacity, x => x * x);

            var tasks = new List<Task>(50);

            for (int i = 0; i < 50; i++)
            {
                tasks.Add(Task.Run(() =>
                {
                    Stopwatch stopwatch = new Stopwatch();

                    for (int j = 0; j < length; j++)
                    {
                        stopwatch.Start();
                        var v = lfu.Get(j);
                        stopwatch.Stop();

                        Assert.Equal(j * j, v);

                        Assert.True(lfu.Count <= capacity);
                    }

                    stopwatch.Stop();

                    Interlocked.Add(ref totalMilliseconds, stopwatch.ElapsedMilliseconds);
                }));
            }

            await Task.WhenAll(tasks);

            Assert.True(lfu.Count <= capacity);

            Debug.WriteLine($"Lfu<K,V> 线程安全：{50 * length} 次操作，{totalMilliseconds}ms");
        }

        /// <summary>
        /// 高并发混合读写测试：50% Get + 50% Put 交替执行。
        /// </summary>
        [Fact]
        public async Task ConcurrentMixedReadWrite_ShouldNotCorruptAsync()
        {
            int capacity = 200;
            var lfu = new Lfu<int, int>(capacity, x => x);

            var tasks = new List<Task>(40);

            for (int i = 0; i < 20; i++)
            {
                tasks.Add(Task.Run(() =>
                {
                    for (int j = 0; j < 2000; j++)
                    {
                        lfu.Get(j % 500);
                    }
                }));

                tasks.Add(Task.Run(() =>
                {
                    for (int j = 0; j < 2000; j++)
                    {
                        lfu.Put(j % 500, j);
                    }
                }));
            }

            await Task.WhenAll(tasks);

            Assert.True(lfu.Count <= capacity);
        }

        /// <summary>
        /// 不同分片的工厂应可并发执行。
        /// </summary>
        [Fact]
        public async Task Get_FactoryShouldRunConcurrently_ForDifferentKeysAsync()
        {
            int running = 0;
            int maxRunning = 0;

            var lfu = new Lfu<int, int>(64, key =>
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
                tasks.Add(Task.Run(() => lfu.Get(key)));
            }

            await Task.WhenAll(tasks);

            Assert.True(maxRunning > 1);
            Assert.Equal(16, lfu.Count);
        }

        /// <summary>
        /// 同一键高并发未命中时，工厂不重复调用（分片锁内执行）。
        /// </summary>
        [Fact]
        public async Task Get_SameKeyConcurrent_ShouldNotDuplicateFactoryAsync()
        {
            int factoryCalls = 0;

            var lfu = new Lfu<int, int>(32, key =>
            {
                Interlocked.Increment(ref factoryCalls);
                Thread.Sleep(30);
                return key * key;
            });

            var tasks = new List<Task>(24);

            for (int i = 0; i < 24; i++)
            {
                tasks.Add(Task.Run(() => { lfu.Get(7); }));
            }

            await Task.WhenAll(tasks);

            Assert.Equal(1, factoryCalls);
            Assert.Equal(49, lfu.Get(7));
        }

        /// <summary>
        /// 同一键高并发未命中时，最终仅保留一个缓存值，重复创建的值应被释放。
        /// </summary>
        [Fact]
        public async Task Get_SameKeyConcurrent_ShouldDisposeDuplicatedCreatedValuesAsync()
        {
            int factoryCalls = 0;
            var createdValues = new ConcurrentBag<DisposableProbe>();

            var lfu = new Lfu<int, DisposableProbe>(32, _ =>
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
                    var _ = lfu.Get(7);
                }));
            }

            await Task.WhenAll(tasks);

            Assert.Equal(1, lfu.Count);
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

            var lfu = new Lfu<int, int>(capacity, x => x * x);

            for (int i = 0; i < capacity; i++)
            {
                for (int j = 0; j < capacity; j++)
                {
                    stopwatch.Start();
                    var v = lfu.Get(j);
                    stopwatch.Stop();

                    Assert.Equal(j * j, v);

                    Assert.True(lfu.Count <= capacity);
                }
            }

            Debug.WriteLine($"Lfu 性能测试：{capacity * capacity} 次操作，{stopwatch.ElapsedMilliseconds}ms");
        }

        /// <summary>
        /// 热点数据性能测试：容量不足时频繁淘汰+命中。
        /// </summary>
        [Fact]
        public void TestHot()
        {
            int capacity = 1000;

            Stopwatch stopwatch = new Stopwatch();

            var lfu = new Lfu<int, int>(capacity / 2, x => x * x);

            for (int i = 0; i < capacity; i++)
            {
                for (int j = 0; j < capacity; j++)
                {
                    stopwatch.Start();
                    var v = lfu.Get(j);
                    stopwatch.Stop();

                    Assert.Equal(j * j, v);

                    Assert.True(lfu.Count <= capacity);
                }
            }

            Debug.WriteLine($"Lfu 热点数据测试：{capacity * capacity} 次操作，{stopwatch.ElapsedMilliseconds}ms");
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

            var lfu = new Lfu<int, int>(capacity, x => x * x);
            var sw = Stopwatch.StartNew();

            var tasks = new List<Task>(threadCount);

            for (int t = 0; t < threadCount; t++)
            {
                int offset = t * 100;

                tasks.Add(Task.Run(() =>
                {
                    for (int j = 0; j < opsPerThread; j++)
                    {
                        lfu.Get((offset + j) % (capacity * 2));
                    }
                }));
            }

            await Task.WhenAll(tasks);
            sw.Stop();

            Assert.True(lfu.Count <= capacity);

            Debug.WriteLine($"Lfu 并发压测：{threadCount} 线程 × {opsPerThread} 次 = {threadCount * opsPerThread} 总操作，{sw.ElapsedMilliseconds}ms");
        }

        #endregion
    }
}
