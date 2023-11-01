using System;
using System.Linq.Expressions;
using System.Reflection;

namespace Inkslab.Map.Maps
{
    using static Expression;

    /// <summary>
    /// 使用 <see cref="System.Convert"/> 类转换。
    /// </summary>
    public class ConvertMap : IMap
    {
        private static bool IsPrimitive(Type type) => type.IsPrimitive || type == MapConstants.StirngType || type == typeof(decimal);

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <param name="sourceType"><inheritdoc/></param>
        /// <param name="destinationType"><inheritdoc/></param>
        /// <returns><inheritdoc/></returns>
        public bool IsMatch(Type sourceType, Type destinationType) =>
            (sourceType == MapConstants.StirngType && destinationType == typeof(DateTime)) ||
            (sourceType == typeof(DateTime) && destinationType == MapConstants.StirngType) ||
            (IsPrimitive(sourceType) && IsPrimitive(destinationType));

        /// <inheritdoc/>
        public Expression ToSolve(Expression sourceExpression, Type destinationType, IMapApplication application)
        {
            Type sourceType = sourceExpression.Type;

            if (sourceType == destinationType)
            {
                return sourceExpression;
            }

            var convertMethod = typeof(Convert).GetMethod("To" + destinationType.Name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.DeclaredOnly, null, new[] { sourceType }, null);

            return Call(convertMethod, sourceExpression);
        }
    }
}
