using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Inkslab.Map
{
    using static Expression;

    /// <summary>
    /// 抽象映射类。
    /// </summary>
    public abstract class AbstractMap : IMap
    {
        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <param name="sourceType"><inheritdoc/></param>
        /// <param name="destinationType"><inheritdoc/></param>
        /// <returns><inheritdoc/></returns>
        public abstract bool IsMatch(Type sourceType, Type destinationType);

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <param name="sourceExpression"><inheritdoc/></param>
        /// <param name="sourceType"><inheritdoc/></param>
        /// <param name="destinationType"><inheritdoc/></param>
        /// <param name="configuration"><inheritdoc/></param>
        /// <returns><inheritdoc/></returns>
        public virtual Expression ToSolve(Expression sourceExpression, Type sourceType, Type destinationType, IMapConfiguration configuration)
        {
            var instanceExpression = CreateNew(destinationType);

            var destinationExpression = Variable(destinationType);

            var bodyExp = ToSolve(sourceExpression, sourceType, destinationExpression, destinationType, configuration);

            return Block(destinationType, new ParameterExpression[] { destinationExpression }, new Expression[] { Assign(destinationExpression, instanceExpression), bodyExp, destinationExpression });
        }

        /// <summary>
        /// 解决。
        /// </summary>
        /// <param name="sourceExpression">源对象表达式。</param>
        /// <param name="sourceType">源类型。</param>
        /// <param name="destinationExpression">目标对象表达式。</param>
        /// <param name="destinationType">目标对象表达式。</param>
        /// <param name="configuration">映射配置。</param>
        /// <returns>目标对象<paramref name="destinationType"/>的映射逻辑表达式。</returns>
        protected abstract Expression ToSolve(Expression sourceExpression, Type sourceType, ParameterExpression destinationExpression, Type destinationType, IMapConfiguration configuration);

        private static NewExpression CreateNew(Type destinationType)
        {
            var constructorInfo = destinationType.GetConstructor(MapConstants.InstanceBindingFlags, null, Type.EmptyTypes, null);

            if (constructorInfo is not null)
            {
                return New(constructorInfo);
            }

            var ctorWithOptionalArgs = destinationType
                .GetConstructors(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                .FirstOrDefault(c => c.GetParameters().All(p => p.IsOptional));

            if (ctorWithOptionalArgs is null)
            {
                throw new InvalidCastException($"目标【{destinationType}】必须包含一个无参构造函数或者只有可选参数的构造函数!");
            }

            return New(ctorWithOptionalArgs, ctorWithOptionalArgs.GetParameters().Select(DefaultValue));
        }

        private static Expression DefaultValue(ParameterInfo x)
        {
            if (x.DefaultValue is null && x.ParameterType.IsValueType)
            {
                return Default(x.ParameterType);
            }

            return Constant(x.DefaultValue, x.ParameterType);
        }
    }
}
