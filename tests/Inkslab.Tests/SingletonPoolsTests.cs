using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
        public void TestCombinationWithB()
        {
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
            SingletonPools.Singleton<SimpleB>();
            var combinationC2 = SingletonPools.Singleton<CombinationC>();

            Assert.True(combinationC2.SimpleB is null);
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
    }
}
