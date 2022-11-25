using System;
using System.Linq.Expressions;

namespace Inkslab.Map.Maps
{
    using static Expression;

    /// <summary>
    /// 克隆映射。
    /// </summary>
    public class CloneableMap : IMap
    {
        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <param name="sourceType"><inheritdoc/></param>
        /// <param name="destinationType"><inheritdoc/></param>
        /// <returns><inheritdoc/></returns>
        public bool IsMatch(Type sourceType, Type destinationType) => destinationType.IsAssignableFrom(sourceType) && typeof(ICloneable).IsAssignableFrom(sourceType);

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <param name="sourceExpression"><inheritdoc/></param>
        /// <param name="sourceType"><inheritdoc/></param>
        /// <param name="destinationType"><inheritdoc/></param>
        /// <param name="configuration"><inheritdoc/></param>
        /// <returns><inheritdoc/></returns>
        public Expression ToSolve(Expression sourceExpression, Type sourceType, Type destinationType, IMapConfiguration configuration) => Call(sourceExpression, MapConstants.CloneMtd);
    }
}
