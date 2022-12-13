using System;
using System.Diagnostics;
using Xunit;

namespace Inkslab.Tests
{
    /// <summary>
    /// <see cref="StringExtentions"/> 测试。
    /// </summary>
    public class StringExtentionsTests
    {
        /// <summary>
        /// 语法糖测试。
        /// </summary>
        [Fact]
        public void StringSugarTest()
        {
            int? i = 1;
            int j = 2;
            var 测试中文 = "方程式";

            var q = $"{测试中文}：{i}+{j}={i + j}";

            Stopwatch stopwatch = Stopwatch.StartNew();

            for (int k = 0; k < 100000; k++)
            {
                var r = "{测试中文}：{i}+{j}={i + j}".StringSugar(new
                {
                    i,
                    j,
                    测试中文
                });

                Assert.True(r == q);
            }

            stopwatch.Stop();
        }

        /// <summary>
        /// 命名测试。
        /// </summary>
        /// <param name="name">原名称。</param>
        /// <param name="namingType">命名方式。</param>
        /// <param name="naming">命名后的名称。</param>
        [Theory]
        [InlineData("namingCase", NamingType.Normal, "namingCase")]
        [InlineData("NamingCase", NamingType.Normal, "NamingCase")]
        [InlineData("naming_case", NamingType.Normal, "naming_case")]
        [InlineData("namingCase", NamingType.PascalCase, "NamingCase")]
        [InlineData("NamingCase", NamingType.PascalCase, "NamingCase")]
        [InlineData("naming_case", NamingType.PascalCase, "NamingCase")]
        [InlineData("namingCase", NamingType.CamelCase, "namingCase")]
        [InlineData("NamingCase", NamingType.CamelCase, "namingCase")]
        [InlineData("naming_case", NamingType.CamelCase, "namingCase")]
        [InlineData("namingCase", NamingType.UrlCase, "naming_case")]
        [InlineData("NamingCase", NamingType.UrlCase, "naming_case")]
        [InlineData("naming_case", NamingType.UrlCase, "naming_case")]
        public void NamingTest(string name, NamingType namingType, string naming)
        {
            var r = name.ToNamingCase(namingType);

            Assert.Equal(naming, r);
        }
    }
}
