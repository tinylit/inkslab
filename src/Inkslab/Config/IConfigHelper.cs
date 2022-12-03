using Inkslab.Annotations;

namespace Inkslab.Config
{
    /// <summary>
    /// 服务配置帮助类。
    /// </summary>
    [Ignore]
    public interface IConfigHelper
    {
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
