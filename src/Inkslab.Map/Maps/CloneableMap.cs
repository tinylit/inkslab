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
        public bool IsMatch(Type sourceType, Type destinationType) => destinationType.IsAssignableFrom(sourceType) && typeof(ICloneable).IsAssignableFrom(sourceType);

        public Expression ToSolve(Expression sourceExpression, Type sourceType, Type destinationType, IMapConfiguration configuration) => Call(sourceExpression, MapConstants.CloneMtd);
    }
}
