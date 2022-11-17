using Inkslab.Collections;
using System.Collections;
using System.Collections.Concurrent;
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

        bool IConfiguration.IsDepthMapping => configuration.IsDepthMapping;

        bool IConfiguration.AllowPropagationNullValues => configuration.AllowPropagationNullValues;

        protected ProfileExpression(TConfiguration configuration)
        {
            this.configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        bool IMapConfiguration.IsMatch(Type sourceType, Type destinationType) => IsMatch(sourceType, destinationType) || configuration.IsMatch(sourceType, destinationType);

        Expression IMapConfiguration.Map(Expression sourceExpression, Type destinationType) => Map(sourceExpression, destinationType);

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

        public virtual TDestination Map<TDestination>(object source)
        {
            if (source is null)
            {
                return default(TDestination);
            }

            var sourceType = source.GetType();
            var destinationType = typeof(TDestination);

            if (IsMatch(sourceType, destinationType))
            {
                var router = routerCachings
                        .GetOrAdd(destinationType, type => new MapRouter<TDestination>(type));

                if (router is MapRouter<TDestination> mapRouter)
                {
                    return mapRouter.Map(this, source);
                }
            }

            return Mapper<TDestination>.Map(configuration, source);
        }

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

        private readonly ConcurrentDictionary<Type, IRouter> routerCachings = new ConcurrentDictionary<Type, IRouter>();

        private interface IRouter : IDisposable
        {
        }

        private class MapRouter<TDestination> : IRouter, IDisposable
        {
            private readonly Type destinationType;
            private readonly ConcurrentDictionary<Type, Func<object, TDestination>> cachings = new ConcurrentDictionary<Type, Func<object, TDestination>>();

            public MapRouter(Type destinationType)
            {
                this.destinationType = destinationType;
            }

            public TDestination Map(ProfileExpression<TMapper, TConfiguration> mapper, object source)
            {
                var factory = cachings.GetOrAdd(source.GetType(), type =>
                {
                    var sourceExp = Variable(type);

                    var bodyExp = Mapper<TDestination>.Visit(mapper.Map(sourceExp, destinationType));

                    if (bodyExp.Type != destinationType)
                    {
                        throw new InvalidOperationException();
                    }

                    var parameterExp = Parameter(typeof(object));

                    var expressions = new List<Expression>
                    {
                        Assign(sourceExp, Convert(parameterExp, type))
                    };

                    switch (bodyExp)
                    {
                        case LambdaExpression lambdaExp:

                            if (lambdaExp.Parameters.Count != 1 || !ReferenceEquals(lambdaExp.Parameters[0], sourceExp))
                            {
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

            public void Dispose()
            {
                cachings.Clear();

                GC.SuppressFinalize(this);
            }
        }

        private static class Mapper<TDestination>
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

            private static readonly LFU<Type, Func<object, TDestination>> cachings = new LFU<Type, Func<object, TDestination>>();

            public static TDestination Map(TConfiguration mapper, object source)
            {
                var sourceType = source.GetType();

                var conversionType = destinationType;

                if (sourceType.IsNullable())
                {
                    sourceType = Nullable.GetUnderlyingType(sourceType);
                }

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
                else if (runtimeType == typeof(object))
                {
                    conversionType = sourceType;
                }

                if (!mapper.IsMatch(sourceType, conversionType))
                {
                    throw new InvalidCastException();
                }

                var factory = cachings.GetOrCreate(sourceType, type =>
                {
                    var sourceExp = Variable(type);

                    var bodyExp = Visit(mapper.Map(sourceExp, conversionType));

                    if (bodyExp.Type != destinationType)
                    {
                        throw new InvalidOperationException();
                    }

                    var parameterExp = Parameter(typeof(object));

                    var expressions = new List<Expression>
                    {
                        Assign(sourceExp, Convert(parameterExp, type))
                    };

                    switch (bodyExp)
                    {
                        case LambdaExpression lambdaExp:

                            if (lambdaExp.Parameters.Count != 1 || !ReferenceEquals(lambdaExp.Parameters[0], sourceExp))
                            {
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

            public static Expression Visit(Expression node) => MapperExpressionVisitor.Instance.Visit(node);
        }

        /// <summary>
        /// 映射表达式访问器。
        /// </summary>
        private class MapperExpressionVisitor : ExpressionVisitor
        {
            private MapperExpressionVisitor() { }

            protected override Expression VisitMethodCall(MethodCallExpression node)
            {
                var visitor = new IgnoreIfNullExpressionVisitor();

                foreach (var argument in node.Arguments)
                {
                    visitor.Visit(argument);
                }

                visitor.Visit(node.Object);

                if (visitor.HasIgnore)
                {
                    if (node.Type == typeof(void))
                    {
                        return IfThen(visitor.Test, node);
                    }

                    return Condition(visitor.Test, node, Default(node.Type));
                }

                return node;
            }

            protected override Expression VisitNew(NewExpression node)
            {
                var visitor = new IgnoreIfNullExpressionVisitor();

                foreach (var argument in node.Arguments)
                {
                    visitor.Visit(argument);
                }

                if (visitor.HasIgnore)
                {
                    return Condition(visitor.Test, node, Throw(New(typeof(InvalidCastException))), node.Type);
                }

                return node;
            }

            protected override Expression VisitMemberInit(MemberInitExpression node)
            {
                var visitor = new IgnoreIfNullExpressionVisitor();

                foreach (var argument in node.NewExpression.Arguments)
                {
                    visitor.Visit(argument);
                }

                if (visitor.HasIgnore)
                {
                    var parameterExp = Parameter(node.Type);

                    var expressions = new List<Expression>(node.Bindings.Count + 1);

                    foreach (var binding in node.Bindings)
                    {
                        if ((binding.Member.MemberType == MemberTypes.Field || binding.Member.MemberType == MemberTypes.Property) && binding is MemberAssignment assignment)
                        {
                            var ignoreVisitor = new IgnoreIfNullExpressionVisitor();

                            ignoreVisitor.Visit(assignment.Expression);

                            if (ignoreVisitor.HasIgnore)
                            {
                                expressions.Add(IfThen(ignoreVisitor.Test, Assign(MakeMemberAccess(parameterExp, assignment.Member), assignment.Expression)));
                            }
                            else
                            {
                                expressions.Add(Assign(MakeMemberAccess(parameterExp, assignment.Member), assignment.Expression));
                            }
                        }
                        else
                        {
                            throw new InvalidCastException();
                        }
                    }

                    expressions.Add(parameterExp);

                    var lambdaExp = Lambda(Block(node.Type, expressions), parameterExp);

                    return Invoke(lambdaExp, Condition(visitor.Test, node.NewExpression, Throw(New(typeof(InvalidCastException))), node.NewExpression.Type));
                }

                return VisitMemberInitValid(node);
            }

            private static Expression VisitMemberInitValid(MemberInitExpression node)
            {
                var bindings = new List<MemberAssignment>();
                var conditions = new List<Expression>();

                foreach (var binding in node.Bindings)
                {
                    if ((binding.Member.MemberType == MemberTypes.Field || binding.Member.MemberType == MemberTypes.Property) && binding is MemberAssignment assignment)
                    {
                        var visitor = new IgnoreIfNullExpressionVisitor();

                        visitor.Visit(assignment.Expression);

                        if (visitor.HasIgnore)
                        {
                            bindings.Add(assignment);

                            conditions.Add(visitor.Test);
                        }
                    }
                }

                if (bindings.Count == 0)
                {
                    return node;
                }

                var parameterExp = Parameter(node.Type);

                var expressions = new List<Expression>(bindings.Count + 1);

                expressions.AddRange(bindings.Zip(conditions, (x, y) => IfThen(y, Assign(MakeMemberAccess(parameterExp, x.Member), x.Expression))));
                expressions.Add(parameterExp);

                var lambdaExp = Lambda(Block(node.Type, expressions), parameterExp);

                if (bindings.Count == node.Bindings.Count)
                {
                    return Invoke(lambdaExp, node.NewExpression);
                }

                return Invoke(lambdaExp, node.Update(node.NewExpression, node.Bindings.Except(bindings)));
            }

            protected override Expression VisitBinary(BinaryExpression node)
            {
                var visitor = new IgnoreIfNullExpressionVisitor();

                visitor.Visit(node.Right);

                if (visitor.HasIgnore)
                {
                    return IfThen(visitor.Test, node);
                }

                return node;
            }

            protected override Expression VisitInvocation(InvocationExpression node)
            {
                var visitor = new IgnoreIfNullExpressionVisitor();

                foreach (var argument in node.Arguments)
                {
                    visitor.Visit(argument);
                }

                if (visitor.HasIgnore)
                {
                    if (node.Type == typeof(void))
                    {
                        return IfThen(visitor.Test, node);
                    }

                    return Condition(visitor.Test, node, Default(node.Type));
                }

                return node;
            }

            public static ExpressionVisitor Instance = new MapperExpressionVisitor();
        }
    }
}
