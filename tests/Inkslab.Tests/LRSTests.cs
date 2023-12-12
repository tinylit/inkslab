using Inkslab.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Xunit;

namespace Inkslab.Tests
{
    /// <summary>
    /// <see cref="LRS{TKey, TValue}"/> 算法测试。
    /// </summary>
    public class LRSTests
    {
        /// <summary>
        /// 线程安全测试。
        /// </summary>
        [Fact]
        public async Task TestThreadSafety()
        {
            //int total = 0;
            long totalMilliseconds = 0;

            Stopwatch totalStopwatch = Stopwatch.StartNew();

            int length = 1000;

            int capacity = length / 2;
            var lru = new LRS<int, int>(capacity, x => x * x);

            var tasks = new List<Task>(capacity);

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

                        //Debug.WriteLine($"{j}*{j}={v}");

                        Assert.True(j * j == v);

                        Assert.True(lru.Count <= capacity);
                    }

                    stopwatch.Stop();

                    totalMilliseconds += stopwatch.ElapsedMilliseconds;
                }));
            }

            await Task.WhenAll(tasks.ToArray());

            totalStopwatch.Stop();

            Assert.True(lru.Count <= capacity);

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

            var lru = new LRS<int, int>(capacity, x => x * x);

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

            var lru = new LRS<int, int>(capacity / 2, x => x * x);

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
