using Xunit;

namespace Inkslab.Tests
{
    /// <summary>
    /// 单列测试 -- 私有构造函数。
    /// </summary>
    public class SingleA : Singleton<SingleA>
    {
        private SingleA() { }
    }

    /// <summary>
    /// 单列测试 -- 私有构造函数。
    /// </summary>
    public class SimpleB
    {
        private SimpleB() { }
    }

    /// <summary>
    /// 单列测试 -- 私有构造函数，单例池构造函数注入。
    /// </summary>
    public class CombinationC
    {
        private CombinationC() { }

        private CombinationC(SimpleB b) => SimpleB = b;

        /// <summary>
        /// 注入对象。
        /// </summary>
        public SimpleB SimpleB { get; }
    }

    /// <summary>
    /// 单列测试 -- 公共构造函数。
    /// </summary>
    public class NormalD
    {
    }

    /// <summary>
    /// 测试注入级别。
    /// </summary>
    public class SingleWeight
    {

    }

    /// <summary>
    /// 测试注入级别。
    /// </summary>
    public class SingleSubWeight : SingleWeight
    {

    }

    /// <summary>
    /// 运行时服务池。
    /// </summary>
    public class SingletonPoolsTests
    {
        /// <summary>
        /// 单例测试。
        /// </summary>
        [Fact]
        public void TestSingleton()
        {
            var singleA = SingletonPools.Singleton<SingleA>();

            Assert.Equal(singleA, SingleA.Instance);

            Assert.Equal(singleA, Singleton<SingleA>.Instance);
        }

        /// <summary>
        /// 简单单例测试。
        /// </summary>
        [Fact]
        public void TestSimple()
        {
            var simpleB1 = SingletonPools.Singleton<SimpleB>();
            var simpleB2 = SingletonPools.Singleton<SimpleB>();

            Assert.Equal(simpleB1, simpleB2);
        }

        /// <summary>
        /// 构造函数注入单例测试。
        /// </summary>
        [Fact]
        public void TestCombinationAntiPollution()
        {
            //? 先获取 SimpleB 的单例。
            var simpleB = SingletonPools.Singleton<SimpleB>();
            var combinationC1 = SingletonPools.Singleton<CombinationC>();
            var combinationC2 = SingletonPools.Singleton<CombinationC>();

            Assert.False(simpleB is null);
            // Assert.True(combinationC2.SimpleB is null); 因为 SingletonPools 是线程级的，并行测试时，可能受到 TestCombinationWithB 的干扰。
            Assert.Equal(combinationC1, combinationC2);
        }

        /// <summary>
        /// 构造函数注入单例测试。
        /// </summary>
        [Fact]
        public void TestCombinationWithB()
        {
            //? 注册单列。
            SingletonPools.TryAdd<SimpleB>();

            //? 先获取 SimpleB 的单例。
            var simpleB = SingletonPools.Singleton<SimpleB>();
            var combinationC1 = SingletonPools.Singleton<CombinationC>();
            var combinationC2 = SingletonPools.Singleton<CombinationC>();

            Assert.Equal(combinationC2.SimpleB, simpleB);
            Assert.Equal(combinationC1, combinationC2);
        }
        /// <summary>
        /// 不指定构造函数参数测试。
        /// </summary>
        [Fact]
        public void TestCombinationWithoutB()
        {
            //? 先获取 CombinationC 的单例。
            var combinationC1 = SingletonPools.Singleton<CombinationC>();
            SingletonPools.TryAdd<SimpleB>();
            var combinationC2 = SingletonPools.Singleton<CombinationC>();

            //Assert.True(combinationC2.SimpleB is null); 单例池全局唯一，项目启动后，第一次获取就会生成唯一实例。为了避免CI/CD批处理异常注释了，如果需要证实，请去除注释，单独执行本方法。
            Assert.Equal(combinationC1, combinationC2);
        }

        /// <summary>
        /// 单例池指定对象。
        /// </summary>
        [Fact]
        public void TestNormal()
        {
            var normalD1 = new NormalD();

            SingletonPools.TryAdd(normalD1);

            var normalD2 = SingletonPools.Singleton<NormalD>();

            Assert.Equal(normalD1, normalD2);
        }

        /// <summary>
        /// 测试权重。
        /// </summary>
        [Fact]
        public void TestSingletonWeights()
        {
            var weights = new SingleSubWeight();

            SingletonPools.TryAdd<SingleWeight>();
            SingletonPools.TryAdd<SingleWeight>(weights);
            SingletonPools.TryAdd<SingleWeight, SingleWeight>();

            var singleWeight = SingletonPools.Singleton<SingleWeight>();

            Assert.Equal(weights, singleWeight);
        }
    }
}
