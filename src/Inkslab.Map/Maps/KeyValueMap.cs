using System.Linq.Expressions;
using System.Reflection;
using System.Security.AccessControl;

namespace Inkslab.Map.Maps
{
    using static Expression;

    /// <summary>
    /// 键值对映射。
    /// </summary>
    public class KeyValueMap : IMap
    {
        private readonly static Type KeyValueType = typeof(KeyValuePair<,>);

        private static bool IsKeyValue(Type conversionType) => conversionType.IsGenericType && conversionType.GetGenericTypeDefinition() == KeyValueType;

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <param name="sourceType"><inheritdoc/></param>
        /// <param name="destinationType"><inheritdoc/></param>
        /// <returns><inheritdoc/></returns>
        public bool IsMatch(Type sourceType, Type destinationType) => IsKeyValue(sourceType) && IsKeyValue(destinationType);

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <param name="sourceExpression"><inheritdoc/></param>
        /// <param name="destinationType"><inheritdoc/></param>
        /// <param name="configuration"><inheritdoc/></param>
        /// <returns><inheritdoc/></returns>
        public Expression ToSolve(Expression sourceExpression, Type sourceType, Type destinationType, IMapConfiguration configuration)
        {
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
                        : configuration.Map(propertyExpression, x);
                }
                finally
                {
                    flag = false;
                }
            });

            return New(destinationType.GetConstructor(conversionGenericArguments), arguments);
        }
    }
}
