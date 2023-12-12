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
            var propertyInfos = Array.FindAll(destinationType.GetProperties(BindingFlags.Public | BindingFlags.Instance), x => x.CanWrite);

            if (propertyInfos.Length == 0)
            {
                return destinationExpression;
            }

            LabelTarget breakLabel = Label(MapConstants.VoidType);
            LabelTarget continueLabel = Label(MapConstants.VoidType);

            var keyValueExp = Variable(typeof(kvString));
            var enumeratorExp = Variable(kvStringEnumeratorType);

            var sourceKeyProp = Property(keyValueExp, propertyKey);
            var sourceValueProp = Property(keyValueExp, propertyValue);

            List<SwitchCase> switchCases = new List<SwitchCase>();

            var hash = new HashSet<string>();

            foreach (var propertyInfo in propertyInfos)
            {
                if (propertyInfo.IsIgnore())
                {
                    continue;
                }

                var propertyType = propertyInfo.PropertyType;

                var destinationProp = Property(destinationExpression, propertyInfo);

                var testValues = new List<Expression>(5);

                string propertyName = propertyInfo.Name;

                foreach (NamingType namingType in Enum.GetValues(typeof(NamingType)))
                {
                    string name = propertyName.ToNamingCase(namingType);

                    if (hash.Add(name))
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
