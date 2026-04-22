using System;
using System.Collections.Generic;
using System.Threading;
using Xunit;

namespace Inkslab.Tests
{
    /// <summary>
    /// <see cref="XStartup"/> 单元测试。启动配置类不考虑并发场景。
    /// </summary>
    public class XStartupTests
    {
        /// <summary>
        /// 同一类型重复调用 <see cref="XStartup.DoStartup"/> 不会再次执行启动。
        /// </summary>
        [Fact]
        public void DoStartup_CalledTwice_InvokesOnlyOnce()
        {
            RepeatStartup.Reset();

            var types = new[] { typeof(RepeatStartup) };

            using var startup = new XStartup(types);
            startup.DoStartup();
            startup.DoStartup();

            Assert.Equal(1, RepeatStartup.StartupCount);
        }

        /// <summary>
        /// 相同 <see cref="IStartup.Code"/> 时，仅执行权重最大的启动项。
        /// </summary>
        [Fact]
        public void DoStartup_SameCode_PicksHighestWeight()
        {
            WeightedStartupLow.Reset();
            WeightedStartupHigh.Reset();

            var types = new[] { typeof(WeightedStartupLow), typeof(WeightedStartupHigh) };

            using var startup = new XStartup(types);
            startup.DoStartup();

            Assert.Equal(0, WeightedStartupLow.StartupCount);
            Assert.Equal(1, WeightedStartupHigh.StartupCount);
        }

        /// <summary>
        /// <see cref="XStartup"/> 传入 null 类型集合时抛出异常。
        /// </summary>
        [Fact]
        public void Ctor_NullTypes_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new XStartup((IEnumerable<Type>)null));
        }
    }

    /// <summary>
    /// 低权重启动项。
    /// </summary>
    public class WeightedStartupLow : IStartup
    {
        private static int _startupCount;

        /// <summary>
        /// 被启动次数。
        /// </summary>
        public static int StartupCount => _startupCount;

        /// <summary>
        /// 重置计数。
        /// </summary>
        public static void Reset() => Interlocked.Exchange(ref _startupCount, 0);

        /// <inheritdoc />
        public int Code => 100;

        /// <inheritdoc />
        public int Weight => 1;

        /// <inheritdoc />
        public void Startup() => Interlocked.Increment(ref _startupCount);
    }

    /// <summary>
    /// 高权重启动项。
    /// </summary>
    public class WeightedStartupHigh : IStartup
    {
        private static int _startupCount;

        /// <summary>
        /// 被启动次数。
        /// </summary>
        public static int StartupCount => _startupCount;

        /// <summary>
        /// 重置计数。
        /// </summary>
        public static void Reset() => Interlocked.Exchange(ref _startupCount, 0);

        /// <inheritdoc />
        public int Code => 100;

        /// <inheritdoc />
        public int Weight => 10;

        /// <inheritdoc />
        public void Startup() => Interlocked.Increment(ref _startupCount);
    }

    /// <summary>
    /// 用于验证重复调用 <see cref="XStartup.DoStartup"/> 只执行一次的启动项。
    /// </summary>
    public class RepeatStartup : IStartup
    {
        private static int _startupCount;

        /// <summary>
        /// 被启动次数。
        /// </summary>
        public static int StartupCount => _startupCount;

        /// <summary>
        /// 重置计数。
        /// </summary>
        public static void Reset() => Interlocked.Exchange(ref _startupCount, 0);

        /// <inheritdoc />
        public int Code => 200;

        /// <inheritdoc />
        public int Weight => 0;

        /// <inheritdoc />
        public void Startup() => Interlocked.Increment(ref _startupCount);
    }
}
