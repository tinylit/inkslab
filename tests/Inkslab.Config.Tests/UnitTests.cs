using Inkslab.Config.Settings;
using System;
using Xunit;

namespace Inkslab.Config.Tests
{
    /// <summary>
    /// 测试
    /// </summary>
    public class UnitTests
    {
        /// <summary>
        /// 默认测试。
        /// </summary>
        [Fact]
        public void TestDef()
        {
            //+ 热启动。
            using (var startup = new XStartup())
            {
                startup.DoStartup();
            }

            var equal = "Production";

            //? 读取配置文件 appsettings.json 内容。
            var environment = "Environment".Config<string>();

            Assert.Equal(environment, equal);
        }

        /// <summary>
        /// 自定义。
        /// </summary>
        [Fact]
        public void TestCus()
        {
            //+ 热启动。
            using (var startup = new XStartup())
            {
                startup.DoStartup();
            }

            SingletonPools.TryAdd(new JsonConfigSettings(x =>
            {
                // 自定义配置。
            }));

            var equal = "Production";

            //? 读取配置文件 appsettings.json 内容。
            var environment = "Environment".Config<string>();

            Assert.Equal(environment, equal);
        }
    }
}
