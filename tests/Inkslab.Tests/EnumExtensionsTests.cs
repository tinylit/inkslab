using System;
using System.ComponentModel;
using Xunit;

namespace Inkslab.Tests
{
    /// <summary>
    /// 默认类型。
    /// </summary>
    public enum EnumDefault
    {
        /// <summary>
        /// A
        /// </summary>
        A,
        /// <summary>
        /// B
        /// </summary>
        [Description("常规B")]
        B,
        /// <summary>
        /// C
        /// </summary>
        C
    }

    /// <summary>
    /// 位运算。
    /// </summary>
    [Flags]
    public enum EnumOperation
    {
        /// <summary>
        /// A
        /// </summary>
        A = 1 << 0,
        /// <summary>
        /// B
        /// </summary>
        [Description("位运算B")]
        B = 1 << 1,
        /// <summary>
        /// C
        /// </summary>
        C = 1 << 2
    }

    /// <summary>
    /// 大类型。
    /// </summary>
    public enum EnumBig : long
    {
        /// <summary>
        /// A
        /// </summary>
        A = 1,
        /// <summary>
        /// B
        /// </summary>
        [Description("大类型B")]
        B = 2,
        /// <summary>
        /// C
        /// </summary>
        C = 3
    }

    /// <summary>
    /// <see cref="EnumExtensions"/> 测试。
    /// </summary>
    public class EnumExtensionsTests
    {
        /// <summary>
        /// 获取文本。
        /// </summary>
        [Theory]
        [InlineData(EnumDefault.A, "A")]
        [InlineData(EnumDefault.B, "常规B")]
        [InlineData(EnumDefault.C, "C")]
        public void GetTextOfDefault(EnumDefault @enum, string text)
        {
            var txt = @enum.GetText();

            Assert.Equal(text, txt);
        }

        /// <summary>
        /// 获取文本。
        /// </summary>
        [Theory]
        [InlineData(EnumOperation.A, "A")]
        [InlineData(EnumOperation.B, "位运算B")]
        [InlineData(EnumOperation.C, "C")]
        [InlineData(EnumOperation.A | EnumOperation.B, "A|位运算B")]
        public void GetTextOfOperation(EnumOperation @enum, string text)
        {
            var txt = @enum.GetText();

            Assert.Equal(text, txt);
        }

        /// <summary>
        /// 测试获取枚举值。
        /// </summary>
        [Theory]
        [InlineData(EnumDefault.A, 0)]
        [InlineData(EnumDefault.B, 1)]
        [InlineData(EnumDefault.C, 2)]
        public void ToInt32(EnumDefault @enum, int value)
        {
            int i32 = @enum.ToInt32();

            Assert.Equal(value, i32);
        }

        /// <summary>
        /// 测试获取枚举值。
        /// </summary>
        [Theory]
        [InlineData(EnumOperation.A, 1)]
        [InlineData(EnumOperation.B, 2)]
        [InlineData(EnumOperation.C, 4)]
        [InlineData(EnumOperation.A | EnumOperation.C, 5)]
        public void ToInt32OfOperatio(EnumOperation @enum, int value)
        {
            int i32 = @enum.ToInt32();

            Assert.Equal(value, i32);
        }

        /// <summary>
        /// 测试获取枚举值。
        /// </summary>
        [Theory]
        [InlineData(EnumDefault.A, "0")]
        [InlineData(EnumDefault.B, "1")]
        [InlineData(EnumDefault.C, "2")]
        public void ToValueString(EnumDefault @enum, string value)
        {
            string s = @enum.ToValueString();

            Assert.Equal(value, s);
        }

        /// <summary>
        /// 测试获取枚举值。
        /// </summary>
        [Theory]
        [InlineData(EnumOperation.A, "1")]
        [InlineData(EnumOperation.B, "2")]
        [InlineData(EnumOperation.C, "4")]
        [InlineData(EnumOperation.A | EnumOperation.C, "5")]
        public void ToValueStringOfOperatio(EnumOperation @enum, string value)
        {
            string s = @enum.ToValueString();

            Assert.Equal(value, s);
        }

        /// <summary>
        /// 测试获取多个值。如果没有标记<see cref="FlagsAttribute"/>的枚举，只返回匹配被声明枚举值的唯一一个元素的数组，或空数组。
        /// </summary>
        /// <param name="enum"></param>
        /// <param name="operations"></param>
        [Theory]
        [InlineData(EnumOperation.A, new EnumOperation[1] { EnumOperation.A })]
        [InlineData(EnumOperation.B, new EnumOperation[1] { EnumOperation.B })]
        [InlineData(EnumOperation.C, new EnumOperation[1] { EnumOperation.C })]
        [InlineData(EnumOperation.A | EnumOperation.C, new EnumOperation[2] { EnumOperation.A, EnumOperation.C })]
        public void ToValues(EnumOperation @enum, EnumOperation[] operations)
        {
            EnumOperation[] enums = @enum.ToValues();

            Assert.Equal(enums, operations);
        }
    }
}
