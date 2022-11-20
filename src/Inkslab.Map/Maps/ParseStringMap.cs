using System;
using System.Linq.Expressions;
using System.Reflection;

namespace Inkslab.Map.Maps
{
    using static Expression;

    /// <summary>
    /// 字符串解析。
    /// </summary>
    public class ParseStringMap : IMap
    {
        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <param name="sourceType"><inheritdoc/></param>
        /// <param name="destinationType"><inheritdoc/></param>
        /// <returns><inheritdoc/></returns>
        public bool IsMatch(Type sourceType, Type destinationType) => sourceType == typeof(string) && (destinationType == typeof(Guid) || destinationType == typeof(Version) || destinationType == typeof(TimeSpan) || destinationType == typeof(DateTimeOffset));

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <param name="sourceExpression"><inheritdoc/></param>
        /// <param name="destinationType"><inheritdoc/></param>
        /// <param name="configuration"><inheritdoc/></param>
        /// <returns><inheritdoc/></returns>
        public Expression ToSolve(Expression sourceExpression, Type sourceType, Type destinationType, IMapConfiguration configuration)
        {
            var parseMethod = destinationType.GetMethod("Parse", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly, null, new[] { sourceType }, null);

            return Call(null, parseMethod, sourceExpression);
        }
    }
}
