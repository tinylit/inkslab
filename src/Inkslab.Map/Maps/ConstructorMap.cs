using System;
using System.Linq.Expressions;
using System.Reflection;

namespace Inkslab.Map.Maps
{
    using static Expression;

    /// <summary>
    /// 构造函数映射。
    /// </summary>
    public class ConstructorMap : IMap
    {
        public bool IsMatch(Type sourceType, Type destinationType)
            => Array.Exists(destinationType.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance), x =>
            {
                var parameterInfos = x.GetParameters();

                return parameterInfos.Length == 1 && Array.TrueForAll(parameterInfos, y => y.ParameterType.IsAssignableFrom(sourceType));
            });

        public Expression ToSolve(Expression sourceExpression, Type sourceType, Type destinationType, IMapConfiguration configuration)
        {
            var constructorInfo = Array.Find(destinationType.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance), x =>
            {
                var parameterInfos = x.GetParameters();

                return parameterInfos.Length == 1 && Array.TrueForAll(parameterInfos, y => y.ParameterType.IsAssignableFrom(sourceType));
            });

            return New(constructorInfo, sourceExpression);
        }
    }
}
