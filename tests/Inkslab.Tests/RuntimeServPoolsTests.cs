using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Inkslab.Tests
{
    public class SingleA : Singleton<SingleA>
    {
        private SingleA() { }
    }

    public class SimpleB
    {
        private SimpleB() { }
    }

    public class CombinationC
    {
        private CombinationC() { }

        private CombinationC(SimpleB b) => SimpleB = b;

        public SimpleB SimpleB { get; }
    }

    public class NormalD
    {
        public NormalD() { }
    }

    /// <summary>
    /// 运行时服务池。
    /// </summary>
    public class RuntimeServPoolsTests
    {
        [Fact]
        public void TestSingleton()
        {
            var singleA = RuntimeServPools.Singleton<SingleA>();

            Assert.Equal(singleA, SingleA.Instance);

            Assert.Equal(singleA, Singleton<SingleA>.Instance);
        }

        [Fact]
        public void TestSimple()
        {
            var simpleB1 = RuntimeServPools.Singleton<SimpleB>();
            var simpleB2 = RuntimeServPools.Singleton<SimpleB>();

            Assert.Equal(simpleB1, simpleB2);
        }

        [Fact]
        public void TestCombinationWithB()
        {
            //? 先获取 SimpleB 的单例。
            var simpleB = RuntimeServPools.Singleton<SimpleB>();
            var combinationC1 = RuntimeServPools.Singleton<CombinationC>();
            var combinationC2 = RuntimeServPools.Singleton<CombinationC>();

            Assert.Equal(combinationC2.SimpleB, simpleB);
            Assert.Equal(combinationC1, combinationC2);
        }

        [Fact]
        public void TestCombinationWithoutB()
        {
            //? 先获取 CombinationC 的单例。
            var combinationC1 = RuntimeServPools.Singleton<CombinationC>();
            RuntimeServPools.Singleton<SimpleB>();
            var combinationC2 = RuntimeServPools.Singleton<CombinationC>();

            Assert.True(combinationC2.SimpleB is null);
            Assert.Equal(combinationC1, combinationC2);
        }

        [Fact]
        public void TestNormal()
        {
            var normalD1 = new NormalD();

            RuntimeServPools.TryAddSingleton(normalD1);

            var normalD2 = RuntimeServPools.Singleton<NormalD>();

            Assert.Equal(normalD1, normalD2);
        }
    }
}
