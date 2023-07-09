using System;
using System.Linq.Expressions;

namespace Inkslab.Map.Maps
{
    using static Expression;

    /// <summary>
    /// 转字符串。调用 <see cref="object.ToString"/> 方法完成。
    /// </summary>
    public class ToStringMap : IMap
    {
        /// <summary>
        /// 转字符串。
        /// </summary>
        /// <param name="sourceType"><inheritdoc/></param>
        /// <param name="destinationType"><inheritdoc/></param>
        /// <returns><inheritdoc/></returns>
        public bool IsMatch(Type sourceType, Type destinationType) => destinationType == MapConstants.StirngType;

        /// <inheritdoc/>
        public Expression ToSolve(Expression sourceExpression, Type sourceType, Type destinationType, IMapApplication application)
        {
            Type objectType = MapConstants.ObjectType;

            var toStringMethod = sourceType.GetMethod(nameof(ToString), Type.EmptyTypes) ?? objectType.GetMethod(nameof(ToString), Type.EmptyTypes);

            if (sourceType.IsValueType && toStringMethod.DeclaringType == objectType)
            {
                return Call(Convert(sourceExpression, objectType), toStringMethod);
            }

            return Call(sourceExpression, toStringMethod);
        }
    }
}
