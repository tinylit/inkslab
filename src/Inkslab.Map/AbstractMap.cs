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
        public abstract bool IsMatch(Type sourceType, Type destinationType);

        public virtual Expression ToSolve(Expression sourceExpression, Type sourceType, Type destinationType, IMapConfiguration configuration)
        {
            var instanceExpression = CreateNew(destinationType);

            var destinationExpression = Variable(destinationType);

            var bodyExp = ToSolve(sourceExpression, sourceType, destinationExpression, destinationType, configuration);

            return Block(destinationType, new ParameterExpression[] { destinationExpression }, new Expression[] { Assign(destinationExpression, instanceExpression), bodyExp, destinationExpression });
        }

        protected abstract Expression ToSolve(Expression sourceExpression, Type sourceType, ParameterExpression destinationExpression, Type destinationType, IMapConfiguration configuration);

        private static NewExpression CreateNew(Type destinationType)
        {
            var constructorInfo = destinationType.GetConstructor(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, null, Type.EmptyTypes, null);

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
