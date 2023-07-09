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
        /// 源 <paramref name="sourceType"/> 类型是目标类型 <paramref name="destinationType"/> 的父类，且实现了 <see cref="ICloneable"/> 接口。
        /// </summary>
        /// <param name="sourceType"><inheritdoc/></param>
        /// <param name="destinationType"><inheritdoc/></param>
        /// <returns><inheritdoc/></returns>
        public bool IsMatch(Type sourceType, Type destinationType) => destinationType.IsAssignableFrom(sourceType) && typeof(ICloneable).IsAssignableFrom(sourceType);

        /// <inheritdoc/>
        public Expression ToSolve(Expression sourceExpression, Type sourceType, Type destinationType, IMapApplication application) => Convert(Call(sourceExpression, MapConstants.CloneMtd), destinationType);
    }
}
