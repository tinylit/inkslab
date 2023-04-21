using System;
using System.Linq.Expressions;

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
        /// <returns>映射表达式。</returns>
        IMappingExpression<TSource, TDestination> Map<TSource, TDestination>()
            where TSource : class
            where TDestination : class;

        /// <summary>
        /// 实例化。
        /// </summary>
        /// <typeparam name="TSource">源类型。</typeparam>
        /// <typeparam name="TDestination">目标类型。</typeparam>
        /// <param name="destinationOptions">创建实例的表达式。</param>
        /// <returns>映射表达式。</returns>
        IMappingExpressionBase<TSource, TDestination> New<TSource, TDestination>(Expression<Func<TSource, TDestination>> destinationOptions)
            where TSource : class
            where TDestination : class;
    }
}
