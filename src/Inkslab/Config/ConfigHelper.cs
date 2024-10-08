﻿using System;

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

        /// <summary>
        /// 默认配置助手。
        /// </summary>
        private class DefaultConfigHelper : IConfigHelper
        {
#if !NET_Traditional
            /// <summary> 配置文件变更事件。 </summary>
            event Action<object> IConfigHelper.OnConfigChanged { add { } remove { } }
#endif

            T IConfigHelper.Get<T>(string key, T defaultValue) => defaultValue;
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
    }
}
