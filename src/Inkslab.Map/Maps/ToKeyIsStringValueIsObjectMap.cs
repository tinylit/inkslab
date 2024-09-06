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
        private static readonly Type _kvStringType = typeof(kvString);
        private static readonly Type _kvStringCollectionType = typeof(ICollection<kvString>);
        private static readonly ConstructorInfo _kvStringCtor = typeof(kvString).GetConstructor(new Type[2] { MapConstants.StringType, MapConstants.ObjectType });
        private static readonly Type _kvStringDictionaryType = typeof(IDictionary<string, object>);
        private static readonly MethodInfo _collectionAddMtd = _kvStringCollectionType.GetMethod("Add", MapConstants.InstanceBindingFlags, null, new Type[1] { _kvStringType }, null);
        private static readonly MethodInfo _dictionaryAddMtd = _kvStringDictionaryType.GetMethod("Add", MapConstants.InstanceBindingFlags, null, new Type[2] { MapConstants.StringType, MapConstants.ObjectType }, null);

        /// <summary>
        /// 解决 <see cref="KeyValuePair{TKey, TValue}"/>, TKey is <seealso cref="string"/>, TValue is <seealso cref="object"/> 到对象的映射。
        /// </summary>
        /// <param name="sourceType"><inheritdoc/></param>
        /// <param name="destinationType"><inheritdoc/></param>
        /// <returns><inheritdoc/></returns>
        public override bool IsMatch(Type sourceType, Type destinationType) => !sourceType.IsSimple() && _kvStringCollectionType.IsAssignableFrom(destinationType);

        /// <inheritdoc/>
        protected override Expression ToSolve(Expression sourceExpression, Type sourceType, ParameterExpression destinationExpression, Type destinationType, IMapApplication application)
        {
            var propertyInfos = Array.FindAll(sourceType.GetProperties(), x => x.CanRead);

            var expressions = new List<Expression>(propertyInfos.Length);

            bool flag = _kvStringDictionaryType.IsAssignableFrom(destinationType);

            foreach (var propertyInfo in propertyInfos)
            {
                if (propertyInfo.IsIgnore())
                {
                    continue;
                }

                var keyExpression = Constant(propertyInfo.Name);
                var valueExpression = application.Map(Property(sourceExpression, propertyInfo), MapConstants.ObjectType);

                expressions.Add(flag
                    ? Call(destinationExpression, _dictionaryAddMtd, keyExpression, valueExpression)
                    : Call(destinationExpression, _collectionAddMtd, New(_kvStringCtor, keyExpression, valueExpression)));
            }

            return Block(expressions);
        }
    }
}