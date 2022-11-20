using System;
using System.Linq.Expressions;

namespace Inkslab.Map.Maps
{
    using static Expression;

    /// <summary>
    /// 转字符串。
    /// </summary>
    public class ToStringMap : IMap
    {
        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <param name="sourceType"><inheritdoc/></param>
        /// <param name="destinationType"><inheritdoc/></param>
        /// <returns><inheritdoc/></returns>
        public bool IsMatch(Type sourceType, Type destinationType) => destinationType == typeof(string);

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <param name="sourceExpression"><inheritdoc/></param>
        /// <param name="destinationType"><inheritdoc/></param>
        /// <param name="configuration"><inheritdoc/></param>
        /// <returns><inheritdoc/></returns>
        public Expression ToSolve(Expression sourceExpression, Type sourceType, Type destinationType, IMapConfiguration configuration)
        {
            Type objectType = typeof(object);

            var toStringMethod = sourceType.GetMethod(nameof(object.ToString), Type.EmptyTypes) ?? objectType.GetMethod(nameof(object.ToString), Type.EmptyTypes);

            if (sourceType.IsValueType && toStringMethod.DeclaringType == objectType)
            {
                return Call(Convert(sourceExpression, objectType), toStringMethod);
            }

            return Call(sourceExpression, toStringMethod);
        }
    }
}
