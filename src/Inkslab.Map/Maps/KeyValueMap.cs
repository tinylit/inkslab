using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Inkslab.Map.Maps
{
    using static Expression;

    /// <summary>
    /// 键值对映射。
    /// </summary>
    public class KeyValueMap : IMap
    {
        private static readonly Type _keyValueType = typeof(KeyValuePair<,>);

        private static bool IsKeyValue(Type conversionType) => conversionType.IsGenericType && conversionType.GetGenericTypeDefinition() == _keyValueType;

        /// <summary>
        /// <see cref="KeyValuePair{TKey, TValue}"/> 映射。
        /// </summary>
        /// <param name="sourceType"><inheritdoc/></param>
        /// <param name="destinationType"><inheritdoc/></param>
        /// <returns><inheritdoc/></returns>
        public bool IsMatch(Type sourceType, Type destinationType) => IsKeyValue(sourceType) && IsKeyValue(destinationType);

        /// <inheritdoc/>
        public Expression ToSolve(Expression sourceExpression, Type destinationType, IMapApplication application)
        {
            Type sourceType = sourceExpression.Type;

            var sourceGenericArguments = sourceType.GetGenericArguments();

            if (sourceType == destinationType && Array.TrueForAll(sourceGenericArguments, x => x.IsValueType))
            {
                return sourceExpression;
            }

            var arguments = new Expression[2];

            var conversionGenericArguments = destinationType.GetGenericArguments();

            if (CloneCheck(conversionGenericArguments[0], sourceGenericArguments[0]))
            {
                arguments[0] = application.Map(Property(sourceExpression, "Key"), conversionGenericArguments[0]);
            }
            else
            {
                arguments[0] = Property(sourceExpression, "Key");
            }

            if (CloneCheck(conversionGenericArguments[1], sourceGenericArguments[1]))
            {
                arguments[1] = application.Map(Property(sourceExpression, "Value"), conversionGenericArguments[1]);
            }
            else
            {
                arguments[1] = Property(sourceExpression, "Value");
            }

            return New(destinationType.GetConstructor(conversionGenericArguments)!, arguments);
        }

        private static bool CloneCheck(Type sourceType, Type destinationType)
        {
            if (sourceType != destinationType)
            {
                return true;
            }

            if (sourceType.IsValueType)
            {
                return false;
            }

            return true;
        }
    }
}
