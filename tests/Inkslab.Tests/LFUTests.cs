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
            int total = 0;
            long totalMilliseconds = 0;

            Stopwatch totalStopwatch = Stopwatch.StartNew();

            int capacity = 1000;
            var lfu = new LFU<int, int>(capacity / 10);

            int length = 20;
            var tasks = new List<Task>(capacity);

            for (int i = 0; i < length; i++)
            {
                tasks.Add(Task.Run(() =>
                {
                    Stopwatch stopwatch = new Stopwatch();

                    for (int j = 0; j < capacity; j++)
                    {
                        stopwatch.Start();
                        var v = lfu.GetOrCreate(j, x => x * x);
                        stopwatch.Stop();

                        Debug.WriteLine(++total);

                        Assert.Equal(j * j, v);

                        Assert.True(lfu.Count <= capacity);
                    }

                    stopwatch.Stop();

                    totalMilliseconds += stopwatch.ElapsedMilliseconds;
                }));
            }

            await Task.WhenAll(tasks.ToArray());

            totalStopwatch.Stop();

            Debug.WriteLine($"计算{length * capacity}次，共执行{totalMilliseconds}毫秒");
        }

        /// <summary>
        /// 性能测试。
        /// </summary>
        [Fact]
        public void TestPerformance()
        {
            int capacity = 1000;

            Stopwatch stopwatch = new Stopwatch();

            var lfu = new LFU<int, int>(capacity);

            for (int i = 0; i < capacity; i++)
            {
                for (int j = 0; j < capacity; j++)
                {
                    stopwatch.Start();
                    var v = lfu.GetOrCreate(j, x => x * x);
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

            var lfu = new LFU<int, int>(capacity / 2);

            for (int i = 0; i < capacity; i++)
            {
                for (int j = 0; j < capacity; j++)
                {
                    stopwatch.Start();
                    var v = lfu.GetOrCreate(j, x => x * x);
                    stopwatch.Stop();

                    Assert.Equal(j * j, v);

                    Assert.True(lfu.Count <= capacity);
                }
            }

            Debug.WriteLine($"热点数据计算{capacity * capacity}次，共执行{stopwatch.ElapsedMilliseconds}毫秒");
        }
    }
}
