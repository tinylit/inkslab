using Inkslab.Config.Settings;
using System;
using Xunit;

namespace Inkslab.Config.Tests
{
    /// <summary>
    /// 逐个执行。
    /// </summary>
    public class UnitTests
    {
        [Fact]
        public void TestDef()
        {
            //+ 引包即用：添加Nuget包或工程引用即可使用。
            using (var xstartup = new XStartup())
            {
                xstartup.DoStartup();
            }

            var equal = "Production";

            //? 生产环境自动加载 appsettings.json 文件。
            //!? 项目开发中，环境变量【ASPNETCORE_ENVIRONMENT】为开发模式（Development）时，会同时加载 appsettings.json 和 appsettings.Development.json 文件，且 appsettings.Development.json 优先级更高。
            var environment = "Environment".Config<string>();

            Assert.Equal(environment, equal);
        }

        [Fact]
        public void TestCus()
        {
            //+ 引包即用：添加Nuget包或工程引用即可使用。
            using (var xstartup = new XStartup())
            {
                xstartup.DoStartup();
            }

            RuntimeServPools.TryAddSingleton(new JsonConfigSettings(x =>
            {
                //TODO: 可以在这里初始化配置。
            }));

            var equal = "Production";

            //? 生产环境自动加载 appsettings.json 文件。
            //!? 项目开发中，环境变量【ASPNETCORE_ENVIRONMENT】为开发模式（Development）时，会同时加载 appsettings.json 和 appsettings.Development.json 文件，且 appsettings.Development.json 优先级更高。
            var environment = "Environment".Config<string>();

            Assert.Equal(environment, equal);
        }
    }
}
