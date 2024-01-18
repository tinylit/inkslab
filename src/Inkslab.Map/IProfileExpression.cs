using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Inkslab.Map
{
    /// <summary>
    /// 是否匹配协议。
    /// </summary>
    /// <param name="sourceType">源类型。</param>
    /// <param name="sourceArguments">源类型的泛型参数。</param>
    /// <param name="destinationArguments">目标类型的泛型参数。</param>
    /// <returns>是否匹配。</returns>
    public delegate bool MatchConstraints(Type sourceType, Type[] sourceArguments, Type[] destinationArguments);

    /// <summary>
    /// 基础表达式。
    /// </summary>
    /// <typeparam name="TSource">源。</typeparam>
    /// <typeparam name="TDestination">目标。</typeparam>
    public interface IProfileExpressionBase<TSource, TDestination>
    {
        /// <summary>
        /// 指定成员映射规则。
        /// </summary>
        /// <typeparam name="TMember">成员。</typeparam>
        /// <param name="destinationMember">目标成员。</param>
        /// <param name="memberOptions">成员配置。</param>
        /// <returns></returns>
        IProfileExpressionBase<TSource, TDestination> Map<TMember>(Expression<Func<TDestination, TMember>> destinationMember, Action<IMemberConfigurationExpression<TSource, TMember>> memberOptions);

        /// <summary>
        /// 继承泛型约束：类型 <typeparamref name="TDestination"/> 是型参数的泛型类时，泛型参数作为继承约束类型适配。
        /// </summary>
        /// <param name="matchConstraints">泛型参数是否匹配。</param>
        /// <returns>映射表达式。</returns>
        void IncludeConstraints(MatchConstraints matchConstraints);
    }

    /// <summary>
    /// 映射表达式。
    /// </summary>
    /// <typeparam name="TSource">源。</typeparam>
    /// <typeparam name="TDestination">目标。</typeparam>
    public interface IProfileExpression<TSource, TDestination> : IIncludeProfileExpression<TSource, TDestination>
    {
        /// <summary>
        /// 迭代器映射。
        /// </summary>
        /// <typeparam name="TSourceEnumerable"><typeparamref name="TSource"/>的迭代集合处理。</typeparam>
        /// <typeparam name="TDestinationEnumerable"><typeparamref name="TDestination"/>的迭代集合处理。</typeparam>
        /// <param name="destinationOptions">生成迭代器配置。</param>
        /// <returns>映射表达式。</returns>
        IProfileExpression<TSource, TDestination> NewEnumerable<TSourceEnumerable, TDestinationEnumerable>(Expression<Func<TSourceEnumerable, List<TDestination>, TDestinationEnumerable>> destinationOptions) where TSourceEnumerable : IEnumerable<TSource> where TDestinationEnumerable : IEnumerable<TDestination>;
    }

    /// <summary>
    /// 【包含】映射表达式。
    /// </summary>
    /// <typeparam name="TSource">源。</typeparam>
    /// <typeparam name="TDestination">目标。</typeparam>
    public interface IIncludeProfileExpression<TSource, TDestination> : IProfileExpressionBase<TSource, TDestination>
    {
        /// <summary>
        /// 包含继承 <typeparamref name="TDestination"/> 的类型，指定共享规则。
        /// </summary>
        /// <typeparam name="TAssignableToDestination">可赋值到<typeparamref name="TDestination"/>的类型。</typeparam>
        /// <returns>映射表达式。</returns>
        IIncludeProfileExpression<TSource, TDestination> Include<TAssignableToDestination>() where TAssignableToDestination : TDestination;
    }

    /// <summary>
    /// 成员映射表达式。
    /// </summary>
    /// <typeparam name="TSource">源。</typeparam>
    /// <typeparam name="TMember">成员。</typeparam>
    public interface IMemberConfigurationExpression<TSource, TMember>
    {
        /// <summary>
        /// 忽略。
        /// </summary>
        void Ignore();

        /// <summary>
        /// 按照属性名称忽略大小写匹配（解决例如在自定义映射时，只读属性默认不被映射的问题）。
        /// </summary>
        void Auto();

        /// <summary>
        /// 常量。
        /// </summary>
        /// <param name="member">成员值。</param>
        void Constant(TMember member);

        /// <summary>
        /// 从源中获取。
        /// </summary>
        /// <param name="sourceMember">源成员。</param>
        void From(Expression<Func<TSource, TMember>> sourceMember);

        /// <summary>
        /// 从源中获取。
        /// </summary>
        /// <param name="valueResolver">解决方案。</param>
        void From(IValueResolver<TSource, TMember> valueResolver);

        /// <summary>
        /// 使用转换器转换指定成员值。
        /// </summary>
        /// <typeparam name="TSourceMember">源。</typeparam>
        /// <param name="valueConverter">转换器。</param>
        /// <param name="sourceMember">源表达式。</param>
        void ConvertUsing<TSourceMember>(Expression<Func<TSource, TSourceMember>> sourceMember, IValueConverter<TSourceMember, TMember> valueConverter);
    }

    /// <summary>
    /// 泛型映射表达式。
    /// </summary>
    /// <typeparam name="TSource">源类型。</typeparam>
    /// <typeparam name="TDestination">目标类型。</typeparam>
    public interface IProfileGenericExpression<TSource, TDestination> : IProfileExpressionBase<TSource, TDestination>
    {
        /// <summary>
        /// 新建实例映射。
        /// </summary>
        /// <param name="newInstanceExpression">创建实例表达式。</param>
        /// <returns>映射关系表达式。</returns>
        IProfileExpressionBase<TSource, TDestination> New(Expression<Func<TSource, TDestination>> newInstanceExpression);
    }

    /// <summary>
    /// 解决方案。
    /// </summary>
    /// <typeparam name="TSource">源。</typeparam>
    /// <typeparam name="TMember">成员。</typeparam>
    public interface IValueResolver<in TSource, out TMember>
    {
        /// <summary>
        /// 解决。
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        TMember Resolve(TSource source);
    }

    /// <summary>
    /// 转换器。
    /// </summary>
    /// <typeparam name="TSourceMember">源成员。</typeparam>
    /// <typeparam name="TDestinationMember">目标成员。</typeparam>
    public interface IValueConverter<in TSourceMember, out TDestinationMember>
    {
        /// <summary>
        /// 转换器。
        /// </summary>
        /// <param name="sourceMember">源成员值。</param>
        /// <returns>目标成员值。</returns>
        TDestinationMember Convert(TSourceMember sourceMember);
    }
}
