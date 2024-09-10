using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Inkslab.Map.Maps
{
    using static Expression;

    /// <summary>
    /// 从 <see cref="IEnumerable{T}"/>, T is <see cref="KeyValuePair{TKey,TValue}"/> and TKey is <see cref="string"/> 中映射。
    /// </summary>
    public class FromKeyIsStringValueIsAnyMap : AbstractMap, IMap
    {
        private static readonly Type _keyValueType = typeof(KeyValuePair<,>);

        private static bool IsKeyValue(Type conversionType) => conversionType.IsGenericType && conversionType.GetGenericTypeDefinition() == _keyValueType;

        private static bool TryKeyValue(Type sourceType, out Type valueType)
        {
            foreach (var interfaceType in sourceType.GetInterfaces())
            {
                if (interfaceType.IsGenericType && interfaceType.GetGenericTypeDefinition() == MapConstants.Enumerable_T_Type)
                {
                    var keyValueType = interfaceType.GetGenericArguments().Single();

                    if (!IsKeyValue(keyValueType))
                    {
                        continue;
                    }

                    var genericArguments = keyValueType.GetGenericArguments();

                    if (genericArguments[0] == MapConstants.StringType)
                    {
                        valueType = genericArguments[1];

                        return true;
                    }
                }
            }

            valueType = null;

            return false;
        }

        /// <summary>
        /// 解决从 <see cref="IEnumerable{T}"/>, T is <see cref="KeyValuePair{TKey,TValue}"/> and TKey is <see cref="string"/> 到对象的映射。
        /// </summary>
        /// <param name="sourceType"><inheritdoc/></param>
        /// <param name="destinationType"><inheritdoc/></param>
        /// <returns><inheritdoc/></returns>
        public override bool IsMatch(Type sourceType, Type destinationType) => !destinationType.IsSimple() && TryKeyValue(sourceType, out _);

        /// <inheritdoc/>
        protected override Expression ToSolve(Expression sourceExpression, Type sourceType, ParameterExpression destinationExpression, Type destinationType, IMapApplication application)
        {
            var propertyInfos = Array.FindAll(destinationType.GetProperties(BindingFlags.Public | BindingFlags.Instance), x => x.CanWrite && !x.IsIgnore());

            if (propertyInfos.Length == 0)
            {
                return Empty();
            }

            if (!TryKeyValue(sourceType, out Type valueType))
            {
                throw new NotSupportedException();
            }

            var dictionaryType = typeof(Dictionary<,>).MakeGenericType(MapConstants.StringType, valueType);

            var dictionaryVar = Variable(dictionaryType);

            return IfThenElse(TypeIs(sourceExpression, dictionaryType),
                Block(new ParameterExpression[] { dictionaryVar }, Assign(dictionaryVar, TypeAs(sourceExpression, dictionaryType)), ToSolveDictionary(dictionaryType, valueType, dictionaryVar, destinationExpression, propertyInfos, application)),
                ToSolveUniversal(valueType, sourceExpression, destinationExpression, propertyInfos, application));
        }

        private static Expression ToSolveDictionary(Type dictionaryType, Type valueType, Expression sourceExpression, ParameterExpression destinationExpression, PropertyInfo[] propertyInfos, IMapApplication application)
        {
            var dictionaryVar = Variable(valueType);

            var expressions = new List<Expression>(propertyInfos.Length);

            var tryGetValueMtd = dictionaryType.GetMethod("TryGetValue", new Type[] { MapConstants.StringType, valueType.MakeByRefType() })!;

            foreach (var propertyInfo in propertyInfos)
            {
                var propertyType = propertyInfo.PropertyType;

                var destinationProp = Property(destinationExpression, propertyInfo);

                expressions.Add(IfThen(Call(sourceExpression, tryGetValueMtd, Constant(propertyInfo.Name), dictionaryVar), Assign(destinationProp, application.Map(dictionaryVar, propertyType))));
            }

            return Block(new ParameterExpression[] { dictionaryVar }, expressions);
        }

        private static Expression ToSolveUniversal(Type valueType, Expression sourceExpression, ParameterExpression destinationExpression, PropertyInfo[] propertyInfos, IMapApplication application)
        {
            LabelTarget breakLabel = Label(MapConstants.VoidType);
            LabelTarget continueLabel = Label(MapConstants.VoidType);

            var keyValueType = typeof(KeyValuePair<,>).MakeGenericType(MapConstants.StringType, valueType);
            var kvStringEnumerableType = typeof(IEnumerable<>).MakeGenericType(keyValueType);
            var kvStringEnumeratorType = typeof(IEnumerator<>).MakeGenericType(keyValueType);

            var getEnumeratorMtd = kvStringEnumerableType.GetMethod("GetEnumerator", Type.EmptyTypes)!;

            var propertyCurrent = kvStringEnumeratorType.GetProperty("Current")!;

            var propertyKey = keyValueType.GetProperty("Key")!;
            var propertyValue = keyValueType.GetProperty("Value")!;

            var keyValueExp = Variable(keyValueType);
            var enumeratorExp = Variable(kvStringEnumeratorType);

            var sourceKeyProp = Property(keyValueExp, propertyKey);
            var sourceValueProp = Property(keyValueExp, propertyValue);

            List<SwitchCase> switchCases = new List<SwitchCase>(propertyInfos.Length);

            var hash = new HashSet<string>();

            Array.ForEach(propertyInfos, x => hash.Add(x.Name));

            var namingTypeArray = Enum.GetValues(typeof(NamingType));

            foreach (var propertyInfo in propertyInfos)
            {
                var propertyType = propertyInfo.PropertyType;

                var destinationProp = Property(destinationExpression, propertyInfo);

                var testValues = new List<Expression>(5);

                string propertyName = propertyInfo.Name;

                foreach (NamingType namingType in namingTypeArray)
                {
                    string name = propertyName.ToNamingCase(namingType);

                    if (namingType == NamingType.Normal || hash.Add(name))
                    {
                        testValues.Add(Constant(name));
                    }
                }

                switchCases.Add(SwitchCase(Assign(destinationProp, application.Map(sourceValueProp, propertyType)), testValues));
            }

            var bodyExp = Switch(MapConstants.VoidType, sourceKeyProp, null, null, switchCases);

            return Block(new ParameterExpression[]
            {
                keyValueExp,
                enumeratorExp
            }, new Expression[]
            {
                Assign(enumeratorExp, Call(sourceExpression, getEnumeratorMtd)),
                Loop(
                    IfThenElse(
                        Call(enumeratorExp, MapConstants.MoveNextMtd),
                        Block(
                            MapConstants.VoidType,
                            Assign(keyValueExp, Property(enumeratorExp, propertyCurrent)),
                            bodyExp,
                            Continue(continueLabel)
                        ),
                        Break(breakLabel)), // push to eax/rax --> return value
                    breakLabel, continueLabel)
            });
        }
    }
}