using System;
using System.Collections.Generic;
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
        /// <param name="createInstanceExpression">创建实例的表达式。</param>
        /// <returns>映射表达式。</returns>
        IMappingExpressionBase<TSource, TDestination> New<TSource, TDestination>(Expression<Func<TSource, TDestination>> createInstanceExpression)
            where TSource : class
            where TDestination : class;

        /// <summary>
        /// 实例化，支持定义类型(<see cref="Type.IsGenericTypeDefinition"/>)。
        /// </summary>
        /// <param name="newInstanceType">创建实例类型，必须实现 <see cref="INewInstance{TSource, TSourceItem, TDestination, TDestinationItem}"/> 接口。</param>
        void New(Type newInstanceType);
    }

    /// <summary>
    /// 创建实例。
    /// </summary>
    /// <typeparam name="TSource">源类型。</typeparam>
    /// <typeparam name="TSourceItem">源集合元素类型。</typeparam>
    /// <typeparam name="TDestination">目标类型。</typeparam>
    /// <typeparam name="TDestinationItem">目标集合元素类型。</typeparam>
    public interface INewInstance<TSource, TSourceItem, TDestination, TDestinationItem> where TSource : IEnumerable<TSourceItem> where TDestination : IEnumerable<TDestinationItem>
    {
        /// <summary>
        /// 创建实例。
        /// </summary>
        /// <param name="source">源数据。</param>
        /// <param name="destinationItems">目标集合元素数据。</param>
        /// <returns>目标类型实例。</returns>
        TDestination NewInstance(TSource source, List<TDestinationItem> destinationItems);
    }
}
