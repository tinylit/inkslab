namespace Inkslab.Map
{
    /// <summary>
    /// 配置。
    /// </summary>
    public interface IConfiguration
    {
        /// <summary>
        /// 是否深度映射，若深度映射，每次生成一份新的数据；否则，可以对目标类型赋值时，会直接返回源数据。
        /// </summary>
        bool IsDepthMapping { get; }

        /// <summary>
        /// 是否允许空值传播，若允许，会将空值映射到目标对象上；否则，忽略目标对象属性的赋值或（根节点时）抛出 <see cref="System.InvalidCastException"/> 异常。
        /// </summary>
        bool AllowPropagationNullValues { get; }
    }
}