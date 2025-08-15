using Inkslab.Collections;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Xunit;

namespace Inkslab.Tests
{
    /// <summary>
    /// <see cref="Lru{TKey, TValue}"/> 算法测试。
    /// </summary>
    public class LRUTests
    {
        /// <summary>
        /// 线程安全测试。
        /// </summary>
        [Fact]
        public async Task TestThreadSafetyOneAsync()
        {
            // int total = 0;
            long totalMilliseconds = 0;

            Stopwatch totalStopwatch = Stopwatch.StartNew();

            int length = 1000;
            int capacity = length / 10;
            var lfu = new Lru<int>(capacity);

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

                        Assert.True(lfu.Count <= length);
                    }

                    stopwatch.Stop();

                    totalMilliseconds += stopwatch.ElapsedMilliseconds;
                }));
            }

            await Task.WhenAll(tasks.ToArray());

            totalStopwatch.Stop();

            Assert.True(lfu.Count == capacity);

            Debug.WriteLine($"计算{50 * length}次，共执行{totalMilliseconds}毫秒");
        }

        /// <summary>
        /// 线程安全测试。
        /// </summary>
        [Fact]
        public async Task TestThreadSafetyAsync()
        {
            // int total = 0;
            long totalMilliseconds = 0;

            Stopwatch totalStopwatch = Stopwatch.StartNew();

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

                        Assert.True(lru.Count <= length);
                    }

                    stopwatch.Stop();

                    totalMilliseconds += stopwatch.ElapsedMilliseconds;
                }));
            }

            await Task.WhenAll(tasks.ToArray());

            totalStopwatch.Stop();

            Debug.WriteLine(lru.Count);

            Assert.True(lru.Count == capacity);

            Debug.WriteLine($"计算{50 * length}次，共执行{totalMilliseconds}毫秒");
        }

        /// <summary>
        /// 性能测试。
        /// </summary>
        [Fact]
        public void TestPerformance()
        {
            int capacity = 1000;

            Stopwatch stopwatch = new Stopwatch();

            var lfu = new Lru<int, int>(capacity, x => x * x);

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

            Debug.WriteLine($"计算{capacity * capacity}次，共执行{stopwatch.ElapsedMilliseconds}毫秒");
        }

        /// <summary>
        /// 热点数据性能测试。
        /// </summary>
        [Fact]
        public void TestHot()
        {
            int capacity = 1000;

            Stopwatch stopwatch = new Stopwatch();

            var lfu = new Lru<int, int>(capacity / 2, x => x * x);

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

            Debug.WriteLine($"热点数据计算{capacity * capacity}次，共执行{stopwatch.ElapsedMilliseconds}毫秒");
        }

        /// <summary>
        /// 功能测试。
        /// </summary>
        [Fact]
        public void Test()
        {
            var cache = new Lru<string, int>(capacity: 2, strings => strings.GetHashCode());

            // 添加初始元素
            cache.Put("A", 1);
            cache.Put("B", 2);

            // 访问元素
            Assert.True(cache.TryGet("A", out _));  // 返回 true

            // 触发淘汰（容量=2，添加第三个元素淘汰最旧的"B"）
            cache.Put("C", 3);

            // 检查淘汰结果
            Assert.False(cache.TryGet("B", out _));
        }
    }
}
