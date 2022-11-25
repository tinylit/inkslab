using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Inkslab.Map.Maps
{
    using static Expression;

    /// <summary>
    /// 默认映射。
    /// </summary>
    public class DefaultMap : AbstractMap
    {
        /// <summary>
        /// 目标类型 <paramref name="destinationType"/> 不是抽象类型，且源类型 <paramref name="sourceType"/> 和目标类型 <paramref name="destinationType"/> 均不是基础类型。
        /// </summary>
        /// <param name="sourceType"></param>
        /// <param name="destinationType"></param>
        /// <returns></returns>
        public override bool IsMatch(Type sourceType, Type destinationType)
        {
            if (destinationType.IsAbstract)
            {
                return false;
            }

            if (sourceType.IsSimple() || destinationType.IsSimple())
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <param name="sourceExpression"><inheritdoc/></param>
        /// <param name="sourceType"><inheritdoc/></param>
        /// <param name="destinationExpression"><inheritdoc/></param>
        /// <param name="destinationType"><inheritdoc/></param>
        /// <param name="configuration"><inheritdoc/></param>
        /// <returns><inheritdoc/></returns>
        protected override Expression ToSolve(Expression sourceExpression, Type sourceType, ParameterExpression destinationExpression, Type destinationType, IMapConfiguration configuration)
        {
            var propertyInfos = Array.FindAll(destinationType.GetProperties(), x => x.CanWrite);

            var expressions = new List<Expression>(propertyInfos.Length);

            propertyInfos.JoinEach(sourceType.GetProperties(), x => x.Name, y => y.Name, (x, y) =>
            {
                if (y.CanRead)
                {
                    var sourcePrt = Property(sourceExpression, y);
                    var destinationPrt = Property(destinationExpression, x);

                    expressions.Add(Assign(destinationExpression, configuration.Map(sourcePrt, x.PropertyType)));
                }
            }, StringComparer.OrdinalIgnoreCase);

            return Block(expressions);
        }
    }
}
