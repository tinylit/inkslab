using Inkslab.Annotations;
#if !NET_Traditional
using System;
#endif

namespace Inkslab.Config
{
    /// <summary>
    /// 服务配置帮助类。
    /// </summary>
    [Ignore]
    public interface IConfigHelper
    {
#if !NET_Traditional
        /// <summary> 配置文件变更事件。 </summary>
        event Action<object> OnConfigChanged;
#endif

        /// <summary>
        /// 配置读取。
        /// </summary>
        /// <typeparam name="T">读取数据类型。</typeparam>
        /// <param name="key">健。</param>
        /// <param name="defaultValue">默认值。</param>
        /// <returns>如果找到 <paramref name="key"/> 对应的值，则返回键值；否则，返回默认值 <paramref name="defaultValue"/> 。</returns>
        T Get<T>(string key, T defaultValue = default);
    }
}
