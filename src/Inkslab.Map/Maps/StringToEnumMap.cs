using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.Serialization;

namespace Inkslab.Map.Maps
{
    using static Expression;

    /// <summary>
    /// 字符串转枚举映射。
    /// </summary>
    public class StringToEnumMap : IMap
    {
        private static readonly PropertyInfo LengthPrt = typeof(string).GetProperty("length");
        private readonly static MethodInfo ConcatMtd = typeof(string).GetMethod("Concat", BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly, null, new Type[3] { typeof(string), typeof(string), typeof(string) }, null);


        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <param name="sourceType"><inheritdoc/></param>
        /// <param name="destinationType"><inheritdoc/></param>
        /// <returns><inheritdoc/></returns>
        public bool IsMatch(Type sourceType, Type destinationType) => sourceType == typeof(string) && destinationType.IsEnum;

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
            List<SwitchCase> switchCases = new List<SwitchCase>();

            foreach (var memberInfo in destinationType.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly))
            {
                var attribute = (EnumMemberAttribute)Attribute.GetCustomAttribute(memberInfo, typeof(EnumMemberAttribute));

                if (attribute is null)
                {
                    continue;
                }

                if (attribute.IsValueSetExplicitly)
                {
                    switchCases.Add(SwitchCase(Constant(memberInfo.GetRawConstantValue(), destinationType), Constant(attribute.Value?.ToLower())));
                }
            }

            var destinationExpression = Variable(destinationType);

            var bodyExp = Block(new ParameterExpression[] { destinationExpression }, IfThen(Not(Call(MapConstants.TryParseMtd.MakeGenericMethod(destinationType), sourceExpression, Constant(true, typeof(bool)), destinationExpression)), ThrowError(sourceExpression, sourceType, destinationType)), destinationExpression);

            if (switchCases.Count == 0)
            {
                return bodyExp;
            }

            return Condition(Equal(Property(sourceExpression, LengthPrt), Constant(0)), Default(destinationType), Switch(Call(sourceExpression, MapConstants.ToLowerMtd), bodyExp, null, switchCases));
        }

        private static Expression ThrowError(Expression variable, Type sourceType, Type destinationType)
        {
            return Throw(New(MapConstants.InvalidCastExceptionCtorOfString, Call(ConcatMtd, Constant($"无法将类型({sourceType})的值"), Call(variable, sourceType.GetMethod("ToString", Type.EmptyTypes)), Constant($"转换为类型({destinationType})!"))));
        }
    }
}
