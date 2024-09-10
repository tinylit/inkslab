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
        private readonly Action<IConfigurationBuilder> _configuration;

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="configuration">配置。</param>
        /// <exception cref="ArgumentNullException">参数 <paramref name="configuration"/> is null.</exception>
        public JsonConfigSettings(Action<IConfigurationBuilder> configuration)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        /// <summary>
        /// 配置。
        /// </summary>
        /// <param name="configurationBuilder">配置构建器。</param>
        public void Config(IConfigurationBuilder configurationBuilder) => _configuration.Invoke(configurationBuilder);
    }
}
#endif