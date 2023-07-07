using System;
using System.Linq.Expressions;

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
            => destinationType.GetConstructor(MapConstants.InstanceBindingFlags, null, new Type[] { sourceType }, null) is not null;

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
            var constructorInfo = destinationType.GetConstructor(MapConstants.InstanceBindingFlags, null, new Type[] { sourceType }, null);

            return New(constructorInfo, sourceExpression);
        }
    }
}
