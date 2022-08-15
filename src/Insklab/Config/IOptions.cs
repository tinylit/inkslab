namespace Insklab.Config
{
    /// <summary>
    /// 用于检索已配置的 <typeparamref name="TOptions"/> 实例。
    /// </summary>
    /// <typeparam name="TOptions">配置类型。</typeparam>
    public interface IOptions<out TOptions> where TOptions : class
    {
        /// <summary>
        /// 配置值。
        /// </summary>
        TOptions Value { get; }
    }
}
