using System.Linq.Expressions;
using System.Reflection;

namespace Inkslab.Map.Maps
{
    using static Expression;
    using kvString = KeyValuePair<string, object>;

    /// <summary>
    /// 从 <see cref="IEnumerable{T}"/>, T is <seealso cref="kvString"/> 中映射。
    /// </summary>
    public class FromKeyStringValueObjectMap : AbstractMap, IMap
    {
        private readonly static Type kvStringType = typeof(kvString);
        private readonly static Type kvStringEnumerableType = typeof(IEnumerable<kvString>);
        private readonly static Type kvStringEnumeratorType = typeof(IEnumerator<kvString>);

        private readonly static MethodInfo GetEnumeratorMtd = kvStringEnumerableType.GetMethod("GetEnumerator", Type.EmptyTypes);

        private readonly static PropertyInfo PropertyCurrent = kvStringEnumeratorType.GetProperty("Current");

        private readonly static PropertyInfo PropertyKey = kvStringType.GetProperty("Key");
        private readonly static PropertyInfo PropertyValue = kvStringType.GetProperty("Value");

        public override bool IsMatch(Type sourceType, Type destinationType) => kvStringEnumerableType.IsAssignableFrom(sourceType);

        protected override Expression ToSolve(Expression sourceExpression, Type sourceType, ParameterExpression destinationExpression, Type destinationType, IMapConfiguration configuration)
        {
            LabelTarget break_label = Label(typeof(void));
            LabelTarget continue_label = Label(typeof(void));

            var keyValueExp = Variable(typeof(kvString));
            var enumeratorExp = Variable(kvStringEnumeratorType);

            var sourceKeyProp = Property(keyValueExp, PropertyKey);
            var sourceValueProp = Property(keyValueExp, PropertyValue);

            List<SwitchCase> switchCases = new List<SwitchCase>();

            foreach (var propertyInfo in destinationType.GetProperties())
            {
                if (!propertyInfo.CanWrite)
                {
                    continue;
                }

                var destinationProp = Property(destinationExpression, propertyInfo);

                switchCases.Add(SwitchCase(Assign(destinationProp, configuration.Map(sourceValueProp, propertyInfo.PropertyType)), Constant(propertyInfo.Name.ToPascalCase())));
            }

            var bodyExp = Switch(Call(MapConstants.ToNamingCaseMtd, sourceKeyProp, Constant(NamingType.PascalCase)), null, null, switchCases);

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
