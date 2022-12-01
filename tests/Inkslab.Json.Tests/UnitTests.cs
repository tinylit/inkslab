using Inkslab.Annotations;
using Inkslab.Serialize.Json;
using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Xunit;

namespace Inkslab.Json.Tests
{
    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public class A
    {
        /// <summary>
        /// 不序列化这个属性。
        /// </summary>
        [Ignore]
        public int A1 { get; set; } = 100;

        /// <summary>
        /// <see cref="A2"/>
        /// </summary>
        public int A2 { get; set; }

        /// <summary>
        /// <see cref="A3"/>
        /// </summary>
        public string A3 { get; set; } = string.Empty;

        /// <summary>
        /// <see cref="A4"/>
        /// </summary>
        public DateTime A4 { get; set; }
    }

    /// <summary>
    /// 单元测试。
    /// </summary>
    public class UnitTests
    {
        /// <summary>
        /// 测试默认Json转换器。
        /// </summary>
        [Fact]
        public void TestDef()
        {
            //+ 引包即用：添加Nuget包或工程引用即可使用。
            using (var xstartup = new XStartup())
            {
                xstartup.DoStartup();
            }

            var a = new A
            {
                A1 = 200,
                A2 = 100,
                A3 = "A3",
                A4 = DateTime.Now
            };

            var json = JsonHelper.ToJson(a);

            Debug.WriteLine(json);

            var a1 = JsonHelper.Json<A>(json);

            //? 不被序列化，所以反序列化时，使用默认值。
            Assert.NotEqual(a.A1, a1.A1);

            Assert.Equal(a.A2, a1.A2);

            Assert.Equal(a.A3, a1.A3);
        }

        /// <summary>
        /// 测试自定义转换器。
        /// </summary>
        [Fact]
        public void TestCus()
        {
            RuntimeServPools.TryAddSingleton(new JsonSerializerSettings
            {
                DateFormatHandling = DateFormatHandling.MicrosoftDateFormat,
                DateFormatString = "yyyy-MM-dd", //? 指定时间格式为：yyyy-MM-dd
                NullValueHandling = NullValueHandling.Ignore
            });

            //+ 引包即用：添加Nuget包或工程引用即可使用。
            using (var xstartup = new XStartup())
            {
                xstartup.DoStartup();
            }

            var a = new A
            {
                A1 = 200,
                A2 = 100,
                A3 = null,
                A4 = DateTime.Now
            };

            var json = JsonHelper.ToJson(a);

            Debug.WriteLine(json);

            var a1 = JsonHelper.Json<A>(json);

            //? 标记忽略的属性，目标使用默认值。
            Assert.NotEqual(a.A1, a1.A1);

            Assert.Equal(a.A2, a1.A2);

            //? null 被忽略，目标使用默认值。
            Assert.NotEqual(a.A3, a1.A3);
        }
    }
}
