namespace Insklab.Config
{
    /// <summary>
    /// 服务配置帮助类。
    /// </summary>
    public interface IConfigHelper
    {
        /// <summary>
        /// 配置读取。
        /// </summary>
        /// <typeparam name="T">读取数据类型。</typeparam>
        /// <param name="key">健。</param>
        /// <param name="defaultValue">默认值。</param>
        /// <returns></returns>
        T Get<T>(string key, T defaultValue = default);
    }
}
