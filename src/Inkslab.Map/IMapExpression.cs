using System.Linq.Expressions;
using System;

namespace Inkslab.Map
{
    /// <summary>
    /// 映射表达式。
    /// </summary>
    public interface IMapExpression
    {
        /// <summary>
        /// 映射。
        /// </summary>
        /// <param name="sourceExpression">源数据。</param>
        /// <param name="destinationType">目标类型。</param>
        /// <returns>映射关系表达式。</returns>
        Expression Map(Expression sourceExpression, Type destinationType);
    }
}
