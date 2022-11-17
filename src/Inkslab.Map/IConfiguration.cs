namespace Inkslab.Map
{
    /// <summary>
    /// 配置。
    /// </summary>
    public interface IConfiguration
    {
        /// <summary>
        /// 深度映射。
        /// </summary>
        bool IsDepthMapping { get; }

        /// <summary>
        /// 允许空值传播。
        /// </summary>
        bool AllowPropagationNullValues { get; }
    }
}