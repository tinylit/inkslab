using System;

namespace Inkslab.Config
{
    /// <summary>
    /// 配置助手。
    /// </summary>
    public static class ConfigHelper
    {
        private static readonly IConfigHelper configHelper;

        /// <summary>
        /// 静态构造函数。
        /// </summary>
        static ConfigHelper() => configHelper = SingletonPools.Singleton<IConfigHelper, DefaultConfigHelper>();

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
        public static event Action<object> OnConfigChanged { add { configHelper.OnConfigChanged += value; } remove { configHelper.OnConfigChanged -= value; } }
#endif

        /// <summary>
        /// 配置读取。
        /// </summary>
        /// <typeparam name="T">读取数据类型。</typeparam>
        /// <param name="key">健。</param>
        /// <param name="defaultValue">默认值。</param>
        /// <returns></returns>
        public static T Get<T>(string key, T defaultValue = default) => configHelper.Get(key, defaultValue);
    }
}
