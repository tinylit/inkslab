using Inkslab.Config.Options;
using System;

namespace Inkslab.Config
{
    /// <summary>
    /// 配置助手。
    /// </summary>
    public static class ConfigHelper
    {
        private static readonly IConfigHelper _configHelper;

        /// <summary>
        /// 静态构造函数。
        /// </summary>
        static ConfigHelper() => _configHelper = SingletonPools.Singleton<IConfigHelper, DefaultConfigHelper>();

#if !NET_Traditional
        /// <summary>
        /// 配置。
        /// </summary>
        /// <typeparam name="T">类型。</typeparam>
        private class DefaultOptions<T> : IOptions<T> where T : class
        {
            private readonly IConfigHelper config;
            private readonly string key;
            private readonly T defaultValue;

            public DefaultOptions(IConfigHelper config, string key, T defaultValue = default)
            {
                this.config = config;
                this.key = key;
                this.defaultValue = defaultValue;
            }

            public T Value => config.Get(key, defaultValue);
        }
#endif

        /// <summary>
        /// 默认配置助手。
        /// </summary>
        private class DefaultConfigHelper : IConfigHelper
        {
#if !NET_Traditional
            /// <summary> 配置文件变更事件。 </summary>
            public event Action<object> OnConfigChanged { add { } remove { } }
#endif

            public T Get<T>(string key, T defaultValue = default) => defaultValue;
        }

#if !NET_Traditional
        /// <summary> 配置文件变更事件。 </summary>
        public static event Action<object> OnConfigChanged { add { _configHelper.OnConfigChanged += value; } remove { _configHelper.OnConfigChanged -= value; } }
#endif

        /// <summary>
        /// 配置读取。
        /// </summary>
        /// <typeparam name="T">读取数据类型。</typeparam>
        /// <param name="key">健。</param>
        /// <param name="defaultValue">默认值。</param>
        /// <returns></returns>
        public static T Get<T>(string key, T defaultValue = default) => _configHelper.Get(key, defaultValue);

#if !NET_Traditional
        /// <summary>
        /// 获取配置。
        /// </summary>
        /// <typeparam name="T">类型。</typeparam>
        /// <param name="key">键。</param>
        /// <param name="defaultValue">默认值。</param>
        /// <returns></returns>
        public static IOptions<T> Options<T>(string key, T defaultValue = default) where T : class => new DefaultOptions<T>(_configHelper, key, defaultValue);
#endif
    }
}
