using Inkslab.Map.Visitors;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Inkslab.Map.Expressions
{
    using static Expression;

    /// <summary>
    /// 配置。
    /// </summary>
    public abstract class ProfileExpression<TMapper, TConfiguration> : Profile, IMapApplication, IConfiguration, IProfile, IMap where TMapper : ProfileExpression<TMapper, TConfiguration> where TConfiguration : class, IMapConfiguration, IConfiguration
    {
        private readonly TConfiguration configuration;

        bool IConfiguration.IsDepthMapping => configuration.IsDepthMapping;

        bool IConfiguration.AllowPropagationNullValues => configuration.AllowPropagationNullValues;

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="configuration">映射配置。</param>
        /// <exception cref="ArgumentNullException">参数 <paramref name="configuration"/> is null.</exception>
        protected ProfileExpression(TConfiguration configuration)
        {
            this.configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        bool IMap.IsMatch(Type sourceType, Type destinationType) => base.IsMatch(sourceType, destinationType);

        Expression IMap.ToSolve(Expression sourceExpression, Type destinationType, IMapApplication application) => Map(sourceExpression, destinationType, application);

        /// <inheritdoc/>
        public override bool IsMatch(Type sourceType, Type destinationType) => base.IsMatch(sourceType, destinationType) || configuration.IsMatch(sourceType, destinationType);

        Expression IMapApplication.Map(Expression sourceExpression, Type destinationType) => Map(sourceExpression, destinationType);

        /// <summary>
        /// 映射。
        /// </summary>
        /// <param name="sourceExpression">源类型表达式。</param>
        /// <param name="destinationType">目标类型。</param>
        /// <returns>目标类型 <paramref name="destinationType"/> 的计算结果。</returns>
        /// <exception cref="InvalidCastException">无法转换。</exception>
        protected virtual Expression Map(Expression sourceExpression, Type destinationType) => configuration.Map(sourceExpression, destinationType, this);

        /// <summary>
        /// 映射。
        /// </summary>
        /// <typeparam name="TDestination">目标类型。</typeparam>
        /// <param name="source">源对象。</param>
        /// <returns>目标类型对象。</returns>
        public virtual TDestination Map<TDestination>(object source)
        {
            if (source is null)
            {
                return default;
            }

            var sourceType = source.GetType();
            var destinationType = typeof(TDestination);

            if (sourceType.IsValueType || destinationType.IsValueType)
            {
                return Mapper<TDestination>.Map(this, source);
            }

            return (TDestination)routerCachings.GetOrAdd(destinationType, runtimeType => new MapperDestination(this, runtimeType))
                .Map(source);
        }

        /// <summary>
        /// 映射。
        /// </summary>
        /// <param name="source">源对象。</param>
        /// <param name="destinationType">目标类型。</param>
        /// <returns>目标类型对象。</returns>
        /// <exception cref="ArgumentNullException">参数 <paramref name="destinationType"/> is null.</exception>
        public virtual object Map(object source, Type destinationType)
        {
            if (destinationType is null)
            {
                throw new ArgumentNullException(nameof(destinationType));
            }

            if (source is null)
            {
                return null;
            }

            return routerCachings.GetOrAdd(destinationType, runtimeType => new MapperDestination(this, runtimeType))
                .Map(source);
        }

        /// <summary>
        /// 释放资源。
        /// </summary>
        /// <param name="disposing">是否深度释放。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                foreach (var kv in routerCachings)
                {
                    kv.Value.Dispose();
                }

                routerCachings.Clear();
            }

            base.Dispose(disposing);
        }

        private readonly ConcurrentDictionary<Type, MapperDestination> routerCachings = new ConcurrentDictionary<Type, MapperDestination>();

        private class Mapper
        {
            protected static Tuple<BlockExpression, ParameterExpression> Map(IMapApplication application, Type sourceType, Type destinationType)
            {
                var sourceExp = Variable(sourceType, "source");

                var bodyExp = application.Map(sourceExp, destinationType);

                if (!destinationType.IsAssignableFrom(bodyExp.Type))
                {
                    throw new InvalidOperationException();
                }

                var parameterExp = Parameter(MapConstants.ObjectType, "arg");

                var expressions = new List<Expression>
                {
                    Assign(sourceExp, Convert(parameterExp, sourceType))
                };

                switch (bodyExp)
                {
                    case LambdaExpression lambdaExp:

                        if (lambdaExp.Parameters.Count != 1)
                        {
                            throw new InvalidOperationException();
                        }

                        var parameterByLambdaExp = lambdaExp.Parameters[0];

                        if (!ReferenceEquals(parameterByLambdaExp, sourceExp))
                        {
                            if (parameterByLambdaExp.Type.IsAssignableFrom(sourceExp.Type))
                            {
                                var visitor = new ReplaceExpressionVisitor(parameterByLambdaExp, sourceExp);

                                expressions.Add(visitor.Visit(lambdaExp.Body));

                                break;
                            }

                            throw new InvalidOperationException();
                        }

                        expressions.Add(lambdaExp.Body);

                        break;
                    default:
                        expressions.Add(bodyExp);

                        break;
                }

                return Tuple.Create(Block(new ParameterExpression[] { sourceExp }, expressions), parameterExp);
            }
        }

        private class Mapper<TDestination> : Mapper
        {
            private static readonly Type runtimeType;

            private static readonly Type destinationType;

            static Mapper()
            {
                runtimeType = destinationType = typeof(TDestination);

                if (destinationType.IsInterface)
                {
                    if (destinationType.IsGenericType)
                    {
                        var typeDefinition = destinationType.GetGenericTypeDefinition();

                        if (typeDefinition == typeof(IList<>)
                            || typeDefinition == typeof(IReadOnlyList<>)
                            || typeDefinition == typeof(ICollection<>)
                            || typeDefinition == typeof(IReadOnlyCollection<>)
                            || typeDefinition == typeof(IEnumerable<>))
                        {
                            destinationType = typeof(List<>).MakeGenericType(destinationType.GetGenericArguments());
                        }
                        else if (typeDefinition == typeof(IDictionary<,>)
                                 || typeDefinition == typeof(IReadOnlyDictionary<,>))
                        {
                            destinationType = typeof(Dictionary<,>).MakeGenericType(destinationType.GetGenericArguments());
                        }
                    }
                    else if (destinationType == typeof(IEnumerable)
                             || destinationType == typeof(ICollection)
                             || destinationType == typeof(IList))
                    {
                        destinationType = typeof(List<object>);
                    }
                }
            }

            private static readonly ConcurrentDictionary<Type, Func<object, TDestination>> cachings = new ConcurrentDictionary<Type, Func<object, TDestination>>();

            public static TDestination Map(IMapApplication application, object source)
            {
                var sourceType = source.GetType();

                var factory = cachings.GetOrAdd(Nullable.GetUnderlyingType(sourceType) ?? sourceType, type =>
                {
                    var conversionType = destinationType;

                    if (runtimeType.IsInterface || runtimeType.IsAbstract)
                    {
                        if (runtimeType.IsAssignableFrom(sourceType))
                        {
                            if (conversionType.IsAbstract) //? 避免推测类型支持转换，但源类型不支持转换的问题。
                            {
                                conversionType = sourceType;
                            }
                        }
                        else if (conversionType.IsInterface)
                        {
                            throw new InvalidCastException($"无法推测有效的接口（{destinationType.Name}）实现，无法进行({sourceType.Name}=>{destinationType.Name})转换!");
                        }
                        else if (conversionType.IsAbstract)
                        {
                            throw new InvalidCastException($"无法推测有效的抽象类（{destinationType.Name}）实现，无法进行({sourceType.Name}=>{destinationType.Name})转换!");
                        }
                    }
                    else if (runtimeType == MapConstants.ObjectType)
                    {
                        conversionType = sourceType;
                    }

                    var tuple = Map(application, type, conversionType);

                    var lambda = Lambda<Func<object, TDestination>>(tuple.Item1, tuple.Item2);

                    return lambda.Compile();
                });

                return factory.Invoke(source);
            }
        }

        private class MapperDestination : Mapper, IDisposable
        {
            private readonly IMapApplication application;
            private readonly Type runtimeType;
            private readonly Type destinationType;
            private readonly ConcurrentDictionary<Type, Func<object, object>> cachings = new ConcurrentDictionary<Type, Func<object, object>>();

            public MapperDestination(IMapApplication application, Type destinationType)
            {
                this.application = application;

                this.destinationType = runtimeType = destinationType;

                if (destinationType.IsInterface)
                {
                    if (destinationType.IsGenericType)
                    {
                        var typeDefinition = destinationType.GetGenericTypeDefinition();

                        if (typeDefinition == typeof(IList<>)
                            || typeDefinition == typeof(IReadOnlyList<>)
                            || typeDefinition == typeof(ICollection<>)
                            || typeDefinition == typeof(IReadOnlyCollection<>)
                            || typeDefinition == typeof(IEnumerable<>))
                        {
                            this.destinationType = typeof(List<>).MakeGenericType(destinationType.GetGenericArguments());
                        }
                        else if (typeDefinition == typeof(IDictionary<,>)
                                 || typeDefinition == typeof(IReadOnlyDictionary<,>))
                        {
                            this.destinationType = typeof(Dictionary<,>).MakeGenericType(destinationType.GetGenericArguments());
                        }
                    }
                    else if (destinationType == typeof(IEnumerable)
                             || destinationType == typeof(ICollection)
                             || destinationType == typeof(IList))
                    {
                        this.destinationType = typeof(List<object>);
                    }
                }
            }

            public object Map(object source)
            {
                var sourceType = source.GetType();

                var factory = cachings.GetOrAdd(Nullable.GetUnderlyingType(sourceType) ?? sourceType, type =>
                {
                    var conversionType = destinationType;

                    if (runtimeType.IsInterface || runtimeType.IsAbstract)
                    {
                        if (runtimeType.IsAssignableFrom(sourceType))
                        {
                            conversionType = sourceType;
                        }
                        else if (destinationType.IsInterface)
                        {
                            throw new InvalidCastException($"无法推测有效的接口（{destinationType.Name}）实现，无法进行({sourceType.Name}=>{destinationType.Name})转换!");
                        }
                        else if (destinationType.IsAbstract)
                        {
                            throw new InvalidCastException($"无法推测有效的抽象类（{destinationType.Name}）实现，无法进行({sourceType.Name}=>{destinationType.Name})转换!");
                        }
                    }
                    else if (runtimeType == MapConstants.ObjectType)
                    {
                        conversionType = sourceType;
                    }

                    var tuple = Map(application, type, conversionType);

                    var lambda = Lambda<Func<object, object>>(Convert(tuple.Item1, MapConstants.ObjectType), tuple.Item2);

                    return lambda.Compile();
                });

                return factory.Invoke(source);
            }

            public void Dispose() => cachings.Clear();
        }
    }
}