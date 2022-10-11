using Inkslab.Annotations;
using Inkslab.Serialize.Json;
using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Xunit;

namespace Inkslab.Json.Tests
{
    public class A
    {
        //? �����л�������ԡ�
        [Ignore]
        public int A1 { get; set; } = 100;

        public int A2 { get; set; }

        public string A3 { get; set; } = string.Empty;

        public DateTime A4 { get; set; }
    }

    public class UnitTests
    {
        [Fact]
        public void TestDef()
        {
            //+ �������ã����Nuget���򹤳����ü���ʹ�á�
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

            //? �������л������Է����л�ʱ��ʹ��Ĭ��ֵ��
            Assert.NotEqual(a.A1, a1.A1);

            Assert.Equal(a.A2, a1.A2);

            Assert.Equal(a.A3, a1.A3);
        }

        [Fact]
        public void TestCus()
        {
            RuntimeServPools.TryAddSingleton(new JsonSerializerSettings
            {
                DateFormatHandling = DateFormatHandling.MicrosoftDateFormat,
                DateFormatString = "yyyy-MM-dd", //? ָ��ʱ���ʽΪ��yyyy-MM-dd
                NullValueHandling = NullValueHandling.Ignore
            });

            //+ �������ã����Nuget���򹤳����ü���ʹ�á�
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

            //? ��Ǻ��Ե����ԣ�Ŀ��ʹ��Ĭ��ֵ��
            Assert.NotEqual(a.A1, a1.A1);

            Assert.Equal(a.A2, a1.A2);

            //? null �����ԣ�Ŀ��ʹ��Ĭ��ֵ��
            Assert.NotEqual(a.A3, a1.A3);
        }
    }
}
