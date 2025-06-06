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
        private static bool IsPrimitive(Type type) => type.IsPrimitive || type == MapConstants.StringType || type == typeof(decimal);

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <param name="sourceType"><inheritdoc/></param>
        /// <param name="destinationType"><inheritdoc/></param>
        /// <returns><inheritdoc/></returns>
        public bool IsMatch(Type sourceType, Type destinationType) =>
            sourceType == MapConstants.StringType && (destinationType.IsArray ? destinationType == typeof(byte[]) : destinationType == typeof(DateTime)) ||
            destinationType == MapConstants.StringType && (sourceType.IsArray ? sourceType == typeof(byte[]) : sourceType == typeof(DateTime)) ||
            IsPrimitive(sourceType) && IsPrimitive(destinationType);

        /// <inheritdoc/>
        public Expression ToSolve(Expression sourceExpression, Type destinationType, IMapApplication application)
        {
            Type sourceType = sourceExpression.Type;

            if (sourceType == destinationType)
            {
                return sourceExpression;
            }

            string name = sourceType.IsArray
                ? "ToBase64String"
                : destinationType.IsArray
                ? "FromBase64String"
                : "To" + destinationType.Name;

            var convertMethod = typeof(Convert).GetMethod(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.DeclaredOnly, null, new[] { sourceType }, null)!;

            if (sourceType == MapConstants.StringType && destinationType == typeof(bool))
            {
                return OrElse(OrElse(Equal(sourceExpression, Constant("1")), Equal(sourceExpression, Constant("1.0"))), Call(convertMethod, sourceExpression));
            }

            return Call(convertMethod, sourceExpression);
        }
    }
}