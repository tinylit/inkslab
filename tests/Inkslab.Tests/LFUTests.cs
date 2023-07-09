using Inkslab.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Xunit;

namespace Inkslab.Tests
{
    /// <summary>
    /// <see cref="LFU{TKey, TValue}"/> 算法测试。
    /// </summary>
    public class LFUTests
    {
        /// <summary>
        /// 线程安全测试。
        /// </summary>
        [Fact]
        public async Task TestThreadSafety()
        {
            // int total = 0;
            long totalMilliseconds = 0;

            Stopwatch totalStopwatch = Stopwatch.StartNew();

            int length = 1000;
            int capacity = length / 10;
            var lfu = new LFU<int, int>(capacity, x => x * x);

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
        /// 性能测试。
        /// </summary>
        [Fact]
        public void TestPerformance()
        {
            int capacity = 1000;

            Stopwatch stopwatch = new Stopwatch();

            var lfu = new LFU<int, int>(capacity, x => x * x);

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

            var lfu = new LFU<int, int>(capacity / 2, x => x * x);

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
    }
}
