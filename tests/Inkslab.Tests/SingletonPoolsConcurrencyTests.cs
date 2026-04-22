using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Inkslab.Tests
{
    /// <summary>
    /// 并发服务A。
    /// </summary>
    public class ConcurrentServiceA
    {
        private static int _instanceCount;

        /// <summary>
        /// 实例计数。
        /// </summary>
        public static int InstanceCount => _instanceCount;

        /// <summary>
        /// 构造函数。
        /// </summary>
        public ConcurrentServiceA()
        {
            Interlocked.Increment(ref _instanceCount);
        }
    }

    /// <summary>
    /// 并发服务B，依赖 ConcurrentServiceA。
    /// </summary>
    public class ConcurrentServiceB
    {
        /// <summary>
        /// 依赖项。
        /// </summary>
        public ConcurrentServiceA ServiceA { get; }

        /// <summary>
        /// 构造函数。
        /// </summary>
        public ConcurrentServiceB(ConcurrentServiceA serviceA)
        {
            ServiceA = serviceA;
        }
    }

    /// <summary>
    /// 工厂注入的服务。
    /// </summary>
    public class FactoryService
    {
    }

    /// <summary>
    /// SingletonPools 并发安全与边界测试。
    /// </summary>
    public class SingletonPoolsConcurrencyTests
    {
        /// <summary>
        /// 多线程并发获取同一服务应返回相同实例。
        /// </summary>
        [Fact]
        public async Task ConcurrentSingleton_ShouldReturnSameInstanceAsync()
        {
            const int threadCount = 20;
            var instances = new ConcurrentBag<ConcurrentServiceA>();

            var tasks = new Task[threadCount];
            for (int i = 0; i < threadCount; i++)
            {
                tasks[i] = Task.Run(() =>
                {
                    var instance = SingletonPools.Singleton<ConcurrentServiceA>();
                    instances.Add(instance);
                });
            }

            await Task.WhenAll(tasks);

            var first = instances.ToArray()[0];
            foreach (var instance in instances)
            {
                Assert.Same(first, instance);
            }
        }

        /// <summary>
        /// 多线程并发注册和获取不会死锁。
        /// </summary>
        [Fact]
        public async Task ConcurrentRegisterAndResolve_ShouldNotDeadlockAsync()
        {
            const int threadCount = 10;
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

            var tasks = new List<Task>(threadCount * 2);

            for (int i = 0; i < threadCount; i++)
            {
                tasks.Add(Task.Run(() =>
                {
                    SingletonPools.TryAdd(() => new FactoryService());
                }, cts.Token));

                tasks.Add(Task.Run(() =>
                {
                    SingletonPools.Singleton<FactoryService>();
                }, cts.Token));
            }

            await Task.WhenAll(tasks);

            var service = SingletonPools.Singleton<FactoryService>();
            Assert.NotNull(service);
        }

        /// <summary>
        /// 工厂方法返回null时应抛出InvalidOperationException。
        /// </summary>
        [Fact]
        public void Factory_ReturningNull_ShouldThrow_InvalidOperationException()
        {
            SingletonPools.TryAdd<NullFactoryService>(() => null);

            Assert.Throws<InvalidOperationException>(() =>
                SingletonPools.Singleton<NullFactoryService>());
        }

        /// <summary>
        /// TryAdd传入null实例应抛出ArgumentNullException。
        /// </summary>
        [Fact]
        public void TryAdd_NullInstance_ShouldThrow()
        {
            Assert.Throws<ArgumentNullException>(() =>
                SingletonPools.TryAdd<NullFactoryService>(instance: null));
        }

        /// <summary>
        /// TryAdd传入null工厂应抛出ArgumentNullException。
        /// </summary>
        [Fact]
        public void TryAdd_NullFactory_ShouldThrow()
        {
            Assert.Throws<ArgumentNullException>(() =>
                SingletonPools.TryAdd<NullFactoryService>(factory: null));
        }

        /// <summary>
        /// 获取未注册的接口应抛出NotSupportedException。
        /// </summary>
        [Fact]
        public void Singleton_UnregisteredInterface_ShouldThrow()
        {
            Assert.Throws<NotSupportedException>(() =>
                SingletonPools.Singleton<IUnregisteredService>());
        }

        /// <summary>
        /// 依赖注入应正确解析构造函数参数。
        /// </summary>
        [Fact]
        public void Singleton_WithDependency_ShouldResolveCorrectly()
        {
            SingletonPools.TryAdd<ConcurrentServiceA>();

            var serviceB = SingletonPools.Singleton<ConcurrentServiceB>();
            Assert.NotNull(serviceB);
            Assert.NotNull(serviceB.ServiceA);
            Assert.Same(SingletonPools.Singleton<ConcurrentServiceA>(), serviceB.ServiceA);
        }

        /// <summary>
        /// Singleton泛型重载应在服务未注册时返回默认实现。
        /// </summary>
        [Fact]
        public void Singleton_WithFallback_ShouldUseImplementation()
        {
            var result = SingletonPools.Singleton<SingleWeight, SingleSubWeight>();
            Assert.NotNull(result);
            Assert.IsType<SingleSubWeight>(result);
        }
    }

    /// <summary>
    /// 工厂返回null测试用服务。
    /// </summary>
    public class NullFactoryService
    {
    }

    /// <summary>
    /// 未注册的接口。
    /// </summary>
    public interface IUnregisteredService
    {
        /// <summary>
        /// 方法。
        /// </summary>
        void DoWork();
    }
}
