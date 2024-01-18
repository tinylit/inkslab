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

            return routerCachings.GetOrAdd(typeof(TDestination), runtimeType => new MapperDestination(runtimeType))
                .Map<TDestination>(this, source);
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

            return routerCachings.GetOrAdd(destinationType, runtimeType => new MapperDestination(runtimeType))
                .Map(this, source);
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

        private class MapperDestination : IDisposable
        {
            private readonly Type runtimeType;
            private readonly ConcurrentDictionary<Type, Delegate> valueTypeCachings = new ConcurrentDictionary<Type, Delegate>();
            private readonly ConcurrentDictionary<Type, Func<object, object>> cachings = new ConcurrentDictionary<Type, Func<object, object>>();

            public MapperDestination(Type runtimeType)
            {
                this.runtimeType = runtimeType;

                if (runtimeType.IsInterface)
                {
                    if (runtimeType.IsGenericType)
                    {
                        var typeDefinition = runtimeType.GetGenericTypeDefinition();

                        if (typeDefinition == typeof(IList<>)
                            || typeDefinition == typeof(IReadOnlyList<>)
                            || typeDefinition == typeof(ICollection<>)
                            || typeDefinition == typeof(IReadOnlyCollection<>)
                            || typeDefinition == typeof(IEnumerable<>))
                        {
                            this.runtimeType = typeof(List<>).MakeGenericType(runtimeType.GetGenericArguments());
                        }
                        else if (typeDefinition == typeof(IDictionary<,>)
                                 || typeDefinition == typeof(IReadOnlyDictionary<,>))
                        {
                            this.runtimeType = typeof(Dictionary<,>).MakeGenericType(runtimeType.GetGenericArguments());
                        }
                    }
                    else if (runtimeType == typeof(IEnumerable)
                             || runtimeType == typeof(ICollection)
                             || runtimeType == typeof(IList))
                    {
                        this.runtimeType = typeof(List<object>);
                    }
                }
            }

            private static Type ToDestinationType(Type sourceType, Type runtimeType)
            {
                if (runtimeType.IsInterface || runtimeType.IsAbstract)
                {
                    if (runtimeType.IsAssignableFrom(sourceType))
                    {
                        return sourceType;
                    }

                    if (runtimeType.IsInterface)
                    {
                        throw new InvalidCastException($"无法推测有效的接口（{runtimeType.Name}）实现，无法进行({sourceType.Name}=>{runtimeType.Name})转换!");
                    }

                    throw new InvalidCastException($"无法推测有效的抽象类（{runtimeType.Name}）实现，无法进行({sourceType.Name}=>{runtimeType.Name})转换!");
                }

                return runtimeType == MapConstants.ObjectType
                    ? sourceType
                    : runtimeType;
            }

            private static Tuple<BlockExpression, ParameterExpression> Map(IMapApplication application, Type sourceType, Type runtimeType)
            {
                var destinationType = ToDestinationType(sourceType, runtimeType);

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

                bool destinationTypeIsObject = runtimeType == MapConstants.ObjectType;

                switch (bodyExp)
                {
                    case LambdaExpression lambdaExp:

                        if (lambdaExp.Parameters.Count != 1)
                        {
                            throw new InvalidOperationException();
                        }

                        var parameter = lambdaExp.Parameters[0];

                        if (!ReferenceEquals(parameter, sourceExp))
                        {
                            if (!parameter.Type.IsAssignableFrom(sourceExp.Type))
                            {
                                throw new InvalidOperationException();
                            }

                            var visitor = new ReplaceExpressionVisitor(parameter, sourceExp);

                            var body = visitor.Visit(lambdaExp.Body)!;

                            expressions.Add(destinationTypeIsObject
                                ? Convert(body, MapConstants.ObjectType)
                                : body);

                            break;
                        }

                        expressions.Add(destinationTypeIsObject
                            ? Convert(lambdaExp.Body, MapConstants.ObjectType)
                            : lambdaExp.Body);

                        break;
                    default:
                        expressions.Add(destinationTypeIsObject
                            ? Convert(bodyExp, MapConstants.ObjectType)
                            : bodyExp);

                        break;
                }

                return Tuple.Create(Block(runtimeType, new ParameterExpression[] { sourceExp }, expressions), parameterExp);
            }

            public object Map(IMapApplication application, object source)
            {
                var sourceType = source.GetType();

                var factory = cachings.GetOrAdd(Nullable.GetUnderlyingType(sourceType) ?? sourceType, type =>
                {
                    var tuple = Map(application, type, runtimeType);

                    var lambda = Lambda<Func<object, object>>(Convert(tuple.Item1, MapConstants.ObjectType), tuple.Item2);
                    
                    return lambda.Compile();
                });

                return factory.Invoke(source);
            }

            public TDestination Map<TDestination>(IMapApplication application, object source)
            {
                if (!runtimeType.IsValueType)
                {
                    return (TDestination)Map(application, source);
                }

                var sourceType = source.GetType();

                var factory = valueTypeCachings.GetOrAdd(Nullable.GetUnderlyingType(sourceType) ?? sourceType, type =>
                {
                    var tuple = Map(application, type, runtimeType);

                    var lambda = Lambda<Func<object, TDestination>>(tuple.Item1, tuple.Item2);

                    return lambda.Compile();
                });

                return ((Func<object, TDestination>)factory).Invoke(source);
            }

            public void Dispose()
            {
                cachings.Clear();
                valueTypeCachings.Clear();

                GC.SuppressFinalize(this);
            }
        }
    }
}