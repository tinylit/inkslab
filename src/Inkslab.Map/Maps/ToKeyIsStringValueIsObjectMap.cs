using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace Inkslab.Map.Maps
{
    using static Expression;
    using kvString = KeyValuePair<string, object>;

    /// <summary>
    /// 转 <see cref="ICollection{T}"/> , T is <seealso cref="kvString"/> 的映射。
    /// </summary>

    public class ToKeyIsStringValueIsObjectMap : AbstractMap
    {
        private static readonly Type kvStringType = typeof(kvString);
        private static readonly Type kvStringCollectionType = typeof(ICollection<kvString>);
        private static readonly ConstructorInfo kvStringCtor = typeof(kvString).GetConstructor(new Type[2] { MapConstants.StirngType, MapConstants.ObjectType });

        private static readonly Type kvStringDictionaryType = typeof(IDictionary<string, object>);

        private static readonly MethodInfo collectionAddMtd = kvStringCollectionType.GetMethod("Add", MapConstants.InstanceBindingFlags, null, new Type[1] { kvStringType }, null);
        private static readonly MethodInfo dictionaryAddMtd = kvStringDictionaryType.GetMethod("Add", MapConstants.InstanceBindingFlags, null, new Type[2] { MapConstants.StirngType, MapConstants.ObjectType }, null);

        /// <summary>
        /// 解决 <see cref="KeyValuePair{TKey, TValue}"/>, TKey is <seealso cref="string"/>, TValue is <seealso cref="object"/> 到对象的映射。
        /// </summary>
        /// <param name="sourceType"><inheritdoc/></param>
        /// <param name="destinationType"><inheritdoc/></param>
        /// <returns><inheritdoc/></returns>
        public override bool IsMatch(Type sourceType, Type destinationType) => kvStringCollectionType.IsAssignableFrom(destinationType);

        /// <inheritdoc/>
        protected override Expression ToSolve(Expression sourceExpression, Type sourceType, ParameterExpression destinationExpression, Type destinationType, IMapApplication application)
        {
            var propertyInfos = Array.FindAll(sourceType.GetProperties(), x => x.CanRead);

            var expressions = new List<Expression>(propertyInfos.Length);

            bool flag = kvStringDictionaryType.IsAssignableFrom(destinationType);

            foreach (var propertyInfo in propertyInfos)
            {
                if (propertyInfo.IsIgnore())
                {
                    continue;
                }

                var keyExpression = Constant(propertyInfo.Name);
                var valueExpression = application.Map(Property(sourceExpression, propertyInfo), MapConstants.ObjectType);

                if (flag)
                {
                    expressions.Add(Call(destinationExpression, dictionaryAddMtd, keyExpression, valueExpression));
                }
                else
                {
                    expressions.Add(Call(destinationExpression, collectionAddMtd, New(kvStringCtor, keyExpression, valueExpression)));
                }
            }

            return Block(expressions);
        }
    }
}
