using System;
using System.Collections.Generic;
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
        private static readonly PropertyInfo lengthPrt = MapConstants.StringType.GetProperty("length");
        private static readonly MethodInfo concatMtd = MapConstants.StringType.GetMethod("Concat", BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly, null, new Type[3] { MapConstants.StringType, MapConstants.StringType, MapConstants.StringType }, null);


        /// <summary>
        /// 字符串转枚举映射。
        /// </summary>
        /// <param name="sourceType"><inheritdoc/></param>
        /// <param name="destinationType"><inheritdoc/></param>
        /// <returns><inheritdoc/></returns>
        public bool IsMatch(Type sourceType, Type destinationType) => sourceType == MapConstants.StringType && destinationType.IsEnum;

        /// <inheritdoc/>
        public Expression ToSolve(Expression sourceExpression, Type destinationType, IMapApplication application)
        {
            Type sourceType = sourceExpression.Type;

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

            return Condition(Equal(Property(sourceExpression, lengthPrt), Constant(0)), Default(destinationType), Switch(Call(sourceExpression, MapConstants.ToLowerMtd), bodyExp, null, switchCases));
        }

        private static Expression ThrowError(Expression variable, Type sourceType, Type destinationType)
        {
            return Throw(New(MapConstants.InvalidCastExceptionCtorOfString, Call(concatMtd, Constant($"无法将类型({sourceType})的值"), Call(variable, sourceType.GetMethod("ToString", Type.EmptyTypes)!), Constant($"转换为类型({destinationType})!"))));
        }
    }
}
