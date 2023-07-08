using Inkslab.Map.Visitors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Inkslab.Map
{
    using static Expression;

    /// <summary>
    /// 配置。
    /// </summary>
    public abstract class Profile : AbstractMap, IProfile, IDisposable
    {
        private class TypeCode
        {
            private readonly Type x;
            private readonly Type y;

            public TypeCode(Type x, Type y)
            {
                this.x = x;
                this.y = y;
            }

            public Type X => x;
            public Type Y => y;

            private class TypeCodeEqualityComparer : IEqualityComparer<TypeCode>
            {
                public bool Equals(TypeCode x, TypeCode y)
                {
                    if (x is null)
                    {
                        return y is null;
                    }

                    if (y is null)
                    {
                        return false;
                    }

                    return x.x == y.x && x.y == y.y;
                }

                public int GetHashCode(TypeCode obj)
                {
                    if (obj is null)
                    {
                        return 0;
                    }

                    var h1 = obj.x.GetHashCode();
                    var h2 = obj.y.GetHashCode();

                    return ((h1 << 5) + h1) ^ h2;
                }
            }

            public static IEqualityComparer<TypeCode> InstanceComparer = new TypeCodeEqualityComparer();
        }

        private class Slot
        {
            public Slot()
            {
                Ignore = true;
            }

            public Slot(Expression valueExpression)
            {
                ValueExpression = valueExpression;
            }

            public bool Ignore { get; }

            public Expression ValueExpression { get; }
        }

        private enum EsConstraints
        {
            None,
            ValueType,
            Enum,
            Nullable,
            NullableEnum,
            Inherit
        }

        private readonly struct ConstraintsSlot
        {
            private readonly Type typeConstraints;
            private readonly EsConstraints constraints;

            public ConstraintsSlot(Type typeConstraints, EsConstraints constraints)
            {
                this.typeConstraints = typeConstraints;
                this.constraints = constraints;
            }

            public bool IsMatch(Type argumentType)
            {
                switch (constraints)
                {
                    case EsConstraints.None:
                        return true;
                    case EsConstraints.ValueType:
                        return argumentType.IsValueType;
                    case EsConstraints.Enum:
                        return argumentType.IsEnum;
                    case EsConstraints.Nullable:
                        return argumentType.IsNullable();
                    case EsConstraints.NullableEnum:
                        if (argumentType.IsNullable())
                        {
                            var underlyingType = Nullable.GetUnderlyingType(argumentType);

                            return underlyingType.IsEnum;
                        }
                        return false;
                    case EsConstraints.Inherit:
                        return typeConstraints.IsAssignableFrom(argumentType);
                    default:
                        return false;
                }
            }
        }

        private class MapConstraints
        {
            private readonly Type typeDefinition;
            private readonly ConstraintsSlot[] constraintSlots;

            public MapConstraints(Type originalType, Type typeDefinition, ConstraintsSlot[] constraintSlots)
            {
                OriginalType = originalType;
                this.typeDefinition = typeDefinition;
                this.constraintSlots = constraintSlots;
            }

            public Type OriginalType { get; }

            public bool IsMatch(Type destinationType)
            {
                if (!destinationType.IsGenericType)
                {
                    return false;
                }

                var typeDefinition = destinationType.GetGenericTypeDefinition();

                if (typeDefinition != this.typeDefinition)
                {
                    return false;
                }

                var typeArguments = destinationType.GetGenericArguments();

                for (int i = 0; i < typeArguments.Length; i++)
                {
                    if (constraintSlots[i].IsMatch(typeArguments[i]))
                    {
                        continue;
                    }

                    return false;
                }

                return true;
            }
        }

        private const BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

        private class MapExpressionVisitor : ExpressionVisitor
        {
            private readonly Type originalSourceType;
            private readonly Type originalDestinationType;
            private readonly Type sourceType;
            private readonly Type destinationType;
            private readonly IMapConfiguration configuration;
            private readonly Expression[] originalParameters;
            private readonly Expression[] parameters;

            public MapExpressionVisitor(Type originalSourceType, Type originalDestinationType, Type sourceType, Type destinationType, IMapConfiguration configuration, Expression[] originalParameters, Expression[] parameters)
            {
                this.originalSourceType = originalSourceType;
                this.originalDestinationType = originalDestinationType;
                this.sourceType = sourceType;
                this.destinationType = destinationType;
                this.configuration = configuration;
                this.originalParameters = originalParameters;
                this.parameters = parameters;
            }

            public override Expression Visit(Expression node)
            {
                var indexOf = Array.IndexOf(originalParameters, node);

                if (indexOf > -1)
                {
                    return parameters[indexOf];
                }

                return base.Visit(node);
            }

            private bool MemberIsRef(ref MemberInfo memberInfo)
            {
                var declaringType = memberInfo.DeclaringType;

                if (declaringType.IsGenericType)
                {
                    if (originalDestinationType.IsGenericType && (declaringType == originalDestinationType || memberInfo.ReflectedType == originalDestinationType))
                    {
                        memberInfo = memberInfo.MemberType switch
                        {
                            MemberTypes.Property => destinationType.GetProperty(memberInfo.Name, bindingFlags),
                            MemberTypes.Field => destinationType.GetField(memberInfo.Name, bindingFlags),
                            MemberTypes.Event => destinationType.GetEvent(memberInfo.Name, bindingFlags),
                            MemberTypes.Method when memberInfo is MethodInfo methodInfo => MethodBase.GetMethodFromHandle(methodInfo.MethodHandle, destinationType.TypeHandle),
                            _ => Member(memberInfo, destinationType),
                        };

                        return true;
                    }
                }

                return false;

                static MemberInfo Member(MemberInfo memberInfo, Type destinationType)
                {
                    var memberInfos = destinationType.GetMember(memberInfo.Name, memberInfo.MemberType, bindingFlags);

                    if (memberInfos.Length == 1)
                    {
                        return memberInfos[0];
                    }

                    var declaringType = memberInfo.DeclaringType;

                    var typeDefintion = declaringType.GetGenericTypeDefinition();

                    return memberInfos.Single(x => x.DeclaringType.IsGenericType && x.DeclaringType.GetGenericTypeDefinition() == typeDefintion);
                }
            }

            protected override Expression VisitNew(NewExpression node)
            {
                if (node.Type == originalDestinationType && destinationType.IsGenericType)
                {
                    var constructorInfo = (ConstructorInfo)MethodBase.GetMethodFromHandle(node.Constructor.MethodHandle, destinationType.TypeHandle);

                    var arguments = new List<Expression>(node.Arguments.Count);

                    arguments.AddRange(node.Arguments.Zip(constructorInfo.GetParameters(), (x, y) => configuration.Map(Visit(x), y.ParameterType)));

                    if (node.Members is null || node.Members.Count == 0)
                    {
                        return Expression.New(constructorInfo, arguments);
                    }

                    var memberInfos = new List<MemberInfo>(node.Members.Count);

                    for (int i = 0; i < node.Members.Count; i++)
                    {
                        var memberInfo = memberInfos[i];

                        MemberIsRef(ref memberInfo);

                        memberInfos.Add(memberInfo);
                    }

                    return Expression.New(constructorInfo, arguments, memberInfos);
                }

                return base.VisitNew(node);
            }

            protected override Expression VisitMember(MemberExpression node)
            {
                var memberInfo = node.Member;

                var declaringType = memberInfo.DeclaringType;

                var instanceExpression = Visit(node.Expression);

                if (declaringType.IsGenericType && originalSourceType.IsGenericType && (declaringType == originalSourceType || memberInfo.ReflectedType == originalSourceType))
                {
                    return memberInfo.MemberType switch
                    {
                        MemberTypes.Property => Property(instanceExpression, sourceType.GetProperty(memberInfo.Name, bindingFlags)),
                        MemberTypes.Field => Field(instanceExpression, sourceType.GetField(memberInfo.Name, bindingFlags)),
                        MemberTypes.Method when memberInfo is MethodInfo method => Property(instanceExpression, (MethodInfo)MethodBase.GetMethodFromHandle(method.MethodHandle, sourceType.TypeHandle)),
                        _ => throw new NotSupportedException(),
                    };
                }

                return node.Update(instanceExpression);
            }

            protected override MemberAssignment VisitMemberAssignment(MemberAssignment node)
            {
                var memberInfo = node.Member;

                if (MemberIsRef(ref memberInfo))
                {
                    Type destinationType = memberInfo.MemberType switch
                    {
                        MemberTypes.Field => ((FieldInfo)memberInfo).FieldType,
                        MemberTypes.Property => ((PropertyInfo)memberInfo).PropertyType,
                        _ => throw new NotSupportedException(),
                    };

                    var valueExpression = configuration.Map(Visit(node.Expression), destinationType);

                    return Bind(memberInfo, valueExpression);
                }

                return node.Update(configuration.Map(Visit(node.Expression), node.Expression.Type));
            }
        }

        private class MapSlot : IDisposable
        {
            private readonly Type sourceType;
            private readonly Type destinationType;
            private readonly InstanceFactory instanceFactory;
            private readonly List<Type> destinationTypes;
            private readonly Dictionary<string, Slot> memberExpressions = new Dictionary<string, Slot>();

            private readonly bool hasInstanceSlot = false;
            private bool hasInumerableInstanceSlot = false;

            private bool isSourceConstraints;
            private bool isDestinationConstraints;

            private bool hasMatchConstraints = false;
            private int destinationConstraintsCount = 0;
            private MatchConstraints matchConstraints;
            private MapConstraints sourceConstraints;
            private List<MapConstraints> destinationConstraints;
            private Dictionary<TypeCode, InstanceEnumerableFactory> enumerableInstanceFactories;

            public MapSlot(Type sourceType, Type destinationType)
            {
                this.sourceType = sourceType;
                this.destinationType = destinationType;

                destinationTypes = new List<Type>(1) { destinationType };
            }

            public MapSlot(Type sourceType, Type destinationType, InstanceFactory instanceFactory) : this(sourceType, destinationType)
            {
                hasInstanceSlot = true;

                this.instanceFactory = instanceFactory;
            }

            public bool HasInstanceSlot => hasInstanceSlot || hasInumerableInstanceSlot;

            public bool HasMemberSettings => memberExpressions.Count > 0;

            public void Include(Type destinationType) => destinationTypes.Add(destinationType);

            public void IncludeConstraints(MatchConstraints matchConstraints)
            {
                hasMatchConstraints = true;

                this.matchConstraints = matchConstraints;

                foreach (var destinationType in destinationTypes.Skip(destinationConstraintsCount))
                {
                    if (TryMapConstraints(destinationType, out MapConstraints mapConstraints))
                    {
                        if (isDestinationConstraints)
                        {
                            destinationConstraints.Add(mapConstraints);
                        }
                        else
                        {
                            isDestinationConstraints = true;

                            destinationConstraints = new List<MapConstraints> { mapConstraints };
                        }
                    }
                }

                if (isDestinationConstraints)
                {
                    if (TryMapConstraints(sourceType, out MapConstraints mapConstraints))
                    {
                        sourceConstraints = mapConstraints;

                        isSourceConstraints = true;
                    }
                }

                destinationConstraintsCount = destinationTypes.Count;
            }

            private static bool TryMapConstraints(Type type, out MapConstraints mapConstraints)
            {
                mapConstraints = null;

                if (!type.IsGenericType)
                {
                    return false;
                }

                var genericArguments = type.GetGenericArguments();
                var typeDefinition = type.GetGenericTypeDefinition();
                var typeArguments = typeDefinition.GetGenericArguments();

                var constraintSlots = new ConstraintsSlot[genericArguments.Length];

                for (var i = 0; i < genericArguments.Length; i++)
                {
                    var typeConstraints = genericArguments[i];

                    EsConstraints constraints = EsConstraints.None;

                    if (typeConstraints.IsNullable()) //? 可空类型。
                    {
                        var underlyingType = Nullable.GetUnderlyingType(typeConstraints);

                        constraints = underlyingType.IsEnum
                            ? EsConstraints.NullableEnum
                            : EsConstraints.Nullable;
                    }
                    else if (typeConstraints.IsEnum)
                    {
                        constraints = EsConstraints.Enum;
                    }
                    else if (typeConstraints.IsValueType)
                    {
                        constraints = EsConstraints.ValueType;
                    }
                    else if (typeConstraints == typeof(object))
                    {
                        typeConstraints = typeArguments[i];
                    }
                    else
                    {
                        constraints = EsConstraints.Inherit;
                    }

                    constraintSlots[i] = new ConstraintsSlot(typeConstraints, constraints);
                }

                mapConstraints = new MapConstraints(type, typeDefinition, constraintSlots);

                return true;
            }

            public void Add(string memberName, Expression valueExpression) => memberExpressions[memberName] = new Slot(valueExpression);

            public void Ignore(string memberName) => memberExpressions[memberName] = new Slot();

            public bool TryGetSlot(string memberName, out Slot slot) => memberExpressions.TryGetValue(memberName, out slot);

            public bool IsMatch(Type sourceType, Type destinationType, bool definitionOnly = true)
            {
                if (this.sourceType == sourceType)
                {
                    return destinationTypes.Contains(destinationType);
                }

                if (hasInumerableInstanceSlot && sourceType.IsGenericType && destinationType.IsGenericType)
                {
                    var sourceTypeDefinition = sourceType.GetGenericTypeDefinition();
                    var destinationTypeDefinition = destinationType.GetGenericTypeDefinition();

                    if (enumerableInstanceFactories.ContainsKey(new TypeCode(sourceTypeDefinition, destinationTypeDefinition)))
                    {
                        return true;
                    }
                }

                if (isDestinationConstraints)
                {
                    if (isSourceConstraints ? sourceConstraints.IsMatch(sourceType) : this.sourceType.IsAssignableFrom(sourceType))
                    {
                        if (destinationConstraints.Exists(x => x.IsMatch(destinationType)))
                        {
                            if (hasMatchConstraints)
                            {
                                return matchConstraints(sourceType, isSourceConstraints ? sourceType.GetGenericArguments() : Type.EmptyTypes, destinationType.GetGenericArguments());
                            }

                            return true;
                        }
                    }
                }

                if (definitionOnly || sourceType == MapConstants.ObjectType)
                {
                    return false;
                }

                return destinationTypes.Contains(destinationType) && this.sourceType.IsAssignableFrom(sourceType);
            }

            public bool TryCreateMap(Type sourceType, Type destinationType, out IInstanceMapSlot mapSlot)
            {
                if (hasInumerableInstanceSlot && sourceType.IsGenericType && destinationType.IsGenericType)
                {
                    var sourceTypeDefinition = sourceType.GetGenericTypeDefinition();
                    var destinationTypeDefinition = destinationType.GetGenericTypeDefinition();

                    if (enumerableInstanceFactories.TryGetValue(new TypeCode(sourceTypeDefinition, destinationTypeDefinition), out InstanceEnumerableFactory instanceFactory))
                    {
                        mapSlot = instanceFactory.CreateMap(sourceType, destinationType);

                        return true;
                    }
                }

                if (hasInstanceSlot)
                {
                    mapSlot = instanceFactory.CreateMap(sourceType, destinationType);

                    return true;
                }

                mapSlot = null;

                return false;
            }

            public void NewEnumerable(Type sourceTypeEnumerable, Type destinationTypeEnumerable, Expression body, ParameterExpression parameter, ParameterExpression parameterOfSet)
            {
                if (!sourceTypeEnumerable.IsGenericType)
                {
                    throw new NotSupportedException($"{sourceTypeEnumerable}不是泛型类型！");
                }

                if (!destinationTypeEnumerable.IsGenericType)
                {
                    throw new NotSupportedException($"{destinationTypeEnumerable}不是泛型类型！");
                }

                var sourceGenericArguments = sourceTypeEnumerable.GetGenericArguments();

                if (sourceGenericArguments.Length > 1)
                {
                    throw new NotSupportedException($"{sourceTypeEnumerable}泛型参数大于1个！");
                }

                if (sourceGenericArguments[0] != sourceType)
                {
                    throw new NotSupportedException($"{sourceTypeEnumerable}泛型参数必须是“{sourceType}”！");
                }

                var destinationGenericArguments = destinationTypeEnumerable.GetGenericArguments();

                if (destinationGenericArguments.Length > 1)
                {
                    throw new NotSupportedException($"{destinationTypeEnumerable}泛型参数大于1个！");
                }

                if (destinationGenericArguments[0] != destinationType)
                {
                    throw new NotSupportedException($"{destinationTypeEnumerable}泛型参数必须是“{destinationType}”！");
                }

                var sourceTypeDefinition = sourceTypeEnumerable.GetGenericTypeDefinition();
                var destinationTypeDefinition = destinationTypeEnumerable.GetGenericTypeDefinition();

                var instanceFactory = new InstanceEnumerableFactory(sourceTypeEnumerable, destinationTypeEnumerable, body, parameter, parameterOfSet);

                var typeCode = new TypeCode(sourceTypeDefinition, destinationTypeDefinition);

                if (enumerableInstanceFactories is null)
                {
                    hasInumerableInstanceSlot = true;

                    enumerableInstanceFactories = new Dictionary<TypeCode, InstanceEnumerableFactory>(TypeCode.InstanceComparer)
                    {
                        [typeCode] = instanceFactory
                    };
                }
                else
                {
                    enumerableInstanceFactories[typeCode] = instanceFactory;
                }
            }

            public void Dispose()
            {
                destinationTypes.Clear();
                memberExpressions.Clear();

                if (enumerableInstanceFactories?.Count > 0)
                {
                    enumerableInstanceFactories.Clear();
                }

                if (destinationConstraints?.Count > 0)
                {
                    destinationConstraints.Clear();
                }

                sourceConstraints = null;
                destinationConstraints = null;
                enumerableInstanceFactories = null;

                GC.SuppressFinalize(this);
            }
        }

        private interface IInstanceFactory
        {
            IInstanceMapSlot CreateMap(Type sourceType, Type destinationType);
        }

        private interface IInstanceMapSlot
        {
            Expression Map(Expression source, IMapConfiguration configuration);
        }

        private class InstanceFactory : IInstanceFactory
        {
            private readonly Type sourceType;
            private readonly Type destinationType;
            private readonly Expression body;
            private readonly ParameterExpression parameter;

            public InstanceFactory(Type sourceType, Type destinationType, Expression body, ParameterExpression parameter)
            {
                this.sourceType = sourceType;
                this.destinationType = destinationType;
                this.body = body;
                this.parameter = parameter;
            }

            private class SimpleSlot : IInstanceMapSlot
            {
                private readonly Expression body;
                private readonly ParameterExpression parameter;

                public SimpleSlot(Expression body, ParameterExpression parameter)
                {
                    this.body = body;
                    this.parameter = parameter;
                }

                public Expression Map(Expression source, IMapConfiguration configuration)
                {
                    var visitor = new ReplaceExpressionVisitor(parameter, source);

                    return visitor.Visit(body);
                }
            }

            private class MapperSlot : IInstanceMapSlot
            {
                private readonly Type destinationType;
                private readonly Expression body;
                private readonly ParameterExpression parameter;

                public MapperSlot(Type destinationType, Expression body, ParameterExpression parameter)
                {
                    this.destinationType = destinationType;
                    this.body = body;
                    this.parameter = parameter;
                }

                public Expression Map(Expression source, IMapConfiguration configuration)
                {
                    var visitor = new MapExpressionVisitor(parameter.Type, body.Type, source.Type, destinationType, configuration, new[] { parameter }, new[] { source });

                    return visitor.Visit(body);
                }
            }

            public IInstanceMapSlot CreateMap(Type sourceType, Type destinationType)
            {
                if (this.sourceType == sourceType && this.destinationType == destinationType)
                {
                    return new SimpleSlot(body, parameter);
                }

                return new MapperSlot(destinationType, body, parameter);
            }
        }

        private class InstanceEnumerableFactory : IInstanceFactory
        {
            private readonly Type sourceTypeEnumerable;
            private readonly Type destinationTypeEnumerable;
            private readonly Expression body;
            private readonly ParameterExpression parameter;
            private readonly ParameterExpression parameterOfSet;

            public InstanceEnumerableFactory(Type sourceTypeEnumerable, Type destinationTypeEnumerable, Expression body, ParameterExpression parameter, ParameterExpression parameterOfSet)
            {
                this.sourceTypeEnumerable = sourceTypeEnumerable;
                this.destinationTypeEnumerable = destinationTypeEnumerable;
                this.body = body;
                this.parameter = parameter;
                this.parameterOfSet = parameterOfSet;
            }

            private class SimpleSlot : IInstanceMapSlot
            {
                private readonly Type destinationSetType;
                private readonly Expression body;
                private readonly ParameterExpression parameter;
                private readonly ParameterExpression parameterOfSet;

                public SimpleSlot(Type destinationSetType, Expression body, ParameterExpression parameter, ParameterExpression parameterOfSet)
                {
                    this.destinationSetType = destinationSetType;
                    this.body = body;
                    this.parameter = parameter;
                    this.parameterOfSet = parameterOfSet;
                }

                public Expression Map(Expression source, IMapConfiguration configuration)
                {
                    var destinationSetVariable = Variable(destinationSetType);

                    var destinationSetExpression = configuration.Map(source, destinationSetType);

                    var visitor = new ReplaceExpressionVisitor(new[] { parameter, parameterOfSet }, new[] { source, destinationSetVariable });

                    return Block(new ParameterExpression[] { destinationSetVariable }, Assign(destinationSetVariable, destinationSetExpression), visitor.Visit(body));
                }
            }

            private class MapperSlot : IInstanceMapSlot
            {
                private readonly Type destinationType;
                private readonly Type destinationSetType;
                private readonly Expression body;
                private readonly ParameterExpression parameter;
                private readonly ParameterExpression parameterOfSet;

                public MapperSlot(Type destinationType, Type destinationSetType, Expression body, ParameterExpression parameter, ParameterExpression parameterOfSet)
                {
                    this.destinationType = destinationType;
                    this.destinationSetType = destinationSetType;
                    this.body = body;
                    this.parameter = parameter;
                    this.parameterOfSet = parameterOfSet;
                }

                public Expression Map(Expression source, IMapConfiguration configuration)
                {
                    var destinationSetVariable = Variable(destinationSetType);

                    var destinationSetExpression = configuration.Map(source, destinationSetType);

                    var visitor = new MapExpressionVisitor(parameter.Type, body.Type, source.Type, destinationType, configuration, new[] { parameter, parameterOfSet }, new[] { source, destinationSetVariable });

                    return Block(new ParameterExpression[] { destinationSetVariable }, Assign(destinationSetVariable, destinationSetExpression), visitor.Visit(body));
                }
            }

            public IInstanceMapSlot CreateMap(Type sourceType, Type destinationType)
            {
                if (sourceTypeEnumerable == sourceType && destinationTypeEnumerable == destinationType)
                {
                    return new SimpleSlot(parameterOfSet.Type, body, parameter, parameterOfSet);
                }

                return new MapperSlot(destinationType, typeof(List<>).MakeGenericType(destinationType.GetGenericArguments()), body, parameter, parameterOfSet);
            }
        }

        private static bool TryAnalysisInstanceExpression(Expression node, out Expression validExpression)
        {
            validExpression = node;

            return node switch
            {
                NewExpression => true,
                MemberInitExpression => true,
                GotoExpression gotoExpression when gotoExpression.Kind == GotoExpressionKind.Return => TryAnalysisInstanceExpression(gotoExpression.Value, out validExpression),
                BlockExpression blockExpression when blockExpression.Variables.Count == 0 && blockExpression.Expressions.Count == 1 => TryAnalysisInstanceExpression(blockExpression.Expressions[0], out validExpression),
                _ => false,
            };
        }

        private class MemberMappingExpression<TSource, TMember> : IMemberMappingExpression<TSource, TMember>
        {
            private readonly string memberName;
            private readonly MapSlot mapSlot;

            public MemberMappingExpression(string memberName, MapSlot mapSlot)
            {
                this.memberName = memberName;
                this.mapSlot = mapSlot;
            }

            public void Constant(TMember member) => mapSlot.Add(memberName, Expression.Constant(member, typeof(TMember)));

            public void ConvertUsing<TSourceMember>(Expression<Func<TSource, TSourceMember>> sourceMember, IValueConverter<TSourceMember, TMember> valueConverter)
            {
                if (sourceMember is null)
                {
                    throw new ArgumentNullException(nameof(sourceMember));
                }

                if (valueConverter is null)
                {
                    throw new ArgumentNullException(nameof(valueConverter));
                }

                Expression<Func<TSourceMember, TMember>> convertExp = x => valueConverter.Convert(x);

                mapSlot.Add(memberName, Lambda(Invoke(convertExp, sourceMember.Body), sourceMember.Parameters));
            }

            public void From(Expression<Func<TSource, TMember>> sourceMember)
            {
                if (sourceMember is null)
                {
                    throw new ArgumentNullException(nameof(sourceMember));
                }

                mapSlot.Add(memberName, sourceMember);
            }

            public void From(IValueResolver<TSource, TMember> valueResolver)
            {
                if (valueResolver is null)
                {
                    throw new ArgumentNullException(nameof(valueResolver));
                }

                From(x => valueResolver.Resolve(x));
            }

            public void Ignore() => mapSlot.Ignore(memberName);
        }

        private class MappingExpression<TSource, TDestination> : IMappingExpression<TSource, TDestination>
        {
            private readonly MapSlot mapSlot;

            public MappingExpression(MapSlot mapSlot)
            {
                this.mapSlot = mapSlot;
            }

            public IIncludeMappingExpression<TSource, TDestination> Include<TAssignableToDestination>() where TAssignableToDestination : TDestination
            {
                mapSlot.Include(typeof(TAssignableToDestination));

                return this;
            }

            private static string NameOfAnalysis(Expression node)
            {
                return node switch
                {
                    MemberExpression member => member.Member.Name,
                    InvocationExpression invocation when invocation.Arguments.Count == 1 => NameOfAnalysis(invocation.Expression),
                    LambdaExpression lambda when lambda.Parameters.Count == 1 => NameOfAnalysis(lambda.Body),
                    _ => throw new NotSupportedException(),
                };
            }

            public IMappingExpressionBase<TSource, TDestination> Map<TMember>(Expression<Func<TDestination, TMember>> destinationMember, Action<IMemberMappingExpression<TSource, TMember>> memberOptions)
            {
                if (destinationMember is null)
                {
                    throw new ArgumentNullException(nameof(destinationMember));
                }

                if (memberOptions is null)
                {
                    throw new ArgumentNullException(nameof(memberOptions));
                }

                string memberName = NameOfAnalysis(destinationMember);

                var options = new MemberMappingExpression<TSource, TMember>(memberName, mapSlot);

                memberOptions.Invoke(options);

                return this;
            }

            public IMappingExpression<TSource, TDestination> NewEnumerable<TSourceEnumerable, TDestinationEnumerable>(Expression<Func<TSourceEnumerable, List<TDestination>, TDestinationEnumerable>> destinationOptions)
                where TSourceEnumerable : IEnumerable<TSource>
                where TDestinationEnumerable : IEnumerable<TDestination>
            {
                if (destinationOptions is null)
                {
                    throw new ArgumentNullException(nameof(destinationOptions));
                }

                if (!TryAnalysisInstanceExpression(destinationOptions.Body, out Expression body))
                {
                    throw new ArgumentException("仅支持形如：(x, y) => new A(y, x.Id, ...) 或 (x, y) => new B(y) { A = 1, B = x.C ... } 的表达式！", nameof(destinationOptions));
                }

                mapSlot.NewEnumerable(typeof(TSourceEnumerable), typeof(TDestinationEnumerable), body, destinationOptions.Parameters[0], destinationOptions.Parameters[1]);

                return this;
            }

            public void IncludeConstraints(MatchConstraints matchConstraints)
            {
                if (matchConstraints is null)
                {
                    throw new ArgumentNullException(nameof(matchConstraints));
                }

                mapSlot.IncludeConstraints(matchConstraints);
            }
        }

        private bool disposedValue;

        private readonly List<MapSlot> mapSlots = new List<MapSlot>();
        private readonly Dictionary<TypeCode, MapSlot> mapCachings = new Dictionary<TypeCode, MapSlot>(TypeCode.InstanceComparer);
        private readonly Dictionary<TypeCode, IInstanceMapSlot> instanceMapCachings = new Dictionary<TypeCode, IInstanceMapSlot>(TypeCode.InstanceComparer);
        private readonly System.Collections.Concurrent.ConcurrentDictionary<TypeCode, bool> matchCachings = new System.Collections.Concurrent.ConcurrentDictionary<TypeCode, bool>(TypeCode.InstanceComparer);

        /// <summary>
        /// 解决映射关系。
        /// </summary>
        /// <param name="sourceExpression">源对象表达式。</param>
        /// <param name="sourceType">源类型。</param>
        /// <param name="destinationExpression">目标对象表达式。</param>
        /// <param name="destinationType">目标类型。</param>
        /// <param name="configuration">映射配置。</param>
        /// <returns>赋值表达式迭代器。</returns>
        /// <exception cref="InvalidCastException">类型不能被转换。</exception>
        protected virtual IEnumerable<BinaryExpression> ToSolveCore(Expression sourceExpression, Type sourceType, ParameterExpression destinationExpression, Type destinationType, IMapConfiguration configuration)
        {
            bool flag = true;

            PropertyInfo[] propertyInfos = null;

            var validSlots = new List<MapSlot>();

            foreach (var propertyInfo in destinationType.GetProperties())
            {
                if (flag) //? 匿名或元组等无可写属性的类型。
                {
                    flag = false;

                    Type currentType = sourceType;

                    do
                    {
                        if (mapCachings.TryGetValue(new TypeCode(currentType, destinationType), out var mapSlot))
                        {
                            if (mapSlot.HasMemberSettings)
                            {
                                validSlots.Add(mapSlot);
                            }
                        }

                        currentType = currentType.BaseType;

                        if (currentType is null)
                        {
                            break;
                        }

                    } while (currentType != MapConstants.ObjectType);
                }

                foreach (var mapSlot in validSlots)
                {
                    if (mapSlot.TryGetSlot(propertyInfo.Name, out Slot slot))
                    {
                        if (slot.Ignore)
                        {
                            goto label_skip;
                        }

                        var destinationPropEx = slot.ValueExpression is LambdaExpression lambda
                            ? new ReplaceExpressionVisitor(lambda.Parameters[0], sourceExpression)
                                .Visit(lambda.Body)
                            : slot.ValueExpression;

                        if (propertyInfo.CanWrite)
                        {
                            yield return Assign(Property(destinationExpression, propertyInfo), destinationPropEx);
                        }
                        else if (TryAdd(propertyInfo.PropertyType, out Type destinationSetType, out MethodInfo methodInfo))
                        {
                            yield return Add(IgnoreIfNull(Property(destinationExpression, propertyInfo)), configuration.Map(destinationPropEx, destinationSetType), methodInfo);
                        }
                        else
                        {
                            throw new InvalidCastException();
                        }

                        goto label_skip;
                    }
                }

                if (propertyInfo.IsIgnore())
                {
                    continue;
                }

                propertyInfos ??= sourceType.GetProperties();

                foreach (var memberInfo in propertyInfos)
                {
                    if (memberInfo.CanRead && string.Equals(memberInfo.Name, propertyInfo.Name, StringComparison.OrdinalIgnoreCase))
                    {
                        if (propertyInfo.CanWrite)
                        {
                            yield return Assign(Property(destinationExpression, propertyInfo), configuration.Map(Property(sourceExpression, memberInfo), propertyInfo.PropertyType));
                        }
                        else if (TryAdd(propertyInfo.PropertyType, out Type destinationSetType, out MethodInfo methodInfo))
                        {
                            yield return Add(IgnoreIfNull(Property(destinationExpression, propertyInfo)), configuration.Map(Property(sourceExpression, memberInfo), destinationSetType), methodInfo);
                        }

                        goto label_skip;
                    }
                }
label_skip:
                continue;
            }
        }

        private static readonly System.Collections.Concurrent.ConcurrentDictionary<Type, Tuple<Type, MethodInfo, bool>> addCachings = new System.Collections.Concurrent.ConcurrentDictionary<Type, Tuple<Type, MethodInfo, bool>>();

        private static Expression IgnoreIfNull(Expression node) => IgnoreIfNullExpressionVisitor.IgnoreIfNull(node, true);

        private static bool TryAdd(Type destinationType, out Type destinationSetType, out MethodInfo methodInfo)
        {
            if (!destinationType.IsGenericType)
            {
                methodInfo = null;
                destinationSetType = null;

                return false;
            }

            var tuple = addCachings.GetOrAdd(destinationType, conversionType =>
            {
                var genericArguments = conversionType.GetGenericArguments();

                if (genericArguments.Length != 1)
                {
                    return new Tuple<Type, MethodInfo, bool>(null, null, false);
                }

                var destinationItemType = genericArguments[0];

                var destinationSetType = typeof(List<>).MakeGenericType(destinationItemType);

                var addRangeMethodInfo = destinationType.GetMethod("AddRange", MapConstants.InstanceBindingFlags, null, new Type[] { typeof(IEnumerable<>).MakeGenericType(destinationItemType) }, null);

                if (addRangeMethodInfo != null)
                {
                    return new Tuple<Type, MethodInfo, bool>(destinationSetType, addRangeMethodInfo, true);
                }

                var addMethodInfo = destinationType.GetMethod("Add", MapConstants.InstanceBindingFlags, null, new Type[] { destinationItemType }, null);

                if (addMethodInfo is null)
                {
                    return new Tuple<Type, MethodInfo, bool>(null, null, false);
                }

                var sourceExpression = Parameter(destinationSetType, "source");

                var destinationExpression = Parameter(destinationType, "destination");

                var bodyExp = ArrayToArray(sourceExpression, destinationExpression, addRangeMethodInfo);

                var lambdaExp = Lambda(bodyExp, sourceExpression, destinationExpression);

                var @delegate = lambdaExp.Compile();

                return new Tuple<Type, MethodInfo, bool>(destinationSetType, @delegate.Method, true);
            });

            destinationSetType = tuple.Item1;
            methodInfo = tuple.Item2;

            return tuple.Item3;
        }

        private static Expression ArrayToArray(ParameterExpression sourceExpression, ParameterExpression destinationExpression, MethodInfo methodInfo)
        {
            var indexExp = Variable(typeof(int));

            var lengthExp = Variable(typeof(int));

            LabelTarget break_label = Label(MapConstants.VoidType);
            LabelTarget continue_label = Label(MapConstants.VoidType);

            return Block(new ParameterExpression[]
              {
                indexExp,
                lengthExp
              }, new Expression[]
              {
                Assign(indexExp, Constant(0)),
                Assign(lengthExp, ArrayLength(sourceExpression)),
                Loop(
                    IfThenElse(
                        GreaterThan(lengthExp, indexExp),
                        Block(
                            Call(destinationExpression, methodInfo, ArrayIndex(sourceExpression, indexExp)),
                            AddAssign(indexExp, Constant(1)),
                            Continue(continue_label)
                        ),
                        Break(break_label)), // push to eax/rax --> return value
                    break_label, continue_label
                )
              });
        }

        /// <summary>
        /// 是否匹配。
        /// </summary>
        /// <param name="sourceType">源。/</param>
        /// <param name="destinationType">目标。</param>
        /// <returns>是否匹配。</returns>
        public override bool IsMatch(Type sourceType, Type destinationType)
        {
            if (sourceType.IsValueType || destinationType.IsValueType)
            {
                return false;
            }

            var typeCode = new TypeCode(sourceType, destinationType);

            if (mapCachings.ContainsKey(typeCode) || instanceMapCachings.ContainsKey(typeCode))
            {
                return true;
            }

            return matchCachings.GetOrAdd(typeCode, tuple =>
            {
                foreach (var mapSlot in mapSlots)
                {
                    if (mapSlot.IsMatch(tuple.X, tuple.Y, false))
                    {
                        if (mapSlot.HasInstanceSlot && mapSlot.TryCreateMap(tuple.X, tuple.Y, out var slot))
                        {
                            instanceMapCachings[typeCode] = slot;
                        }
                        else
                        {
                            mapCachings[typeCode] = mapSlot;
                        }

                        return true;
                    }
                }

                return false;
            });
        }

        /// <summary>
        /// 映射。
        /// </summary>
        /// <param name="sourceExpression">源对象表达式。</param>
        /// <param name="destinationType">目标类型。</param>
        /// <param name="configuration">配置。</param>
        /// <returns>目标类型<paramref name="destinationType"/>的映射结果表达式。</returns>
        public Expression Map(Expression sourceExpression, Type destinationType, IMapConfiguration configuration)
        {
            var sourceType = sourceExpression.Type;

            var typeCode = new TypeCode(sourceType, destinationType);

            if (instanceMapCachings.TryGetValue(typeCode, out IInstanceMapSlot mapSlot))
            {
                var destinationVariable = Variable(destinationType);

                List<ParameterExpression> variables = new List<ParameterExpression>(1)
                {
                    destinationVariable
                };

                List<Expression> expressions = new List<Expression>(3);

                var instanceExpression = mapSlot.Map(sourceExpression, configuration);

                expressions.Add(Assign(destinationVariable, instanceExpression));

                var bodyExp = ToSolve(sourceExpression, sourceType, destinationVariable, destinationType, configuration);

                expressions.Add(bodyExp);

                expressions.Add(destinationVariable);

                return Block(destinationType, variables, expressions);
            }

            return base.ToSolve(sourceExpression, sourceExpression.Type, destinationType, configuration);
        }

        /// <summary>
        /// 解决<paramref name="sourceType"/>到<paramref name="destinationType"/>的映射。
        /// </summary>
        /// <param name="sourceExpression">源对象表达式。</param>
        /// <param name="sourceType">源类型。</param>
        /// <param name="destinationExpression">目标对象表达式。</param>
        /// <param name="destinationType">目标类型。</param>
        /// <param name="configuration">映射配置。</param>
        /// <returns>目标类型<paramref name="destinationType"/>的映射结果表达式。</returns>
        protected sealed override Expression ToSolve(Expression sourceExpression, Type sourceType, ParameterExpression destinationExpression, Type destinationType, IMapConfiguration configuration)
        {
            var expressions = new List<Expression>();

            foreach (var node in ToSolveCore(sourceExpression, sourceType, destinationExpression, destinationType, configuration))
            {
                var visitor = new IgnoreIfNullExpressionVisitor();

                var bodyExp = visitor.Visit(node);

                expressions.Add(visitor.HasIgnore ? IfThen(visitor.Test, bodyExp) : bodyExp);
            }

            if (expressions.Count > 0)
            {
                return Block(expressions);
            }

            return Empty();
        }

        /// <summary>
        /// 映射。
        /// </summary>
        /// <typeparam name="TSource">源。</typeparam>
        /// <typeparam name="TDestination">目标。</typeparam>
        /// <returns>映射关系表达式。</returns>
        public IMappingExpression<TSource, TDestination> Map<TSource, TDestination>()
            where TSource : class
            where TDestination : class
        {
            var sourceType = typeof(TSource);
            var destinationType = typeof(TDestination);

            var mapSlot = new MapSlot(sourceType, destinationType);

            mapSlots.Add(mapSlot);

            //? 优先使用自定义。
            var typeCode = new TypeCode(sourceType, destinationType);

            mapCachings[typeCode] = mapSlot;

            return new MappingExpression<TSource, TDestination>(mapSlot);
        }

        /// <summary>
        /// 新建实例映射。
        /// </summary>
        /// <typeparam name="TSource">源。</typeparam>
        /// <typeparam name="TDestination">目标。</typeparam>
        /// <param name="destinationOptions">创建实例表达式(<see cref="Expression.New(ConstructorInfo, Expression[])"/> 或 <seealso cref="MemberInit(NewExpression, MemberBinding[])"/>)。</param>
        /// <returns>映射关系表达式。</returns>
        public IMappingExpressionBase<TSource, TDestination> New<TSource, TDestination>(Expression<Func<TSource, TDestination>> destinationOptions)
            where TSource : class
            where TDestination : class
        {
            if (destinationOptions is null)
            {
                throw new ArgumentNullException(nameof(destinationOptions));
            }

            if (!TryAnalysisInstanceExpression(destinationOptions.Body, out Expression body))
            {
                throw new ArgumentException("仅支持形如：x => new A(x.Id, ...) 或 x => new B() { A = 1, B = x.C ... } 的表达式！", nameof(destinationOptions));
            }

            var sourceType = typeof(TSource);
            var destinationType = typeof(TDestination);

            var instanceFactory = new InstanceFactory(sourceType, destinationType, body, destinationOptions.Parameters[0]);

            var mapSlot = new MapSlot(sourceType, destinationType, instanceFactory);

            mapSlots.Add(mapSlot);

            //? 优先使用自定义。
            var typeCode = new TypeCode(sourceType, destinationType);

            mapCachings[typeCode] = mapSlot;

            instanceMapCachings[typeCode] = instanceFactory.CreateMap(sourceType, destinationType);

            return new MappingExpression<TSource, TDestination>(mapSlot);
        }

        /// <summary>
        /// 释放资源。
        /// </summary>
        /// <param name="disposing">释放深度释放。</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    foreach (var mapSlot in mapSlots)
                    {
                        mapSlot.Dispose();
                    }

                    mapSlots.Clear();
                }

                instanceMapCachings.Clear();

                matchCachings.Clear();

                disposedValue = true;
            }
        }

        /// <summary>
        /// 释放资源。
        /// </summary>
        public void Dispose()
        {
            // 不要更改此代码。请将清理代码放入“Dispose(bool disposing)”方法中
            Dispose(disposing: true);

            GC.SuppressFinalize(this);
        }
    }
}
