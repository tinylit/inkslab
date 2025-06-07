using System;
using System.Linq.Expressions;
using System.Reflection;

namespace Inkslab.Map.Maps
{
    using static Expression;

    /// <summary>
    /// 字符串转布尔解析。
    /// </summary>
    public class ParseStringToBooleanMap : IMap
    {
        /// <summary>
        /// 解析字符串。
        /// </summary>
        /// <param name="sourceType"><inheritdoc/></param>
        /// <param name="destinationType"><inheritdoc/></param>
        /// <returns><inheritdoc/></returns>
        public bool IsMatch(Type sourceType, Type destinationType) => sourceType == MapConstants.StringType && destinationType == typeof(bool);

        /// <inheritdoc/>
        public Expression ToSolve(Expression sourceExpression, Type destinationType, IMapApplication application)
        {
            Type sourceType = sourceExpression.Type;

            var parseMethod = destinationType.GetMethod("Parse", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly, null, new[] { sourceType }, null)!;

            return Condition(
                OrElse(Equal(sourceExpression, Constant("0")), Equal(sourceExpression, Constant("0.0"))),
                    Constant(false),
                    OrElse(OrElse(Equal(sourceExpression, Constant("1")), Equal(sourceExpression, Constant("1.0"))), Call(null, parseMethod, sourceExpression)));
        }
    }
}
