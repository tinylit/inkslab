using Inkslab.Serialize.Xml;
using Xunit;

namespace Inkslab.Tests
{
    /// <summary>
    /// <see cref="CData"/> 相等性测试（含 value 为 null 的边界）。
    /// </summary>
    public class CDataTests
    {
        /// <summary>
        /// default(CData)（value 为 null）与字符串比较不应抛异常。
        /// </summary>
        [Fact]
        public void DefaultCData_EqualsString_ShouldNotThrow()
        {
            CData a = default;

            Assert.False(a.Equals("x"));
        }

        /// <summary>
        /// default(CData) 与另一 CData 比较不应抛异常。
        /// </summary>
        [Fact]
        public void DefaultCData_EqualsCData_ShouldNotThrow()
        {
            CData a = default;
            CData b = "x";

            Assert.False(a.Equals(b));
        }

        /// <summary>
        /// 两个 value 为 null 的 CData 应相等。
        /// </summary>
        [Fact]
        public void TwoNullCData_ShouldBeEqual()
        {
            CData a = default;
            CData b = default;

            Assert.True(a.Equals(b));
        }

        /// <summary>
        /// == 运算符在左值 value 为 null 时不应抛异常。
        /// </summary>
        [Fact]
        public void EqualityOperator_WithNullLeft_ShouldNotThrow()
        {
            CData a = default;
            CData b = "x";

            Assert.False(a == b);
            Assert.True(a != b);
        }

        /// <summary>
        /// 非空 CData 的相等比较保持正确。
        /// </summary>
        [Fact]
        public void NonNullCData_EqualsByValue()
        {
            CData a = "abc";
            CData b = "abc";

            Assert.True(a.Equals(b));
            Assert.True(a.Equals("abc"));
            Assert.False(a.Equals("def"));
        }
    }
}
