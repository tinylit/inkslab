using Inkslab.Collections;
using Inkslab.Map.Visitors;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace Inkslab.Map.Expressions
{
    using static Expression;

    /// <summary>
    /// 配置。
    /// </summary>
    public abstract class ProfileExpression<TMapper, TConfiguration> : Profile, IMapConfiguration, IConfiguration, IProfile where TMapper : ProfileExpression<TMapper, TConfiguration> where TConfiguration : class, IMapConfiguration, IConfiguration
    {
        private readonly TConfiguration configuration;

        private readonly ConcurrentDictionary<Tuple<Type, Type>, bool> matchCachings = new ConcurrentDictionary<Tuple<Type, Type>, bool>();

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

        bool IMapConfiguration.IsMatch(Type sourceType, Type destinationType) => matchCachings.GetOrAdd(Tuple.Create(sourceType, destinationType), tuple => base.IsMatch(tuple.Item1, tuple.Item2)) || configuration.IsMatch(sourceType, destinationType);

        Expression IMapConfiguration.Map(Expression sourceExpression, Type destinationType) => Map(sourceExpression, destinationType);

        /// <summary>
        /// 映射。
        /// </summary>
        /// <param name="sourceExpression">源类型表达式。</param>
        /// <param name="destinationType">目标类型。</param>
        /// <returns>目标类型 <paramref name="destinationType"/> 的计算结果。</returns>
        /// <exception cref="InvalidCastException">无法转换。</exception>
        protected virtual Expression Map(Expression sourceExpression, Type destinationType)
        {
            var sourceType = sourceExpression.Type;

            if (IsMatch(sourceType, destinationType))
            {
                return Map(sourceExpression, destinationType, this);
            }

            if (configuration.IsMatch(sourceType, destinationType))
            {
                return configuration.Map(sourceExpression, destinationType);
            }

            throw new InvalidCastException($"无法从【{sourceType}】源映射到【{destinationType}】的类型！");
        }

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
                return default(TDestination);
            }

            var sourceType = source.GetType();
            var destinationType = typeof(TDestination);

            if (sourceType.IsClass && destinationType.IsClass && matchCachings.GetOrAdd(Tuple.Create(sourceType, destinationType), tuple => base.IsMatch(tuple.Item1, tuple.Item2)))
            {
                return (TDestination)routerCachings.GetOrAdd(destinationType, Type => new MapRouter(Type))
                        .Map(this, source);
            }

            if (destinationType.IsValueType)
            {
                return Mapper<TDestination>.Map(configuration, source);
            }

            return (TDestination)Mapper.Map(configuration, source, destinationType);
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

            var sourceType = source.GetType();

            //? 自己独有的。
            if (sourceType.IsClass && destinationType.IsClass && IsMatch(sourceType, destinationType))
            {
                return routerCachings.GetOrAdd(destinationType, Type => new MapRouter(Type))
                        .Map(this, source);
            }

            return Mapper.Map(configuration, source, destinationType);
        }

        /// <summary>
        /// 释放资源。
        /// </summary>
        /// <param name="disposing">是否深度释放。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {

            }

            foreach (var kv in routerCachings)
            {
                kv.Value.Dispose();
            }

            routerCachings.Clear();

            matchCachings.Clear();

            base.Dispose(disposing);
        }

        private readonly ConcurrentDictionary<Type, MapRouter> routerCachings = new ConcurrentDictionary<Type, MapRouter>();

        private class MapRouter : IDisposable
        {
            private readonly Type destinationType;
            private readonly ConcurrentDictionary<Type, Func<object, object>> cachings = new ConcurrentDictionary<Type, Func<object, object>>();

            public MapRouter(Type destinationType)
            {
                this.destinationType = destinationType;
            }

            public object Map(ProfileExpression<TMapper, TConfiguration> mapper, object source)
            {
                var sourceType = source.GetType();

                if (sourceType.IsNullable())
                {
                    sourceType = Nullable.GetUnderlyingType(sourceType);
                }

                var factory = cachings.GetOrAdd(sourceType, type =>
                {
                    var sourceExp = Variable(type);

                    var bodyExp = Mapper.Visit(mapper.Map(sourceExp, destinationType));

                    if (!destinationType.IsAssignableFrom(bodyExp.Type))
                    {
                        throw new InvalidOperationException();
                    }

                    var parameterExp = Parameter(MapConstants.ObjectType);

                    var expressions = new List<Expression>
                    {
                        Assign(sourceExp, Convert(parameterExp, type))
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

                    var lambda = Lambda<Func<object, object>>(Block(MapConstants.ObjectType, new ParameterExpression[] { sourceExp }, expressions), parameterExp);

                    return lambda.Compile();
                });

                return factory.Invoke(source);
            }

            public void Dispose() => cachings.Clear();
        }

        private interface IMapper
        {
            object Map(TConfiguration mapper, object source);
        }

        private static class Mapper
        {
            private static readonly Type MapperType = typeof(TMapper);
            private static readonly Type Mapper_T_Type = typeof(Mapper<>);
            private static readonly Type TConfigurationType = typeof(TConfiguration);
            private static readonly LRU<Type, IMapper> cachings = new LRU<Type, IMapper>(destinationType => (IMapper)Activator.CreateInstance(Mapper_T_Type.MakeGenericType(MapperType, TConfigurationType, destinationType)));

            public static object Map(TConfiguration mapper, object source, Type destinationType)
            {
                return cachings.Get(destinationType)
                        .Map(mapper, source);
            }

            public static Expression Visit(Expression node) => MapperExpressionVisitor.Instance.Visit(node);
        }

        private class Mapper<TDestination> : IMapper
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

            public static TDestination Map(TConfiguration mapper, object source)
            {
                var sourceType = source.GetType();

                if (sourceType.IsNullable())
                {
                    sourceType = Nullable.GetUnderlyingType(sourceType);
                }

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

                if (!mapper.IsMatch(sourceType, conversionType))
                {
                    throw new InvalidCastException();
                }

                var factory = cachings.GetOrAdd(sourceType, type =>
                {
                    var sourceExp = Variable(type);

                    var bodyExp = Mapper.Visit(mapper.Map(sourceExp, conversionType));

                    if (!destinationType.IsAssignableFrom(bodyExp.Type))
                    {
                        throw new InvalidOperationException();
                    }

                    var parameterExp = Parameter(MapConstants.ObjectType);

                    var expressions = new List<Expression>
                    {
                        Assign(sourceExp, Convert(parameterExp, type))
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

                    var lambda = Lambda<Func<object, TDestination>>(Block(new ParameterExpression[] { sourceExp }, expressions), parameterExp);

                    return lambda.Compile();
                });

                return factory.Invoke(source);
            }

            object IMapper.Map(TConfiguration mapper, object source) => Map(mapper, source);
        }

        /// <summary>
        /// 映射表达式访问器。
        /// </summary>
        private class MapperExpressionVisitor : ExpressionVisitor
        {
            private MapperExpressionVisitor() { }

            protected override Expression VisitMethodCall(MethodCallExpression node)
            {
                var visitor = new IgnoreIfNullExpressionVisitor(this);

                var arguments = new List<Expression>(node.Arguments.Count);

                foreach (var argument in node.Arguments)
                {
                    arguments.Add(visitor.Visit(argument));
                }

                var objectNode = visitor.Visit(node.Object);

                var resultNode = node.Update(objectNode, arguments);

                if (visitor.HasIgnore)
                {
                    if (node.Type == MapConstants.VoidType)
                    {
                        return IfThen(visitor.Test, resultNode);
                    }

                    return Condition(visitor.Test, resultNode, Default(resultNode.Type));
                }

                return resultNode;
            }

            protected override Expression VisitNew(NewExpression node)
            {
                var visitor = new IgnoreIfNullExpressionVisitor(this);

                var arguments = new List<Expression>(node.Arguments.Count);

                foreach (var argument in node.Arguments)
                {
                    arguments.Add(visitor.Visit(argument));
                }

                var resultNode = node.Update(arguments);

                if (visitor.HasIgnore)
                {
                    return Block(new Expression[] { IfThen(Not(visitor.Test), Throw(Expression.New(typeof(InvalidCastException)))), resultNode });
                }

                return resultNode;
            }

            protected override Expression VisitMemberInit(MemberInitExpression node)
            {
                var visitor = new IgnoreIfNullExpressionVisitor(this);

                var newNode = node.NewExpression;

                var arguments = new List<Expression>(newNode.Arguments.Count);

                foreach (var argument in newNode.Arguments)
                {
                    arguments.Add(visitor.Visit(argument));
                }

                var parameterNode = Parameter(node.Type);

                var bindings = new List<MemberBinding>();
                var expressions = new List<Expression>();

                foreach (var binding in node.Bindings)
                {
                    if ((binding.Member.MemberType == MemberTypes.Field || binding.Member.MemberType == MemberTypes.Property) && binding is MemberAssignment assignment)
                    {
                        var ignoreVisitor = new IgnoreIfNullExpressionVisitor(this);

                        var bodyNode = ignoreVisitor.Visit(assignment.Expression);

                        if (ignoreVisitor.HasIgnore)
                        {
                            expressions.Add(IfThen(ignoreVisitor.Test, Assign(MakeMemberAccess(parameterNode, assignment.Member), bodyNode)));
                        }
                        else
                        {
                            bindings.Add(assignment.Update(bodyNode));
                        }
                    }
                }

                var resultNewNode = newNode.Update(arguments);

                Expression resultNode = bindings.Count == 0
                    ? resultNewNode
                    : node.Update(resultNewNode, bindings);

                if (expressions.Count == 0)
                {
                    if (visitor.HasIgnore)
                    {
                        return Block(new Expression[] { IfThen(Not(visitor.Test), Throw(Expression.New(typeof(InvalidCastException)))), resultNode });
                    }

                    return resultNode;
                }

                expressions.Add(parameterNode);

                var lambdaNode = Lambda(Block(node.Type, expressions), parameterNode);

                if (visitor.HasIgnore)
                {
                    return Invoke(lambdaNode, Block(new Expression[] { IfThen(Not(visitor.Test), Throw(Expression.New(typeof(InvalidCastException)))), resultNode }));
                }

                return Invoke(lambdaNode, resultNode);
            }

            protected override Expression VisitBlock(BlockExpression node)
            {
                if (node.Type == MapConstants.VoidType)
                {
                    var visitor = new IgnoreIfNullExpressionVisitor(this);

                    var expressions = new List<Expression>(node.Expressions.Count + 1);

                    foreach (var item in node.Expressions)
                    {
                        expressions.Add(visitor.Visit(item));
                    }

                    if (visitor.HasIgnore)
                    {
                        expressions.Insert(0, IfThen(Not(visitor.Test), Throw(Expression.New(typeof(InvalidCastException)))));
                    }

                    return node.Update(node.Variables, expressions);
                }

                return base.VisitBlock(node);
            }

            protected override Expression VisitBinary(BinaryExpression node)
            {
                var visitor = new IgnoreIfNullExpressionVisitor(this);

                var rightNode = visitor.Visit(node.Right);

                var resultNode = node.Update(node.Left, node.Conversion, rightNode);

                if (visitor.HasIgnore)
                {
                    return IfThen(visitor.Test, resultNode);
                }

                return resultNode;
            }

            protected override Expression VisitInvocation(InvocationExpression node)
            {
                var visitor = new IgnoreIfNullExpressionVisitor(this);
                var arguments = new List<Expression>(node.Arguments.Count);

                foreach (var argument in node.Arguments)
                {
                    arguments.Add(visitor.Visit(argument));
                }

                var bodyNode = visitor.Visit(node.Expression);

                var resultNode = node.Update(bodyNode, arguments);

                if (visitor.HasIgnore)
                {
                    if (node.Type == MapConstants.VoidType)
                    {
                        return IfThen(visitor.Test, resultNode);
                    }

                    return Condition(visitor.Test, resultNode, Default(node.Type));
                }

                return resultNode;
            }

            protected override Expression VisitSwitch(SwitchExpression node)
            {
                var cases = new List<SwitchCase>(node.Cases.Count + 1);

                foreach (var switchCase in node.Cases)
                {
                    var visitor = new IgnoreIfNullExpressionVisitor(this);

                    var testValues = new List<Expression>(switchCase.TestValues.Count + 1);

                    foreach (var testValue in switchCase.TestValues)
                    {
                        testValues.Add(visitor.Visit(testValue));
                    }

                    if (visitor.HasIgnore)
                    {
                        testValues.Insert(0, visitor.Test);
                    }

                    cases.Add(switchCase.Update(switchCase.TestValues, base.Visit(switchCase.Body)));
                }

                var switchVisitor = new IgnoreIfNullExpressionVisitor(this);

                var switchValue = switchVisitor.Visit(node.SwitchValue);

                var resultNode = node.Update(switchValue, cases, base.Visit(node.DefaultBody));

                if (switchVisitor.HasIgnore)
                {
                    return Condition(switchVisitor.Test, resultNode.DefaultBody, resultNode);
                }

                return resultNode;
            }

            public static ExpressionVisitor Instance = new MapperExpressionVisitor();
        }
    }
}
