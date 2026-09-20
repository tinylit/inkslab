using Inkslab.Settings;
using System;
using System.Diagnostics;
using System.Globalization;
using Xunit;

namespace Inkslab.Tests
{
    /// <summary>
    /// <see cref="StringExtensions"/> 测试。
    /// </summary>
    public class StringExtensionsTests
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
                var r = "${测试中文}：${i}+${j}=${i + j}".StringSugar(new
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
        /// 语法糖测试。
        /// </summary>
        [Fact]
        public void StringFormatTest()
        {
            DateTimeKind? i = DateTimeKind.Utc;
            var date = DateTime.Now;
            int j = 2;
            var 测试中文 = "方程式";

            var q = $"{测试中文}：{i}+{j},{i:D},{date:yyyy MM dd},{测试中文 + date.ToString("yyyy")}";//{测试中文 + date:yyyy} 有bug，生成内容为“方程式2023/7/8 10:24”, 与预期的“方程式2023”不符合。

            Stopwatch stopwatch = Stopwatch.StartNew();

            for (int k = 0; k < 100000; k++)
            {
                var r = "${测试中文}：${i}+${j},${i:D},${ date:yyyy MM dd },${ 测试中文 + date:yyyy }".StringSugar(new
                {
                    i,
                    j,
                    date,
                    测试中文
                });

                Assert.True(r == q);
            }

            stopwatch.Stop();
        }

        /// <summary>
        /// 语法糖测试。
        /// </summary>
        [Fact]
        public void StringPreserveSyntaxTest()
        {
            DateTimeKind? i = DateTimeKind.Utc;
            var date = new DateTime(2023, 10, 23);
            int j = 2;
            var 测试中文 = "方程式";

            DefaultSettings settings = new DefaultSettings
            {
                Strict = false,
                PreserveSyntax = true
            };

            Stopwatch stopwatch = Stopwatch.StartNew();

            for (int k = 0; k < 100000; k++)
            {
                var r = "${测试中文}：${i}+${j},${i:D},${ date:yyyy MM dd },${ 测试中文 + date:yyyy } ${ TestEnglish }".StringSugar(new
                {
                    i,
                    j,
                    date,
                    测试中文
                }, settings);

                Assert.Equal("方程式：Utc+2,1,2023 10 23,方程式2023 ${ TestEnglish }", r);
            }

            stopwatch.Stop();
        }

        /// <summary>
        /// 语法糖测试。
        /// </summary>
        [Fact]
        public void StringPropertySyntaxTest()
        {
            DateTimeKind? i = DateTimeKind.Utc;
            var date = new DateTime(2023, 10, 23);
            int j = 2;
            var 测试中文 = "方程式";

            DefaultSettings settings = new DefaultSettings
            {
                Strict = false,
                PreserveSyntax = true
            };

            Stopwatch stopwatch = Stopwatch.StartNew();

            for (int k = 0; k < 100000; k++)
            {
                var r = "${测试中文}：${i}+${j},${i:D},${ date.Ticks },${ 测试中文 + date:yyyy }".StringSugar(new
                {
                    i,
                    j,
                    date,
                    测试中文
                }, settings);

                Assert.Equal($"方程式：Utc+2,1,{date.Ticks},方程式2023", r);
            }

            stopwatch.Stop();
        }

        /// <summary>
        /// 语法糖测试。
        /// </summary>
        [Fact]
        public void StringLengthTest()
        {
            DateTimeKind? i = DateTimeKind.Utc;
            var date = new DateTime(2023, 10, 23);
            int j = 2;
            var 测试中文 = "方程式";

            DefaultSettings settings = new DefaultSettings
            {
                Strict = false,
                PreserveSyntax = true
            };

            Stopwatch stopwatch = Stopwatch.StartNew();

            for (int k = 0; k < 100000; k++)
            {
                var r = "${测试中文:#}：${i}+${j},${i:D},${ date.Ticks },${ 测试中文 + date:yyyy }".StringSugar(new
                {
                    i,
                    j,
                    date,
                    测试中文
                }, settings);

                Assert.Equal($"{测试中文.Length}：Utc+2,1,{date.Ticks},方程式2023", r);
            }

            stopwatch.Stop();
        }

        /// <summary>
        /// 语法糖测试。
        /// </summary>
        [Fact]
        public void StringIndexTest()
        {
            DateTimeKind? i = DateTimeKind.Utc;
            var date = new DateTime(2023, 10, 23);
            int j = 2;
            var 测试中文 = "方程式非常快";

            DefaultSettings settings = new DefaultSettings
            {
                Strict = false,
                PreserveSyntax = true
            };

            Stopwatch stopwatch = Stopwatch.StartNew();

            for (int k = 0; k < 100000; k++)
            {
                var r = "${测试中文:..1}：${i}+${j},${i:D},${ date.Ticks },${测试中文:1..1}+${测试中文:1..}+${测试中文:1..-1}+${测试中文:..-2}".StringSugar(new
                {
                    i,
                    j,
                    date,
                    测试中文
                }, settings);

                Assert.Equal($"{测试中文[..1]}：Utc+2,1,{date.Ticks},{测试中文[1..1]}+{测试中文[1..]}+{测试中文[1..(测试中文.Length - 1)]}+{测试中文[..(测试中文.Length - 2)]}", r);
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
        [InlineData("naming-case", NamingType.Normal, "naming-case")]
        [InlineData("namingCase", NamingType.PascalCase, "NamingCase")]
        [InlineData("NamingCase", NamingType.PascalCase, "NamingCase")]
        [InlineData("naming_case", NamingType.PascalCase, "NamingCase")]
        [InlineData("naming-case", NamingType.PascalCase, "NamingCase")]
        [InlineData("namingCase", NamingType.CamelCase, "namingCase")]
        [InlineData("NamingCase", NamingType.CamelCase, "namingCase")]
        [InlineData("naming_case", NamingType.CamelCase, "namingCase")]
        [InlineData("naming-case", NamingType.CamelCase, "namingCase")]
        [InlineData("namingCase", NamingType.SnakeCase, "naming_case")]
        [InlineData("NamingCase", NamingType.SnakeCase, "naming_case")]
        [InlineData("naming_case", NamingType.SnakeCase, "naming_case")]
        [InlineData("naming-case", NamingType.SnakeCase, "naming_case")]
        [InlineData("namingCase", NamingType.KebabCase, "naming-case")]
        [InlineData("NamingCase", NamingType.KebabCase, "naming-case")]
        [InlineData("naming_case", NamingType.KebabCase, "naming-case")]
        [InlineData("naming-case", NamingType.KebabCase, "naming-case")]
        public void NamingTest(string name, NamingType namingType, string naming)
        {
            var r = name.ToNamingCase(namingType);

            Assert.Equal(naming, r);
        }

        /// <summary>
        /// 命名转换必须在每次调用时一致地使用当前文化。
        /// </summary>
        [Fact]
        public void NamingCase_UsesCurrentCultureAfterCultureChanges()
        {
            var originalCulture = CultureInfo.CurrentCulture;

            try
            {
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
                Assert.Equal("NameI", "name_i".ToPascalCase());

                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("tr-TR");
                Assert.Equal("Name\u0130", "name_i".ToPascalCase());

                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
                Assert.Equal("NameI", "name_i".ToPascalCase());
            }
            finally
            {
                CultureInfo.CurrentCulture = originalCulture;
            }
        }

        /// <summary>
        /// 名称仅拒绝协议约定的五种空白字符。
        /// </summary>
        [Theory]
        [InlineData("a b")]
        [InlineData("a\tb")]
        [InlineData("a\rb")]
        [InlineData("a\nb")]
        [InlineData("a\fb")]
        public void NamingCase_RejectsOnlyConfiguredAsciiWhitespaces(string name)
        {
            Assert.Throws<ArgumentException>(() => name.ToNamingCase(NamingType.Normal));
        }

        /// <summary>
        /// 垂直制表符和不换行空格不属于原有拒绝字符集。
        /// </summary>
        [Theory]
        [InlineData("a\vb")]
        [InlineData("a\u00A0b")]
        public void NamingCase_AcceptsOtherUnicodeWhitespaces(string name)
        {
            Assert.Equal(name, name.ToNamingCase(NamingType.Normal));
        }
    }
}
