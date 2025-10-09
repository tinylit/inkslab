using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Inkslab.Tests.Performance
{
    /// <summary>
    /// SingletonPools 性能基准测试
    /// </summary>
    public class SingletonPoolsPerformanceTests
    {
        private readonly ITestOutputHelper _output;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="output">测试输出</param>
        public SingletonPoolsPerformanceTests(ITestOutputHelper output)
        {
            _output = output;
        }

        /// <summary>
        /// 测试服务
        /// </summary>
        public interface ITestService
        {
            /// <summary>
            /// 获取数据
            /// </summary>
            /// <returns>数据</returns>
            string GetData();
        }

        /// <summary>
        /// 测试服务实现
        /// </summary>
        public class TestService : ITestService
        {
            /// <summary>
            /// 获取数据
            /// </summary>
            /// <returns>数据</returns>
            public string GetData() => "Test Data";
        }

        /// <summary>
        /// 复杂依赖服务
        /// </summary>
        public class ComplexService
        {
            /// <summary>
            /// 测试服务
            /// </summary>
            public ITestService TestService { get; }
            
            /// <summary>
            /// 创建时间
            /// </summary>
            public DateTime CreatedTime { get; }

            /// <summary>
            /// 构造函数
            /// </summary>
            /// <param name="testService">测试服务</param>
            public ComplexService(ITestService testService)
            {
                TestService = testService;
                CreatedTime = DateTime.Now;
            }
        }

        /// <summary>
        /// 测试单例获取性能
        /// </summary>
        [Fact]
        public void TestSingletonGetPerformance()
        {
            // 预热
            SingletonPools.TryAdd<ITestService, TestService>();
            var warmup = SingletonPools.Singleton<ITestService>();

            const int iterations = 1_000_000;
            var stopwatch = Stopwatch.StartNew();

            for (int i = 0; i < iterations; i++)
            {
                var service = SingletonPools.Singleton<ITestService>();
                Assert.NotNull(service);
            }

            stopwatch.Stop();

            var throughput = iterations / stopwatch.Elapsed.TotalSeconds;
            _output.WriteLine($"单例获取性能测试:");
            _output.WriteLine($"迭代次数: {iterations:N0}");
            _output.WriteLine($"总耗时: {stopwatch.ElapsedMilliseconds:N0} ms");
            _output.WriteLine($"平均耗时: {stopwatch.Elapsed.TotalMilliseconds * 1000 / iterations:F2} μs/op");
            _output.WriteLine($"吞吐量: {throughput:N0} ops/sec");

            // 性能要求：应该能够每秒处理至少100万次操作
            Assert.True(throughput > 1_000_000, $"吞吐量过低: {throughput:N0} ops/sec");
        }

        /// <summary>
        /// 测试并发注册性能
        /// </summary>
        [Fact]
        public async Task TestConcurrentRegistrationPerformanceAsync()
        {
            const int threadCount = 10;
            const int operationsPerThread = 1000;
            var totalOperations = threadCount * operationsPerThread;

            var stopwatch = Stopwatch.StartNew();
            var tasks = new Task[threadCount];

            for (int t = 0; t < threadCount; t++)
            {
                int threadIndex = t;
                tasks[t] = Task.Run(() =>
                {
                    for (int i = 0; i < operationsPerThread; i++)
                    {
                        // 模拟注册不同的服务类型
                        SingletonPools.TryAdd<ITestService>(() => new TestService());
                    }
                });
            }

            await Task.WhenAll(tasks);
            stopwatch.Stop();

            var throughput = totalOperations / stopwatch.Elapsed.TotalSeconds;
            _output.WriteLine($"并发注册性能测试:");
            _output.WriteLine($"线程数: {threadCount}");
            _output.WriteLine($"每线程操作数: {operationsPerThread:N0}");
            _output.WriteLine($"总操作数: {totalOperations:N0}");
            _output.WriteLine($"总耗时: {stopwatch.ElapsedMilliseconds:N0} ms");
            _output.WriteLine($"吞吐量: {throughput:N0} ops/sec");

            // 确保所有操作都完成且没有异常
            Assert.True(throughput > 0);
        }

        /// <summary>
        /// 测试依赖注入性能
        /// </summary>
        [Fact]
        public void TestDependencyInjectionPerformance()
        {
            // 注册依赖服务
            SingletonPools.TryAdd<ITestService, TestService>();

            const int iterations = 100_000;
            var stopwatch = Stopwatch.StartNew();

            for (int i = 0; i < iterations; i++)
            {
                var service = SingletonPools.Singleton<ComplexService>();
                Assert.NotNull(service);
                Assert.NotNull(service.TestService);
            }

            stopwatch.Stop();

            var throughput = iterations / stopwatch.Elapsed.TotalSeconds;
            _output.WriteLine($"依赖注入性能测试:");
            _output.WriteLine($"迭代次数: {iterations:N0}");
            _output.WriteLine($"总耗时: {stopwatch.ElapsedMilliseconds:N0} ms");
            _output.WriteLine($"平均耗时: {stopwatch.Elapsed.TotalMilliseconds * 1000 / iterations:F2} μs/op");
            _output.WriteLine($"吞吐量: {throughput:N0} ops/sec");
        }

        /// <summary>
        /// 测试内存使用情况
        /// </summary>
        [Fact]
        public void TestMemoryUsage()
        {
            var initialMemory = GC.GetTotalMemory(true);

            // 注册大量服务以测试内存使用
            const int serviceCount = 10000;
            for (int i = 0; i < serviceCount; i++)
            {
                SingletonPools.TryAdd<ITestService>(() => new TestService());
            }

            // 获取实例以触发创建
            for (int i = 0; i < 100; i++)
            {
                var service = SingletonPools.Singleton<ITestService>();
                Assert.NotNull(service);
            }

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            var finalMemory = GC.GetTotalMemory(false);
            var memoryUsed = finalMemory - initialMemory;

            _output.WriteLine($"内存使用测试:");
            _output.WriteLine($"注册服务数: {serviceCount:N0}");
            _output.WriteLine($"初始内存: {initialMemory:N0} bytes");
            _output.WriteLine($"最终内存: {finalMemory:N0} bytes");
            _output.WriteLine($"内存增长: {memoryUsed:N0} bytes");
            _output.WriteLine($"平均每服务: {(double)memoryUsed / serviceCount:F2} bytes");

            // 内存使用应该是合理的
            Assert.True(memoryUsed < 50 * 1024 * 1024); // 少于50MB
        }

        /// <summary>
        /// 测试并发访问性能
        /// </summary>
        [Fact]
        public async Task TestConcurrentAccessPerformanceAsync()
        {
            // 预注册服务
            SingletonPools.TryAdd<ITestService, TestService>();

            var threadCount = 8; // 固定线程数
            const int operationsPerThread = 100_000;
            var totalOperations = threadCount * operationsPerThread;
            var results = new ConcurrentBag<TimeSpan>();

            var stopwatch = Stopwatch.StartNew();
            var tasks = new Task[threadCount];

            for (int t = 0; t < threadCount; t++)
            {
                tasks[t] = Task.Run(() =>
                {
                    var threadStopwatch = Stopwatch.StartNew();
                    for (int i = 0; i < operationsPerThread; i++)
                    {
                        var service = SingletonPools.Singleton<ITestService>();
                        Assert.NotNull(service);
                    }
                    threadStopwatch.Stop();
                    results.Add(threadStopwatch.Elapsed);
                });
            }

            await Task.WhenAll(tasks);
            stopwatch.Stop();

            var throughput = totalOperations / stopwatch.Elapsed.TotalSeconds;
            var avgThreadTime = TimeSpan.FromTicks((long)results.ToArray().Average(t => t.Ticks));

            _output.WriteLine($"并发访问性能测试:");
            _output.WriteLine($"线程数: {threadCount}");
            _output.WriteLine($"每线程操作数: {operationsPerThread:N0}");
            _output.WriteLine($"总操作数: {totalOperations:N0}");
            _output.WriteLine($"总耗时: {stopwatch.ElapsedMilliseconds:N0} ms");
            _output.WriteLine($"平均线程耗时: {avgThreadTime.TotalMilliseconds:F2} ms");
            _output.WriteLine($"吞吐量: {throughput:N0} ops/sec");

            // 并发性能应该良好
            Assert.True(throughput > 500_000, $"并发吞吐量过低: {throughput:N0} ops/sec");
        }
    }
}