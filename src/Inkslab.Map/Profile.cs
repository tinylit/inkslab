using Inkslab.Map.Maps;
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
    public abstract class Profile : DefaultMap, IProfile, IDisposable
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
                if (valueExpression is null)
                {
                    Auto = true;
                }
                else
                {
                    ValueExpression = valueExpression;
                }
            }

            public bool Ignore { get; }

            public bool Auto { get; }

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
            private readonly IMapApplication application;
            private readonly Expression[] originalParameters;
            private readonly Expression[] parameters;

            public MapExpressionVisitor(Type originalSourceType, Type originalDestinationType, Type sourceType, Type destinationType, IMapApplication application, Expression[] originalParameters, Expression[] parameters)
            {
                this.originalSourceType = originalSourceType;
                this.originalDestinationType = originalDestinationType;
                this.sourceType = sourceType;
                this.destinationType = destinationType;
                this.application = application;
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

                    arguments.AddRange(node.Arguments.Zip(constructorInfo.GetParameters(), (x, y) => application.Map(Visit(x), y.ParameterType)));

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

                    var valueExpression = application.Map(Visit(node.Expression), destinationType);

                    return Bind(memberInfo, valueExpression);
                }

                return node.Update(application.Map(Visit(node.Expression), node.Expression.Type));
            }
        }

        private interface IMapSlot : IDisposable
        {
            bool HasMemberSettings { get; }

            bool TryGetSlot(string memberName, out Slot slot);

            bool IsMatch(Type sourceType, Type destinationType);

            bool IsInstanceSlot { get; }

            IInstanceMapSlot CreateMap(Type sourceType, Type destinationType);
        }

        private class BaseMapSlot : IDisposable
        {
            private readonly Dictionary<string, Slot> memberExpressions = new Dictionary<string, Slot>();
            private bool disposedValue;

            public bool HasMemberSettings => memberExpressions.Count > 0;
            public void Add(string memberName, Expression valueExpression) => memberExpressions[memberName] = new Slot(valueExpression);

            public void Ignore(string memberName) => memberExpressions[memberName] = new Slot();

            public bool TryGetSlot(string memberName, out Slot slot) => memberExpressions.TryGetValue(memberName, out slot);

            protected virtual void Dispose(bool disposing)
            {
                if (!disposedValue)
                {
                    if (disposing)
                    {
                        memberExpressions.Clear();
                    }

                    // TODO: 释放未托管的资源(未托管的对象)并重写终结器
                    // TODO: 将大型字段设置为 null
                    disposedValue = true;
                }
            }

            // // TODO: 仅当“Dispose(bool disposing)”拥有用于释放未托管资源的代码时才替代终结器
            // ~BaseMapSlot()
            // {
            //     // 不要更改此代码。请将清理代码放入“Dispose(bool disposing)”方法中
            //     Dispose(disposing: false);
            // }

            public void Dispose()
            {
                // 不要更改此代码。请将清理代码放入“Dispose(bool disposing)”方法中
                Dispose(disposing: true);
                GC.SuppressFinalize(this);
            }
        }

        private class GenericMapSlot : BaseMapSlot, IMapSlot
        {
            private readonly Type sourceTypeDefinition;
            private readonly Type destinationTypeDefinition;
            private readonly InstanceEnumerableFactory instanceFactory;
            private readonly Func<Type, Type, bool> bindingConstraints;

            public GenericMapSlot(Type sourceTypeDefinition, Type destinationTypeDefinition, InstanceEnumerableFactory instanceFactory, Func<Type, Type, bool> bindingConstraints)
            {
                this.sourceTypeDefinition = sourceTypeDefinition;
                this.destinationTypeDefinition = destinationTypeDefinition;
                this.instanceFactory = instanceFactory;
                this.bindingConstraints = bindingConstraints;
            }

            public bool IsInstanceSlot => true;

            public IInstanceMapSlot CreateMap(Type sourceType, Type destinationType) => instanceFactory.CreateMap(sourceType, destinationType);

            public bool IsMatch(Type sourceType, Type destinationType)
            {
                if (sourceType.IsGenericType && destinationType.IsGenericType)
                {
                    var sourceTypeDefinition = sourceType.GetGenericTypeDefinition();
                    var destinationTypeDefinition = destinationType.GetGenericTypeDefinition();

                    if (sourceTypeDefinition == this.sourceTypeDefinition && destinationTypeDefinition == this.destinationTypeDefinition)
                    {
                        var sourceGenericArguments = sourceType.GetGenericArguments();
                        var destinationGenericArguments = destinationType.GetGenericArguments();

                        return bindingConstraints(sourceGenericArguments[0], destinationGenericArguments[0]);
                    }
                }

                return false;
            }
        }

        private class MapSlot : BaseMapSlot, IMapSlot
        {
            private readonly Type sourceType;
            private readonly Type destinationType;
            private readonly List<IMapSlot> mapSlots;
            private readonly IInstanceFactory instanceFactory;
            private readonly HashSet<Type> destinationTypes;

            private readonly bool hasInstanceSlot = false;

            private bool isSourceConstraints;
            private bool isDestinationConstraints;

            private bool hasMatchConstraints = false;
            private int destinationConstraintsCount = 0;
            private MatchConstraints matchConstraints;
            private MapConstraints sourceConstraints;
            private List<MapConstraints> destinationConstraints;

            public MapSlot(Type sourceType, Type destinationType, List<IMapSlot> mapSlots)
            {
                this.sourceType = sourceType;
                this.destinationType = destinationType;
                this.mapSlots = mapSlots;

                destinationTypes = destinationType == MapConstants.ObjectType
                    ? new HashSet<Type>()
                    : new HashSet<Type> { destinationType };
            }

            public MapSlot(Type sourceType, Type destinationType, IInstanceFactory instanceFactory, List<IMapSlot> mapSlots) : this(sourceType, destinationType, mapSlots)
            {
                hasInstanceSlot = true;

                this.instanceFactory = instanceFactory;
            }

            public bool IsInstanceSlot => hasInstanceSlot;

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

            public bool IsMatch(Type sourceType, Type destinationType)
            {
                if (this.sourceType == sourceType)
                {
                    return destinationTypes.Contains(destinationType);
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

                if (sourceType == MapConstants.ObjectType)
                {
                    return false;
                }

                return destinationTypes.Contains(destinationType) && this.sourceType.IsAssignableFrom(sourceType);
            }

            public IInstanceMapSlot CreateMap(Type sourceType, Type destinationType) => instanceFactory.CreateMap(sourceType, destinationType);

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

                var instanceFactory = new InstanceEnumerableFactory(body, parameter, parameterOfSet);

                Func<Type, Type, bool> bindingConstraints = sourceType == MapConstants.ObjectType && destinationType == MapConstants.ObjectType
                    ? (x, y) => true
                    : IsMatch;

                var mapSlot = new GenericMapSlot(sourceTypeDefinition, destinationTypeDefinition, instanceFactory, bindingConstraints);

                if (body is MemberInitExpression initExpression) //? 忽略已经初始化的属性，避免重复初始化。
                {
                    foreach (var binding in initExpression.Bindings)
                    {
                        mapSlot.Ignore(binding.Member.Name);
                    }
                }

                mapSlots.Add(mapSlot);
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    destinationTypes.Clear();
                    if (destinationConstraints?.Count > 0)
                    {
                        destinationConstraints.Clear();
                    }

                    sourceConstraints = null;
                    destinationConstraints = null;
                }

                base.Dispose(disposing);
            }
        }

        private interface IInstanceFactory
        {
            IInstanceMapSlot CreateMap(Type sourceType, Type destinationType);
        }

        private interface IInstanceMapSlot
        {
            Expression Map(Expression source, IMapApplication application);
        }

        private class InstanceFactory : IInstanceFactory
        {
            private readonly Expression body;
            private readonly ParameterExpression parameter;

            public InstanceFactory(Expression body, ParameterExpression parameter)
            {
                this.body = body;
                this.parameter = parameter;
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

                public Expression Map(Expression source, IMapApplication application)
                {
                    var visitor = new MapExpressionVisitor(parameter.Type, body.Type, source.Type, destinationType, application, new[] { parameter }, new[] { source });

                    return visitor.Visit(body);
                }
            }

            public IInstanceMapSlot CreateMap(Type sourceType, Type destinationType) => new MapperSlot(destinationType, body, parameter);
        }

        private class InstanceEnumerableFactory : IInstanceFactory
        {
            private readonly Expression body;
            private readonly ParameterExpression parameter;
            private readonly ParameterExpression parameterOfSet;

            public InstanceEnumerableFactory(Expression body, ParameterExpression parameter, ParameterExpression parameterOfSet)
            {
                this.body = body;
                this.parameter = parameter;
                this.parameterOfSet = parameterOfSet;
            }

            private class MapperSlot : IInstanceMapSlot
            {
                private readonly Type destinationType;
                private readonly Expression body;
                private readonly ParameterExpression parameter;
                private readonly ParameterExpression parameterOfSet;

                public MapperSlot(Type destinationType, Expression body, ParameterExpression parameter, ParameterExpression parameterOfSet)
                {
                    this.destinationType = destinationType;
                    this.body = body;
                    this.parameter = parameter;
                    this.parameterOfSet = parameterOfSet;
                }

                public Expression Map(Expression source, IMapApplication application)
                {
                    var visitor = new MapExpressionVisitor(parameter.Type, body.Type, source.Type, destinationType, application, new[] { parameter, parameterOfSet }, new[] { source, source });

                    return visitor.Visit(body);
                }
            }

            public IInstanceMapSlot CreateMap(Type sourceType, Type destinationType) => new MapperSlot(destinationType, body, parameter, parameterOfSet);
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

        private class MemberConfigurationExpression<TSource, TMember> : IMemberConfigurationExpression<TSource, TMember>
        {
            private readonly string memberName;
            private readonly MapSlot mapSlot;

            public MemberConfigurationExpression(string memberName, MapSlot mapSlot)
            {
                this.memberName = memberName;
                this.mapSlot = mapSlot;
            }

            public void Auto() => mapSlot.Add(memberName, null);

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

        private class ProfileExpressio<TSource, TDestination> : IProfileExpression<TSource, TDestination>
        {
            private readonly MapSlot mapSlot;

            public ProfileExpressio(MapSlot mapSlot)
            {
                this.mapSlot = mapSlot;
            }

            public IIncludeProfileExpression<TSource, TDestination> Include<TAssignableToDestination>() where TAssignableToDestination : TDestination
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

            public IProfileExpressionBase<TSource, TDestination> Map<TMember>(Expression<Func<TDestination, TMember>> destinationMember, Action<IMemberConfigurationExpression<TSource, TMember>> memberOptions)
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

                var options = new MemberConfigurationExpression<TSource, TMember>(memberName, mapSlot);

                memberOptions.Invoke(options);

                return this;
            }

            public IProfileExpression<TSource, TDestination> NewEnumerable<TSourceEnumerable, TDestinationEnumerable>(Expression<Func<TSourceEnumerable, List<TDestination>, TDestinationEnumerable>> destinationOptions)
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

        private readonly object lockObj = new object();
        private readonly List<IMapSlot> mapSlots = new List<IMapSlot>();
        private readonly HashSet<TypeCode> missCachings = new HashSet<TypeCode>(TypeCode.InstanceComparer);
        private readonly Dictionary<TypeCode, IMapSlot> mapCachings = new Dictionary<TypeCode, IMapSlot>(TypeCode.InstanceComparer);
        private readonly Dictionary<TypeCode, IInstanceMapSlot> instanceMapCachings = new Dictionary<TypeCode, IInstanceMapSlot>(TypeCode.InstanceComparer);

        /// <summary>
        /// 解决映射关系。
        /// </summary>
        /// <param name="sourceExpression">源对象表达式。</param>
        /// <param name="sourceType">源类型。</param>
        /// <param name="destinationExpression">目标对象表达式。</param>
        /// <param name="destinationType">目标类型。</param>
        /// <param name="application">映射配置。</param>
        /// <returns>赋值表达式迭代器。</returns>
        /// <exception cref="InvalidCastException">类型不能被转换。</exception>
        protected override IEnumerable<Expression> ToSolveCore(Expression sourceExpression, Type sourceType, ParameterExpression destinationExpression, Type destinationType, IMapApplication application)
        {
            if (mapCachings.TryGetValue(new TypeCode(sourceType, destinationType), out IMapSlot mapSlot))
            {
                if (mapSlot.IsInstanceSlot || mapSlot.HasMemberSettings)
                {
                    return PrivateToSolveCore(sourceExpression, sourceType, destinationExpression, destinationType, application, mapSlot);
                }
            }

            return base.ToSolveCore(sourceExpression, sourceType, destinationExpression, destinationType, application);
        }

        private IEnumerable<Expression> PrivateToSolveCore(Expression sourceExpression, Type sourceType, ParameterExpression destinationExpression, Type destinationType, IMapApplication application, IMapSlot mapSlot)
        {
            PropertyInfo[] propertyInfos = null;

            foreach (var propertyInfo in destinationType.GetProperties())
            {
                if (mapSlot.HasMemberSettings && mapSlot.TryGetSlot(propertyInfo.Name, out Slot slot))
                {
                    if (slot.Ignore)
                    {
                        continue;
                    }

                    if (slot.Auto)
                    {
                        goto label_auto;
                    }

                    var sourcePrt = slot.ValueExpression is LambdaExpression lambda
                        ? new ReplaceExpressionVisitor(lambda.Parameters[0], sourceExpression)
                            .Visit(lambda.Body)
                        : slot.ValueExpression;

                    var destinationPrt = Property(destinationExpression, propertyInfo);

                    if (TrySolve(propertyInfo, destinationPrt, sourcePrt, application, out var destinationRs))
                    {
                        yield return destinationRs;
                    }
                    else
                    {
                        throw new InvalidCastException($"属性“{destinationType.Name}.{propertyInfo.Name}”不支持映射！");
                    }

                    continue;
                }

                if (!propertyInfo.CanWrite || propertyInfo.IsIgnore())
                {
                    continue;
                }

label_auto:
                propertyInfos ??= sourceType.GetProperties();

                foreach (var memberInfo in propertyInfos)
                {
                    if (memberInfo.CanRead && string.Equals(memberInfo.Name, propertyInfo.Name, StringComparison.OrdinalIgnoreCase))
                    {
                        var sourcePrt = Property(sourceExpression, memberInfo);
                        var destinationPrt = Property(destinationExpression, propertyInfo);

                        if (TrySolve(propertyInfo, destinationPrt, sourcePrt, application, out var destinationRs))
                        {
                            yield return destinationRs;
                        }

                        break;
                    }
                }
            }
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

            if (mapCachings.ContainsKey(typeCode))
            {
                return true;
            }

            if (missCachings.Contains(typeCode))
            {
                return false;
            }

            lock (lockObj)
            {
                if (mapCachings.ContainsKey(typeCode))
                {
                    return true;
                }

                if (missCachings.Contains(typeCode))
                {
                    return false;
                }

                foreach (var mapSlot in mapSlots)
                {
                    if (mapSlot.IsMatch(typeCode.X, typeCode.Y))
                    {
                        if (mapSlot.IsInstanceSlot)
                        {
                            instanceMapCachings[typeCode] = mapSlot.CreateMap(typeCode.X, typeCode.Y);
                        }

                        mapCachings[typeCode] = mapSlot;

                        return true;
                    }
                }

                missCachings.Add(typeCode);

                return false;
            }
        }

        /// <summary>
        /// 映射。
        /// </summary>
        /// <param name="sourceExpression">源对象表达式。</param>
        /// <param name="destinationType">目标类型。</param>
        /// <param name="application">配置应用程序。</param>
        /// <returns>目标类型<paramref name="destinationType"/>的映射结果表达式。</returns>
        public Expression Map(Expression sourceExpression, Type destinationType, IMapApplication application)
        {
            var sourceType = sourceExpression.Type;

            var typeCode = new TypeCode(sourceType, destinationType);

            if (instanceMapCachings.TryGetValue(typeCode, out IInstanceMapSlot mapSlot))
            {
                var destinationVariable = Variable(destinationType, "instance");

                List<ParameterExpression> variables = new List<ParameterExpression>(1)
                {
                    destinationVariable
                };

                List<Expression> expressions = new List<Expression>(3);

                var instanceExpression = mapSlot.Map(sourceExpression, application);

                expressions.Add(Assign(destinationVariable, instanceExpression));

                var bodyExp = ToSolve(sourceExpression, sourceType, destinationVariable, destinationType, application);

                expressions.Add(bodyExp);

                expressions.Add(destinationVariable);

                return Block(destinationType, variables, expressions);
            }

            return ToSolve(sourceExpression, sourceExpression.Type, destinationType, application);
        }

        /// <summary>
        /// 映射。
        /// </summary>
        /// <typeparam name="TSource">源。</typeparam>
        /// <typeparam name="TDestination">目标。</typeparam>
        /// <returns>映射关系表达式。</returns>
        public IProfileExpression<TSource, TDestination> Map<TSource, TDestination>()
            where TSource : class
            where TDestination : class
        {
            var sourceType = typeof(TSource);
            var destinationType = typeof(TDestination);

            //? 对象映射到对象只用作契约，不作为映射标准。
            bool isContract = sourceType == MapConstants.ObjectType && destinationType == MapConstants.ObjectType;

            var mapSlot = new MapSlot(sourceType, destinationType, mapSlots);

            mapSlots.Add(mapSlot);

            if (!isContract)
            {
                //? 优先使用自定义。
                mapCachings[new TypeCode(sourceType, destinationType)] = mapSlot;
            }

            return new ProfileExpressio<TSource, TDestination>(mapSlot);
        }

        /// <summary>
        /// 新建实例映射。
        /// </summary>
        /// <typeparam name="TSource">源。</typeparam>
        /// <typeparam name="TDestination">目标。</typeparam>
        /// <param name="destinationOptions">创建实例表达式(<see cref="Expression.New(ConstructorInfo, Expression[])"/> 或 <seealso cref="MemberInit(NewExpression, MemberBinding[])"/>)。</param>
        /// <returns>映射关系表达式。</returns>
        public IProfileExpressionBase<TSource, TDestination> New<TSource, TDestination>(Expression<Func<TSource, TDestination>> destinationOptions)
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

            var instanceFactory = new InstanceFactory(body, destinationOptions.Parameters[0]);

            var mapSlot = new MapSlot(sourceType, destinationType, instanceFactory, mapSlots);

            if (body is MemberInitExpression initExpression) //? 忽略已经初始化的属性，避免重复初始化。
            {
                foreach (var binding in initExpression.Bindings)
                {
                    mapSlot.Ignore(binding.Member.Name);
                }
            }

            mapSlots.Add(mapSlot);

            //? 优先使用自定义。
            var typeCode = new TypeCode(sourceType, destinationType);

            mapCachings[typeCode] = mapSlot;

            instanceMapCachings[typeCode] = instanceFactory.CreateMap(sourceType, destinationType);

            return new ProfileExpressio<TSource, TDestination>(mapSlot);
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

                    missCachings.Clear();

                    instanceMapCachings.Clear();
                }

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
