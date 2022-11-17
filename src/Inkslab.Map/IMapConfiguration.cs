using System.Linq.Expressions;

namespace Inkslab.Map
{
    /// <summary>
    /// 映射配置。
    /// </summary>
    public interface IMapConfiguration
    {
        /// <summary>
        /// 是否匹配。
        /// </summary>
        /// <param name="sourceType">源类型。</param>
        /// <param name="destinationType">目标类型。</param>
        /// <returns>是否匹配。</returns>
        bool IsMatch(Type sourceType, Type destinationType);

        /// <summary>
        /// 映射。
        /// </summary>
        /// <param name="sourceExpression">源数据。</param>
        /// <param name="destinationType">目标类型。</param>
        /// <returns>映射关系表达式。</returns>
        Expression Map(Expression sourceExpression, Type destinationType);
    }
}
