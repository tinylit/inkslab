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
        /// <returns></returns>
        bool IsMatch(Type sourceType, Type destinationType);

        /// <summary>
        /// 解决。
        /// </summary>
        /// <param name="sourceExpression">源。</param>
        /// <param name="destinationType">目标。</param>
        /// <param name="configuration">配置文件。</param>
        /// <returns></returns>
        Expression ToSolve(Expression sourceExpression, Type sourceType, Type destinationType, IMapConfiguration configuration);
    }
}
