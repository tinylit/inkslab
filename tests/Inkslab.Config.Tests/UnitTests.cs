using Inkslab.Config.Settings;
using System;
using Xunit;

namespace Inkslab.Config.Tests
{
    /// <summary>
    /// ���ִ�С�
    /// </summary>
    public class UnitTests
    {
        /// <summary>
        /// Ĭ�ϡ�
        /// </summary>
        [Fact]
        public void TestDef()
        {
            //+ �������ã����Nuget���򹤳����ü���ʹ�á�
            using (var xstartup = new XStartup())
            {
                xstartup.DoStartup();
            }

            var equal = "Production";

            //? ���������Զ����� appsettings.json �ļ���
            //!? ��Ŀ�����У�����������ASPNETCORE_ENVIRONMENT��Ϊ����ģʽ��Development��ʱ����ͬʱ���� appsettings.json �� appsettings.Development.json �ļ����� appsettings.Development.json ���ȼ����ߡ�
            var environment = "Environment".Config<string>();

            Assert.Equal(environment, equal);
        }

        /// <summary>
        /// �Զ��塣
        /// </summary>
        [Fact]
        public void TestCus()
        {
            //+ �������ã����Nuget���򹤳����ü���ʹ�á�
            using (var xstartup = new XStartup())
            {
                xstartup.DoStartup();
            }

            SingletonPools.TryAdd(new JsonConfigSettings(x =>
            {
                //TODO: �����������ʼ�����á�
            }));

            var equal = "Production";

            //? ���������Զ����� appsettings.json �ļ���
            //!? ��Ŀ�����У�����������ASPNETCORE_ENVIRONMENT��Ϊ����ģʽ��Development��ʱ����ͬʱ���� appsettings.json �� appsettings.Development.json �ļ����� appsettings.Development.json ���ȼ����ߡ�
            var environment = "Environment".Config<string>();

            Assert.Equal(environment, equal);
        }
    }
}
