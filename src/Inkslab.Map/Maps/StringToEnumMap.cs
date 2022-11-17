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
        private static readonly MethodInfo ParseMtd = typeof(Enum).GetMember(nameof(Enum.Parse), MemberTypes.Method, BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .Cast<MethodInfo>()
            .Single(x =>
            {
                if (x.IsGenericMethod)
                {
                    var parameterInfos = x.GetParameters();

                    if (parameterInfos.Length == 2)
                    {
                        return parameterInfos[0].ParameterType == typeof(string) && parameterInfos[1].ParameterType == typeof(bool);
                    }
                }

                return false;
            });

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
        /// <param name="destinationType"><inheritdoc/></param>
        /// <param name="configuration"><inheritdoc/></param>
        /// <returns><inheritdoc/></returns>
        public Expression ToSolve(Expression sourceExpression, Type sourceType, Type destinationType, IMapConfiguration configuration)
        {
            List<SwitchCase> switchCases = new List<SwitchCase>();

            foreach (var memberInfo in destinationType.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly))
            {
                var attribute = (EnumMemberAttribute)Attribute.GetCustomAttribute(memberInfo, typeof(EnumMemberAttribute));

                if (attribute.IsValueSetExplicitly)
                {
                    switchCases.Add(SwitchCase(Constant(memberInfo.GetRawConstantValue(), destinationType), Constant(attribute.Value?.ToLower())));
                }
            }

            var parseEnumExpression = Convert(Call(ParseMtd.MakeGenericMethod(destinationType), sourceExpression, Constant(true)), destinationType);

            return Condition(Call(null, MapConstants.IsEmptyMtd, sourceExpression), Default(destinationType), Switch(Call(sourceExpression, MapConstants.ToLowerMtd), parseEnumExpression, null, switchCases));
        }
    }
}
