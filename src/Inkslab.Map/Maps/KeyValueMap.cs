﻿using System;
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

            var conversionGenericArguments = destinationType.GetGenericArguments();

            if (sourceType == destinationType && Array.TrueForAll(conversionGenericArguments, x => x.IsValueType))
            {
                return sourceExpression;
            }

            bool flag = true;

            var arguments = conversionGenericArguments.Zip(sourceType.GetGenericArguments(), (x, y) =>
            {
                try
                {
                    var propertyExpression = Property(sourceExpression, flag ? "Key" : "Value");

                    return x == y && x.IsValueType
                        ? propertyExpression
                        : application.Map(propertyExpression, x);
                }
                finally
                {
                    flag = false;
                }
            });

            return New(destinationType.GetConstructor(conversionGenericArguments)!, arguments);
        }
    }
}
