using System;
using System.Threading.Tasks;
using Xunit;

namespace Inkslab.Tests
{
    /// <summary>
    /// EnumExtensions.GetText 缓存幂等性 + 并发安全测试。
    /// </summary>
    public class EnumExtensionsCacheTests
    {
        /// <summary>
        /// 普通枚举多次调用返回一致结果（验证值级缓存命中时不会漂移）。
        /// </summary>
        [Fact]
        public void GetText_NonFlags_CachedResultConsistent()
        {
            var first = EnumDefault.B.GetText();
            var second = EnumDefault.B.GetText();
            var third = EnumDefault.B.GetText();

            Assert.Equal(first, second);
            Assert.Equal(second, third);
        }

        /// <summary>
        /// Flags 枚举组合值多次调用返回一致结果（验证组合值缓存）。
        /// </summary>
        [Fact]
        public void GetText_Flags_Combined_CachedResultConsistent()
        {
            var combined = EnumOperation.A | EnumOperation.B;

            var first = combined.GetText();
            var second = combined.GetText();

            Assert.Equal(first, second);
            Assert.Contains("|", first);
        }

        /// <summary>
        /// 并发调用 GetText 不应抛异常，且返回值一致。
        /// </summary>
        [Fact]
        public async Task GetText_Concurrent_NoException()
        {
            var tasks = new Task[16];
            for (int i = 0; i < tasks.Length; i++)
            {
                tasks[i] = Task.Run(() =>
                {
                    for (int k = 0; k < 1000; k++)
                    {
                        _ = EnumDefault.B.GetText();
                        _ = (EnumOperation.A | EnumOperation.C).GetText();
                    }
                });
            }

            await Task.WhenAll(tasks);
        }
    }
}
