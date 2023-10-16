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
        private readonly static Type kvStringType = typeof(kvString);
        private readonly static Type kvStringEnumerableType = typeof(IEnumerable<kvString>);
        private readonly static Type kvStringEnumeratorType = typeof(IEnumerator<kvString>);

        private readonly static MethodInfo GetEnumeratorMtd = kvStringEnumerableType.GetMethod("GetEnumerator", Type.EmptyTypes);

        private readonly static PropertyInfo PropertyCurrent = kvStringEnumeratorType.GetProperty("Current");

        private readonly static PropertyInfo PropertyKey = kvStringType.GetProperty("Key");
        private readonly static PropertyInfo PropertyValue = kvStringType.GetProperty("Value");

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
            var propertyInfos = Array.FindAll(destinationType.GetProperties(), x => x.CanWrite);

            if (propertyInfos.Length == 0)
            {
                return destinationExpression;
            }

            LabelTarget break_label = Label(MapConstants.VoidType);
            LabelTarget continue_label = Label(MapConstants.VoidType);

            var keyValueExp = Variable(typeof(kvString));
            var enumeratorExp = Variable(kvStringEnumeratorType);

            var sourceKeyProp = Property(keyValueExp, PropertyKey);
            var sourceValueProp = Property(keyValueExp, PropertyValue);

            List<SwitchCase> switchCases = new List<SwitchCase>();

            foreach (var propertyInfo in propertyInfos)
            {
                var propertyType = propertyInfo.PropertyType;

                var destinationProp = Property(destinationExpression, propertyInfo);

                switchCases.Add(SwitchCase(Assign(destinationProp, application.Map(sourceValueProp, propertyType)), Constant(propertyInfo.Name.ToLower())));
            }

            var bodyExp = Switch(MapConstants.VoidType, Call(sourceKeyProp, MapConstants.ToLowerMtd), null, null, switchCases);

            return Block(new ParameterExpression[]
             {
                keyValueExp,
                enumeratorExp
             }, new Expression[]
             {
                Assign(enumeratorExp, Call(sourceExpression, GetEnumeratorMtd)),
                Loop(
                    IfThenElse(
                        Call(enumeratorExp, MapConstants.MoveNextMtd),
                        Block(
                            MapConstants.VoidType,
                            Assign(keyValueExp, Property(enumeratorExp, PropertyCurrent)),
                            bodyExp,
                            Continue(continue_label)
                        ),
                        Break(break_label)), // push to eax/rax --> return value
                    break_label, continue_label)
             });
        }
    }
}
