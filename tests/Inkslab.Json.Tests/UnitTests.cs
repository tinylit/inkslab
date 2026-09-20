using Inkslab.Annotations;
using Inkslab.Serialize.Json;
using Newtonsoft.Json;
using System;
using System.Diagnostics;
using Xunit;

namespace Inkslab.Json.Tests
{
    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public class A
    {
        /// <summary>
        /// �����л�������ԡ�
        /// </summary>
        [Ignore]
        public int A1 { get; set; } = 100;

        /// <summary>
        /// <see cref="A2"/>
        /// </summary>
        [Annotations.JsonProperty("C1")]
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
    /// <inheritdoc/>
    /// </summary>
    public class B
    {
        /// <summary>
        /// �����л�������ԡ�
        /// </summary>
        [Ignore]
        public int A1 { get; set; } = 100;

        /// <summary>
        /// <see cref="C1"/>
        /// </summary>
        public int C1 { get; set; }

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
    /// ��Ԫ���ԡ�
    /// </summary>
    public class UnitTests
    {
        /// <summary>
        /// ����Ĭ��Jsonת������
        /// </summary>
        [Fact]
        public void TestDef()
        {
            //+ �������ã�����Nuget���򹤳����ü���ʹ�á�
            StartupFixture.EnsureInitialized();

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

        /// <summary>
        /// �����Զ���ת������
        /// </summary>
        [Fact]
        public void TestCus()
        {
            SingletonPools.TryAdd(new JsonSerializerSettings
            {
                DateFormatHandling = DateFormatHandling.MicrosoftDateFormat,
                DateFormatString = "yyyy-MM-dd", //? ָ��ʱ���ʽΪ��yyyy-MM-dd
                NullValueHandling = NullValueHandling.Ignore
            });

            //+ �������ã�����Nuget���򹤳����ü���ʹ�á�
            StartupFixture.EnsureInitialized();

            var a = new A
            {
                A1 = 200,
                A2 = 100,
                A3 = null,
                A4 = DateTime.Now
            };

            var json = JsonHelper.ToJson(a, NamingType.SnakeCase);

            Debug.WriteLine(json);

            var a1 = JsonHelper.Json<B>(json, NamingType.SnakeCase);

            //? ��Ǻ��Ե����ԣ�Ŀ��ʹ��Ĭ��ֵ��
            Assert.NotEqual(a.A1, a1.A1);

            Assert.Equal(a.A2, a1.C1);

            //? null �����ԣ�Ŀ��ʹ��Ĭ��ֵ��
            Assert.NotEqual(a.A3, a1.A3);
        }
    }
}
