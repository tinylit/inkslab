using System;
using System.Linq.Expressions;

namespace Inkslab.Map
{
    /// <summary>
    /// 映射。
    /// </summary>
    public interface IMap
    {
        /// <summary>
        /// 是否匹配。
        /// </summary>
        /// <param name="sourceType">源类型。</param>
        /// <param name="destinationType">目标类型。</param>
        /// <returns>是否匹配。</returns>
        bool IsMatch(Type sourceType, Type destinationType);

        /// <summary>
        /// 解决。
        /// </summary>
        /// <param name="sourceExpression">源对象表达式。</param>
        /// <param name="sourceType">源类型。</param>
        /// <param name="destinationType">目标对象表达式。</param>
        /// <param name="application">映射程序。</param>
        /// <returns>目标对象<paramref name="destinationType"/>的映射逻辑表达式。</returns>
        Expression ToSolve(Expression sourceExpression, Type sourceType, Type destinationType, IMapApplication application);
    }
}
