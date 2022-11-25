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
        /// <summary>
        /// 目标类型<paramref name="destinationType"/>是否具有任意构造函数仅有一个参数，且参数可以被源类型<paramref name="sourceType"/>赋值。
        /// </summary>
        /// <param name="sourceType"><inheritdoc/></param>
        /// <param name="destinationType"><inheritdoc/></param>
        /// <returns><inheritdoc/></returns>
        public bool IsMatch(Type sourceType, Type destinationType)
            => Array.Exists(destinationType.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance), x =>
            {
                var parameterInfos = x.GetParameters();

                return parameterInfos.Length == 1 && Array.TrueForAll(parameterInfos, y => y.ParameterType.IsAssignableFrom(sourceType));
            });

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
            var constructorInfo = Array.Find(destinationType.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance), x =>
            {
                var parameterInfos = x.GetParameters();

                return parameterInfos.Length == 1 && Array.TrueForAll(parameterInfos, y => y.ParameterType.IsAssignableFrom(sourceType));
            });

            return New(constructorInfo, sourceExpression);
        }
    }
}
