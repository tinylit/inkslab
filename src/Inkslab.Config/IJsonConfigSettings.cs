#if !NET_Traditional
using Microsoft.Extensions.Configuration;

namespace Inkslab.Config
{
    /// <summary>
    /// Json 配置设置。
    /// </summary>
    public interface IJsonConfigSettings
    {
        /// <summary>
        /// 配置。
        /// </summary>
        /// <param name="configurationBuilder">配置构建器。</param>
        void Config(IConfigurationBuilder configurationBuilder);
    }
}
#endif