#if !NET_Traditional
using Microsoft.Extensions.Configuration;
using System;

namespace Inkslab.Config.Settings
{
    /// <summary>
    /// Json 配置设置。
    /// </summary>
    public sealed class JsonConfigSettings : IJsonConfigSettings
    {
        private readonly Action<IConfigurationBuilder> configuration;

        public JsonConfigSettings(Action<IConfigurationBuilder> configuration)
        {
            this.configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        /// <summary>
        /// 配置。
        /// </summary>
        /// <param name="configurationBuilder">配置构建器。</param>
        public void Config(IConfigurationBuilder configurationBuilder) => configuration.Invoke(configurationBuilder);
    }
}
#endif