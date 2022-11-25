using System;
using System.Linq.Expressions;
using System.Reflection;

namespace Inkslab.Map
{
    /// <summary>
    /// 基础表达式。
    /// </summary>
    /// <typeparam name="TSource"></typeparam>
    /// <typeparam name="TDestination"></typeparam>
    public interface IMappingExpressionBase<TSource, TDestination>
    {
        /// <summary>
        /// 指定成员映射规则。
        /// </summary>
        /// <typeparam name="TMember">成员。</typeparam>
        /// <param name="destinationMember">目标成员。</param>
        /// <param name="memberOptions">成员配置。</param>
        /// <returns></returns>
        IMappingExpressionBase<TSource, TDestination> Map<TMember>(Expression<Func<TDestination, TMember>> destinationMember, Action<IMemberMappingExpression<TSource, TMember>> memberOptions);
    }

    /// <summary>
    /// 映射表达式。
    /// </summary>
    /// <typeparam name="TSource">源。</typeparam>
    /// <typeparam name="TDestination">目标。</typeparam>
    public interface IMappingExpression<TSource, TDestination> : IMappingExpressionBase<TSource, TDestination>
    {
        /// <summary>
        /// 包含继承 <typeparamref name="TDestination"/> 的类型，指定共享规则。
        /// </summary>
        /// <typeparam name="TAssignableToDestination">可赋值到<typeparamref name="TDestination"/>的类型。</typeparam>
        /// <returns></returns>
        IMappingExpression<TSource, TDestination> Include<TAssignableToDestination>() where TAssignableToDestination : TDestination;
    }

    /// <summary>
    /// 成员映射表达式。
    /// </summary>
    /// <typeparam name="TSource">源。</typeparam>
    /// <typeparam name="TMember">成员。</typeparam>
    public interface IMemberMappingExpression<TSource, TMember>
    {
        /// <summary>
        /// 忽略。
        /// </summary>
        void Ignore();

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
    /// 解决方案。
    /// </summary>
    /// <typeparam name="TSource">源。</typeparam>
    /// <typeparam name="TMember">成员。</typeparam>
    public interface IValueResolver<in TSource, TMember>
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

    /// <summary>
    /// 比较器。
    /// </summary>
    public interface IMemberComparator
    {
        /// <summary>
        /// 成员是否匹配。
        /// </summary>
        /// <param name="x">左成员。</param>
        /// <param name="y">右成员。</param>
        /// <returns>是否匹配。</returns>
        bool IsMacth(MemberInfo x, MemberInfo y);
    }
}
