﻿using System;
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
        private static bool IsPrimitive(Type type) => type.IsPrimitive || type == typeof(string) || type == typeof(decimal);

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <param name="sourceType"><inheritdoc/></param>
        /// <param name="destinationType"><inheritdoc/></param>
        /// <returns><inheritdoc/></returns>
        public bool IsMatch(Type sourceType, Type destinationType) =>
            (sourceType == typeof(string) && destinationType == typeof(DateTime)) ||
            (sourceType == typeof(DateTime) && destinationType == typeof(string)) ||
            (IsPrimitive(sourceType) && IsPrimitive(destinationType));

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <param name="sourceExpression"><inheritdoc/></param>
        /// <param name="sourceType"><inheritdoc/></param>
        /// <param name="destinationType"><inheritdoc/></param>
        /// <param name="configuration"><inheritdoc/></param>
        /// <returns><inheritdoc/></returns>
        public Expression ToSolve(Expression sourceExpression, Type sourceType, Type destinationType, IMapConfiguration configuration)
        {
            var convertMethod = typeof(Convert).GetMethod("To" + destinationType.Name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.DeclaredOnly, null, new[] { sourceType }, null);

            return Call(convertMethod, sourceExpression);
        }
    }
}
