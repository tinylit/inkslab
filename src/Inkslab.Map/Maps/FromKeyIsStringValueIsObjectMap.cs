using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace Inkslab.Map.Maps
{
    using static Expression;
    using kvString = KeyValuePair<string, object>;

    /// <summary>
    /// 从 <see cref="IEnumerable{T}"/>, T is <seealso cref="kvString"/> 中映射。
    /// </summary>
    public class FromKeyIsStringValueIsObjectMap : AbstractMap, IMap
    {
        private static readonly Type kvStringType = typeof(kvString);
        private static readonly Type kvStringEnumerableType = typeof(IEnumerable<kvString>);
        private static readonly Type kvStringEnumeratorType = typeof(IEnumerator<kvString>);

        private static readonly Type dictionaryType = typeof(IDictionary<string, object>);

        private static readonly MethodInfo tryGetValueMtd = dictionaryType.GetMethod("TryGetValue", new Type[] { MapConstants.StringType, MapConstants.ObjectType.MakeByRefType() });

        private static readonly MethodInfo getEnumeratorMtd = kvStringEnumerableType.GetMethod("GetEnumerator", Type.EmptyTypes);

        private static readonly PropertyInfo propertyCurrent = kvStringEnumeratorType.GetProperty("Current");

        private static readonly PropertyInfo propertyKey = kvStringType.GetProperty("Key");
        private static readonly PropertyInfo propertyValue = kvStringType.GetProperty("Value");

        /// <summary>
        /// 解决从 <see cref="IEnumerable{T}"/>, T is <seealso cref="kvString"/> 到对象的映射。
        /// </summary>
        /// <param name="sourceType"><inheritdoc/></param>
        /// <param name="destinationType"><inheritdoc/></param>
        /// <returns><inheritdoc/></returns>
        public override bool IsMatch(Type sourceType, Type destinationType) => kvStringEnumerableType.IsAssignableFrom(sourceType);

        /// <inheritdoc/>
        protected override Expression ToSolve(Expression sourceExpression, Type sourceType, ParameterExpression destinationExpression, Type destinationType, IMapApplication application)
        {
            var propertyInfos = Array.FindAll(destinationType.GetProperties(BindingFlags.Public | BindingFlags.Instance), x => x.CanWrite && !x.IsIgnore());

            if (propertyInfos.Length == 0)
            {
                return Empty();
            }

            var dictionaryVar = Variable(dictionaryType);

            return IfThenElse(TypeIs(sourceExpression, dictionaryType),
                Block(new ParameterExpression[] { dictionaryVar }, Assign(dictionaryVar, TypeAs(sourceExpression, dictionaryType)), ToSolveDictionary(dictionaryVar, destinationExpression, propertyInfos, application)),
                ToSolveUniversal(sourceExpression, destinationExpression, propertyInfos, application));
        }

        private static Expression ToSolveDictionary(Expression sourceExpression, ParameterExpression destinationExpression, PropertyInfo[] propertyInfos, IMapApplication application)
        {
            var dictionaryVar = Variable(MapConstants.ObjectType);

            var expressions = new List<Expression>(propertyInfos.Length);

            foreach (var propertyInfo in propertyInfos)
            {
                var propertyType = propertyInfo.PropertyType;

                var destinationProp = Property(destinationExpression, propertyInfo);

                expressions.Add(IfThen(Call(sourceExpression, tryGetValueMtd, Constant(propertyInfo.Name), dictionaryVar), Assign(destinationProp, application.Map(dictionaryVar, propertyType))));
            }

            return Block(new ParameterExpression[] { dictionaryVar }, expressions);
        }

        private static Expression ToSolveUniversal(Expression sourceExpression, ParameterExpression destinationExpression, PropertyInfo[] propertyInfos, IMapApplication application)
        {
            LabelTarget breakLabel = Label(MapConstants.VoidType);
            LabelTarget continueLabel = Label(MapConstants.VoidType);

            var keyValueExp = Variable(typeof(kvString));
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