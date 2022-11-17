namespace Inkslab.Map
{
    /// <summary>
    /// 配置文件。
    /// </summary>
    public interface IProfile
    {
        /// <summary>
        /// 映射规则。
        /// </summary>
        /// <typeparam name="TSource">源类型。</typeparam>
        /// <typeparam name="TDestination">目标类型。</typeparam>
        IMappingExpression<TSource, TDestination> Map<TSource, TDestination>() 
            where TSource : class 
            where TDestination : class;
    }
}
