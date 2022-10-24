﻿using Inkslab.Collections;
using System.Diagnostics;
using System.Threading.Tasks;
using Xunit;

namespace Inkslab.Tests
{
    public class LRUTests
    {
        /// <summary>
        /// 线程安全测试。
        /// </summary>
        [Fact]
        public void TestThreadSafety()
        {
            int total = 0;

            int capacity = 1000;
            var lru = new LRU<int, int>(capacity / 10, x => x * x);

            Stopwatch stopwatch = new Stopwatch();

            var tasks = new List<Task>(capacity);

            for (int i = 0; i < 50; i++)
            {
                tasks.Add(Task.Run(() =>
                {
                    for (int j = 0; j < capacity; j++)
                    {
                        stopwatch.Start();
                        var v = lru.Get(j);
                        stopwatch.Stop();

                        Debug.WriteLine(++total);

                        Assert.Equal(j * j, v);

                        Assert.True(lru.Count <= capacity);
                    }
                }));
            }

            Task.WaitAll(tasks.ToArray());

            stopwatch.Stop();
        }

        /// <summary>
        /// 性能测试。
        /// </summary>
        [Fact]
        public void TestPerformance()
        {
            int capacity = 1000;

            Stopwatch stopwatch = new Stopwatch();

            var lru = new LRU<int, int>(capacity, x => x * x);

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

            var lru = new LRU<int, int>(capacity / 2, x => x * x);

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

            Debug.WriteLine($"热点数据计算{capacity * capacity}次，共执行{stopwatch.ElapsedMilliseconds}毫秒");
        }
    }
}
