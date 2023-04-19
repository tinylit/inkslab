using Inkslab.Map.Visitors;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Security.AccessControl;

namespace Inkslab.Map
{
    using static Expression;

    /// <summary>
    /// 配置。
    /// </summary>
    public abstract class Profile : AbstractMap, IProfile, IDisposable
    {
        private static readonly Type NewInstance_T4_Type = typeof(INewInstance<,,,>);

        private class TypeCode
        {
            private readonly Type x;
            private readonly Type y;

            public TypeCode(Type x, Type y)
            {
                this.x = x;
                this.y = y;
            }

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

        private enum MatchKind
        {
            Definition,
            Assignable
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
                        return typeConstraints.IsLikeTemplate(argumentType);
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
            private readonly ParameterExpression[] originalParameters;
            private readonly ParameterExpression[] parameters;

            public MapExpressionVisitor(Type originalSourceType, Type originalDestinationType, Type sourceType, Type destinationType, ParameterExpression[] originalParameters, ParameterExpression[] parameters)
            {
                this.originalSourceType = originalSourceType;
                this.originalDestinationType = originalDestinationType;
                this.sourceType = sourceType;
                this.destinationType = destinationType;
                this.originalParameters = originalParameters;
                this.parameters = parameters;
            }

            public override Expression Visit(Expression node)
            {
                for (int i = 0; i < originalParameters.Length; i++)
                {
                    if (originalParameters[i] == node)
                    {
                        return parameters[i];
                    }
                }

                return base.Visit(node);
            }

            private bool MemberIsRef(ref MemberInfo memberInfo)
            {
                var declaringType = memberInfo.DeclaringType;

                if (declaringType.IsGenericType)
                {
                    var reflectedType = memberInfo.ReflectedType;

                    if (originalSourceType.IsGenericType && (reflectedType == originalSourceType || declaringType == originalSourceType))
                    {
                        switch (memberInfo.MemberType)
                        {
                            case MemberTypes.Property:
                                memberInfo = sourceType.GetProperty(memberInfo.Name, bindingFlags);
                                return true;
                            case MemberTypes.Field:
                                memberInfo = sourceType.GetField(memberInfo.Name, bindingFlags);
                                return true;
                            case MemberTypes.Method when memberInfo is MethodInfo methodInfo:
                                memberInfo = MethodBase.GetMethodFromHandle(methodInfo.MethodHandle, sourceType.TypeHandle);
                                return true;
                            default:
                                return false;
                        }
                    }
                    else if (originalDestinationType.IsGenericType && (reflectedType == originalDestinationType || declaringType == originalDestinationType))
                    {
                        switch (memberInfo.MemberType)
                        {
                            case MemberTypes.Property:
                                memberInfo = destinationType.GetProperty(memberInfo.Name, bindingFlags);
                                return true;
                            case MemberTypes.Field:
                                memberInfo = destinationType.GetField(memberInfo.Name, bindingFlags);
                                return true;
                            case MemberTypes.Method when memberInfo is MethodInfo methodInfo:
                                memberInfo = MethodBase.GetMethodFromHandle(methodInfo.MethodHandle, destinationType.TypeHandle);
                                return true;
                            default:
                                return false;
                        }
                    }
                }

                return false;
            }

            private static MemberInfo GetMemberInfo(Type destinationType, MemberInfo memberInfo)
            {
                var declaringType = memberInfo.DeclaringType;

                if (declaringType.IsGenericType && destinationType.IsGenericType)
                {
                    return memberInfo.MemberType switch
                    {
                        MemberTypes.Property => destinationType.GetProperty(memberInfo.Name, bindingFlags),
                        MemberTypes.Field => destinationType.GetField(memberInfo.Name, bindingFlags),
                        MemberTypes.Method when memberInfo is MethodInfo methodInfo => MethodBase.GetMethodFromHandle(methodInfo.MethodHandle, destinationType.TypeHandle),
                        _ => memberInfo,
                    };
                }

                return memberInfo;
            }

            protected override Expression VisitNew(NewExpression node)
            {
                if (node.Type == originalDestinationType && destinationType.IsGenericType)
                {
                    var arguments = new List<Expression>(node.Arguments.Count);

                    foreach (var arg in node.Arguments)
                    {
                        arguments.Add(Visit(arg));
                    }

                    var constructorInfo = (ConstructorInfo)MethodBase.GetMethodFromHandle(node.Constructor.MethodHandle, destinationType.TypeHandle);

                    if (node.Members is null)
                    {
                        return Expression.New(constructorInfo, arguments);
                    }

                    var memberInfos = new List<MemberInfo>(node.Members.Count);

                    foreach (var memberInfo in node.Members)
                    {
                        memberInfos.Add(GetMemberInfo(destinationType, memberInfo));
                    }


                    return Expression.New(constructorInfo, arguments, memberInfos);
                }

                return base.VisitNew(node);
            }

            protected override Expression VisitMember(MemberExpression node)
            {
                var memberInfo = node.Member;

                var instanceExpression = Visit(node.Expression);

                if (MemberIsRef(ref memberInfo))
                {
                    return memberInfo switch
                    {
                        PropertyInfo propertyInfo => Property(instanceExpression, propertyInfo),
                        FieldInfo fieldInfo => Field(instanceExpression, fieldInfo),
                        MethodInfo methodInfo => Property(instanceExpression, methodInfo),
                        _ => node,
                    };
                }

                return node.Update(instanceExpression);
            }

            protected override MemberAssignment VisitMemberAssignment(MemberAssignment node)
            {
                var memberInfo = node.Member;

                var valueExpression = Visit(node.Expression);

                if (MemberIsRef(ref memberInfo))
                {
                    return Bind(memberInfo, valueExpression);
                }

                return node.Update(valueExpression);
            }
        }

        private class MapSlot
        {
            private readonly Type sourceType;
            private readonly List<Type> destinationTypes;
            private readonly Dictionary<string, Slot> memberExpressions = new Dictionary<string, Slot>();


            private bool isSourceConstraints;
            private bool isDestinationConstraints;

            private bool hasMatchConstraints = false;
            private int destinationConstraintsCount = 0;
            private MatchConstraints matchConstraints;
            private MapConstraints sourceConstraints;
            private List<MapConstraints> destinationConstraints;

            public MapSlot(Type sourceType, Type destinationType)
            {
                this.sourceType = sourceType;

                KeyCode = new TypeCode(sourceType, destinationType);

                destinationTypes = new List<Type>(1) { destinationType };
            }

            public TypeCode KeyCode { get; }

            public bool HasMemberSetting => memberExpressions.Count > 0;

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
        }

        private class InstanceSlot
        {
            private readonly Type sourceType;
            private readonly Type destinationType;
            private readonly Expression body;
            private readonly ParameterExpression parameter;

            public InstanceSlot(Type sourceType, Type destinationType, Expression body, ParameterExpression parameter)
            {
                this.sourceType = sourceType;
                this.destinationType = destinationType;
                this.body = body;
                this.parameter = parameter;
            }

            public Expression NewInstance(Expression parameter)
            {
                var visitor = new ReplaceExpressionVisitor(this.parameter, parameter);

                return visitor.Visit(body);
            }

            public InstanceSlot CreateInstance(Type sourceType, Type destinationType)
            {
                if (this.sourceType == sourceType && this.destinationType == destinationType)
                {
                    return this;
                }

                try
                {
                    var parameter = Parameter(sourceType);

                    var visitor = new MapExpressionVisitor(this.sourceType, this.destinationType, sourceType, destinationType, new[] { this.parameter }, new[] { parameter });

                    var body = visitor.Visit(this.body);

                    return new InstanceSlot(sourceType, destinationType, body, parameter);
                }
                catch (Exception ex)
                {
                    throw new NotSupportedException($"无法从源({this.sourceType}=>{this.destinationType})的表达式，分析出目标({sourceType}=>{destinationType})的表达式！", ex);
                }
            }
        }

        private abstract class CreateInstanceSlot
        {
            public abstract bool IsMatch(Type sourceType, Type sourceItemType, Type destinationType, Type destinationItemType);

            public abstract Expression NewInstance(Expression sourceExpression, Type sourceType, Type sourceItemType, Type destinationType, Type destinationItemType, IMapConfiguration configuration);

            protected Expression NewInstanceCore(Type createInstanceType, Expression sourceExpression, Type sourceType, Type sourceItemType, Type destinationType, Type destinationItemType, IMapConfiguration configuration)
            {
                var constructorInfo = createInstanceType.GetConstructor(MapConstants.InstanceBindingFlags, null, Type.EmptyTypes, null) ?? throw new NotSupportedException($"实例类【{createInstanceType}】不具备无参构造函数！");

                var destinationItemsType = typeof(List<>).MakeGenericType(destinationItemType);

                var createInstanceMtd = createInstanceType.GetMethod("CreateInstance", MapConstants.InstanceBindingFlags, null, new Type[] { sourceType, destinationItemsType }, null);

                if (createInstanceMtd is null || createInstanceMtd.ReturnType != destinationType)
                {
                    createInstanceMtd = NewInstance_T4_Type.MakeGenericType(sourceType, sourceItemType, destinationType, destinationItemType)
                        .GetMethod("CreateInstance", MapConstants.InstanceBindingFlags, null, new Type[] { sourceType, destinationItemsType }, null);
                }

                var bodyExp = configuration.Map(sourceExpression, destinationItemsType);

                return Call(Expression.New(constructorInfo), createInstanceMtd, sourceExpression, bodyExp);
            }
        }

        private class CreateInstanceOfNothingSlot : CreateInstanceSlot
        {
            private readonly Type newInstanceType;
            private readonly Type sourceContractType;
            private readonly Type destinationContractType;

            public CreateInstanceOfNothingSlot(Type newInstanceType, Type sourceContractType, Type destinationContractType)
            {
                this.newInstanceType = newInstanceType;
                this.sourceContractType = sourceContractType;
                this.destinationContractType = destinationContractType;
            }

            public override bool IsMatch(Type sourceType, Type sourceItemType, Type destinationType, Type destinationItemType)
            {
                return destinationType == destinationContractType && sourceType == sourceContractType;
            }

            public override Expression NewInstance(Expression sourceExpression, Type sourceType, Type sourceItemType, Type destinationType, Type destinationItemType, IMapConfiguration configuration)
            {
                return NewInstanceCore(newInstanceType,
                        sourceExpression,
                        sourceType,
                        sourceItemType,
                        destinationType,
                        destinationItemType,
                        configuration);
            }
        }

        private class CreateInstanceOfSingleSlot : CreateInstanceSlot
        {
            private readonly Type newInstanceType;
            private readonly Type itemContractType;
            private readonly Type sourceContractType;
            private readonly Type destinationContractType;

            public CreateInstanceOfSingleSlot(Type newInstanceType, Type sourceContractType, Type destinationContractType, Type itemContractType)
            {
                this.newInstanceType = newInstanceType;
                this.sourceContractType = sourceContractType;
                this.itemContractType = itemContractType;
                this.destinationContractType = destinationContractType;
            }

            public override bool IsMatch(Type sourceType, Type sourceItemType, Type destinationType, Type destinationItemType)
            {
                return sourceItemType == destinationItemType
                    && itemContractType.IsLikeTemplate(sourceItemType)
                    && destinationContractType.IsLikeTemplate(destinationType)
                    && sourceContractType.IsLikeTemplate(sourceType);
            }

            public override Expression NewInstance(Expression sourceExpression, Type sourceType, Type sourceItemType, Type destinationType, Type destinationItemType, IMapConfiguration configuration)
            {
                return NewInstanceCore(newInstanceType.MakeGenericType(sourceItemType),
                        sourceExpression,
                        sourceType,
                        sourceItemType,
                        destinationType,
                        destinationItemType,
                        configuration);
            }
        }

        private class CreateInstanceOfDoubleSlot : CreateInstanceSlot
        {
            private readonly Type newInstanceType;
            private readonly Type sourceContractType;
            private readonly Type sourceItemContractType;
            private readonly Type destinationItemContractType;
            private readonly Type destinationContractType;

            public CreateInstanceOfDoubleSlot(Type newInstanceType, Type sourceContractType, Type sourceItemContractType, Type destinationContractType, Type destinationItemContractType)
            {
                this.newInstanceType = newInstanceType;
                this.sourceContractType = sourceContractType;
                this.sourceItemContractType = sourceItemContractType;
                this.destinationContractType = destinationContractType;
                this.destinationItemContractType = destinationItemContractType;
            }

            public override bool IsMatch(Type sourceType, Type sourceItemType, Type destinationType, Type destinationItemType)
            {
                return destinationItemContractType.IsLikeTemplate(destinationItemType)
                    && sourceItemContractType.IsLikeTemplate(sourceItemType)
                    && destinationContractType.IsLikeTemplate(destinationType)
                    && sourceContractType.IsLikeTemplate(sourceType);
            }

            public override Expression NewInstance(Expression sourceExpression, Type sourceType, Type sourceItemType, Type destinationType, Type destinationItemType, IMapConfiguration configuration)
            {
                return NewInstanceCore(newInstanceType.MakeGenericType(sourceItemType, destinationItemType),
                        sourceExpression,
                        sourceType,
                        sourceItemType,
                        destinationType,
                        destinationItemType,
                        configuration);
            }
        }

        private class CreateInstanceOfFullSlot : CreateInstanceSlot
        {
            private readonly Type newInstanceType;
            private readonly Type sourceContractType;
            private readonly Type sourceItemContractType;
            private readonly Type destinationItemContractType;
            private readonly Type destinationContractType;

            public CreateInstanceOfFullSlot(Type newInstanceType, Type sourceContractType, Type sourceItemContractType, Type destinationContractType, Type destinationItemContractType)
            {
                this.newInstanceType = newInstanceType;
                this.sourceContractType = sourceContractType;
                this.sourceItemContractType = sourceItemContractType;
                this.destinationContractType = destinationContractType;
                this.destinationItemContractType = destinationItemContractType;
            }

            public override bool IsMatch(Type sourceType, Type sourceItemType, Type destinationType, Type destinationItemType)
            {
                return destinationItemContractType.IsLikeTemplate(destinationItemType)
                    && destinationContractType.IsLikeTemplate(destinationType)
                    && sourceItemContractType.IsLikeTemplate(sourceItemType)
                    && sourceContractType.IsLikeTemplate(sourceType);
            }

            public override Expression NewInstance(Expression sourceExpression, Type sourceType, Type sourceItemType, Type destinationType, Type destinationItemType, IMapConfiguration configuration)
            {
                return NewInstanceCore(newInstanceType.MakeGenericType(sourceType, sourceItemType, destinationType, destinationItemType),
                        sourceExpression,
                        sourceType,
                        sourceItemType,
                        destinationType,
                        destinationItemType,
                        configuration);
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

            public IMappingExpression<TSource, TDestination> NewEnumerable<TSourceEnumerable, TDestinationEnumerable>(Expression<Func<TSource, List<TDestination>, TDestinationEnumerable>> destinationOptions)
                where TSourceEnumerable : IEnumerable<TSource>
                where TDestinationEnumerable : IEnumerable<TDestination>
            {
                throw new NotImplementedException();
            }

            public IMappingExpressionBase<TSource, TDestination> IncludeConstraints(MatchConstraints matchConstraints)
            {
                if (matchConstraints is null)
                {
                    throw new ArgumentNullException(nameof(matchConstraints));
                }

                mapSlot.IncludeConstraints(matchConstraints);

                return this;
            }
        }

        private bool disposedValue;

        private readonly List<MapSlot> mapSlots = new List<MapSlot>();
        private readonly List<CreateInstanceSlot> instanceSlots = new List<CreateInstanceSlot>();
        private readonly Dictionary<TypeCode, InstanceSlot> instanceCachings = new Dictionary<TypeCode, InstanceSlot>(TypeCode.InstanceComparer);

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
                if (!propertyInfo.CanWrite)
                {
                    continue;
                }

                if (flag) //? 匿名或元组等无可写属性的类型。
                {
                    flag = false;

                    Type currentType = sourceType;

                    do
                    {
                        for (int i = mapSlots.Count - 1; i >= 0; i--)
                        {
                            var mapSlot = mapSlots[i];

                            if (mapSlot.HasMemberSetting && mapSlot.IsMatch(currentType, destinationType))
                            {
                                validSlots.Add(mapSlot);
                            }
                        }

                        if (currentType.BaseType is null)
                        {
                            break;
                        }

                        currentType = currentType.BaseType;

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

                        if (slot.ValueExpression is LambdaExpression lambda)
                        {
                            var visitor = new ReplaceExpressionVisitor(lambda.Parameters[0], sourceExpression);

                            yield return Assign(Property(destinationExpression, propertyInfo), visitor.Visit(lambda.Body));
                        }
                        else
                        {
                            yield return Assign(Property(destinationExpression, propertyInfo), slot.ValueExpression);
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
                        if (configuration.IsMatch(memberInfo.PropertyType, propertyInfo.PropertyType))
                        {
                            yield return Assign(Property(destinationExpression, propertyInfo), configuration.Map(Property(sourceExpression, memberInfo), propertyInfo.PropertyType));
                        }
                        else
                        {
                            throw new InvalidCastException($"成员【{memberInfo.Name}】({memberInfo.PropertyType})无法转换为【{propertyInfo.Name}】({propertyInfo.PropertyType})类型!");
                        }

                        goto label_skip;
                    }
                }
label_skip:
                continue;
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

            if (instanceCachings.ContainsKey(typeCode))
            {
                return true;
            }

            foreach (var mapSlot in mapSlots)
            {
                if (mapSlot.IsMatch(sourceType, destinationType, false))
                {
                    if (destinationType.IsGenericType
                        && instanceCachings.TryGetValue(mapSlot.KeyCode, out InstanceSlot instanceSlot))
                    {
                        instanceCachings[typeCode] = instanceSlot.CreateInstance(sourceType, destinationType);
                    }

                    return true;
                }
            }

            return false;
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

            if (instanceCachings.TryGetValue(new TypeCode(sourceType, destinationType), out InstanceSlot instanceSlot))
            {
                var instanceExpression = instanceSlot.NewInstance(sourceExpression);

                var destinationExpression = Variable(destinationType);

                var bodyExp = ToSolve(sourceExpression, sourceType, destinationExpression, destinationType, configuration);

                return Block(destinationType, new ParameterExpression[] { destinationExpression }, new Expression[] { Assign(destinationExpression, instanceExpression), bodyExp, destinationExpression });
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
        protected override Expression ToSolve(Expression sourceExpression, Type sourceType, ParameterExpression destinationExpression, Type destinationType, IMapConfiguration configuration)
            => Block(ToSolveCore(sourceExpression, sourceType, destinationExpression, destinationType, configuration));

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
            var mapSlot = new MapSlot(typeof(TSource), typeof(TDestination));

            mapSlots.Add(mapSlot);

            return new MappingExpression<TSource, TDestination>(mapSlot);
        }

        /// <summary>
        /// 新建实例映射。
        /// </summary>
        /// <typeparam name="TSource">源。</typeparam>
        /// <typeparam name="TDestination">目标。</typeparam>
        /// <param name="newInstanceExpression">创建实例表达式(<see cref="Expression.New(ConstructorInfo, Expression[])"/> 或 <seealso cref="MemberInit(NewExpression, MemberBinding[])"/>)。</param>
        /// <returns>映射关系表达式。</returns>
        public IIncludeConstraintsMappingExpression<TSource, TDestination> New<TSource, TDestination>(Expression<Func<TSource, TDestination>> newInstanceExpression)
            where TSource : class
            where TDestination : class
        {
            if (newInstanceExpression is null)
            {
                throw new ArgumentNullException(nameof(newInstanceExpression));
            }

            if (!TryAnalysisInstanceExpression(newInstanceExpression.Body, out Expression body))
            {
                throw new ArgumentException("仅支持形如：x => new A(x.Id, ...) 或 x => new B() { A = 1, B = x.C ... } 的表达式！", nameof(newInstanceExpression));
            }

            var sourceType = typeof(TSource);
            var destinationType = typeof(TDestination);

            var mapSlot = new MapSlot(sourceType, destinationType);

            mapSlots.Add(mapSlot);

            instanceCachings[new TypeCode(sourceType, destinationType)] = new InstanceSlot(sourceType, destinationType, body, newInstanceExpression.Parameters[0]);

            return new MappingExpression<TSource, TDestination>(mapSlot);
        }

        /// <summary>
        /// 实例化，支持定义类型(<see cref="Type.IsGenericTypeDefinition"/>)。
        /// </summary>
        /// <param name="newInstanceType">创建实例类型，必须实现 <see cref="INewInstance{TSource, TSourceItem, TDestination, TDestinationItem}"/> 接口。</param>
        public void New(Type newInstanceType)
        {
            if (newInstanceType is null)
            {
                throw new ArgumentNullException(nameof(newInstanceType));
            }

            if (newInstanceType.IsGenericTypeDefinition)
            {
                var genericArguments = newInstanceType.GetGenericArguments();

                switch (genericArguments.Length)
                {
                    case 1:
                        foreach (var interfaceType in newInstanceType.GetInterfaces())
                        {
                            if (interfaceType.IsGenericType && NewInstance_T4_Type == interfaceType.GetGenericTypeDefinition())
                            {
                                var typeArguments = interfaceType.GetGenericArguments();

                                //? TSourceItem 和 TDestinationItem 相同。
                                if (genericArguments[0] == typeArguments[1] && typeArguments[1] == typeArguments[3])
                                {
                                    instanceSlots.Add(new CreateInstanceOfSingleSlot(newInstanceType, typeArguments[0], typeArguments[2], genericArguments[0]));
                                }
                                else
                                {
                                    throw new NotSupportedException($"一个泛型参数的类型，代表元素类型相同的两个集合类型转换。如：{newInstanceType.Name}<{genericArguments[0].Name}> : INewInstance<List<{genericArguments[0].Name}>,{genericArguments[0].Name},HashSet<{genericArguments[0].Name}>,{genericArguments[0].Name}>");
                                }
                            }
                        }
                        break;
                    case 2:
                        foreach (var interfaceType in newInstanceType.GetInterfaces())
                        {
                            if (interfaceType.IsGenericType && NewInstance_T4_Type == interfaceType.GetGenericTypeDefinition())
                            {
                                var typeArguments = interfaceType.GetGenericArguments();

                                //? TSourceItem 和 TDestinationItem。
                                if (genericArguments[0] == typeArguments[1] && genericArguments[1] == typeArguments[3])
                                {
                                    instanceSlots.Add(new CreateInstanceOfDoubleSlot(newInstanceType, typeArguments[0], typeArguments[1], typeArguments[2], typeArguments[3]));
                                }
                                else
                                {
                                    throw new NotSupportedException($"两个泛型参数的类型，代表两个满足约束的任意元素的集合类型之间的转换。如：{newInstanceType.Name}<{genericArguments[0].Name},{genericArguments[1].Name}> : INewInstance<List<{genericArguments[0].Name}>,{genericArguments[0].Name},HashSet<{genericArguments[1].Name}>,{genericArguments[1].Name}>");
                                }
                            }
                        }
                        break;
                    case 4:
                        foreach (var interfaceType in newInstanceType.GetInterfaces())
                        {
                            if (interfaceType.IsGenericType && NewInstance_T4_Type == interfaceType.GetGenericTypeDefinition())
                            {
                                var typeArguments = interfaceType.GetGenericArguments();

                                if (genericArguments[0] == typeArguments[0] && genericArguments[1] == typeArguments[1] && genericArguments[2] == typeArguments[2] && genericArguments[3] == typeArguments[3])
                                {
                                    instanceSlots.Add(new CreateInstanceOfFullSlot(newInstanceType, typeArguments[0], typeArguments[1], typeArguments[2], typeArguments[3]));
                                }
                                else
                                {
                                    var template = string.Join(",", genericArguments.Select(x => x.Name));

                                    throw new NotSupportedException($"四个泛型参数的类型，代表满足约束的任意元素类型、任意集合之间的转换。如：{newInstanceType.Name}<{template}> : INewInstance<List<{template}>");
                                }
                            }
                        }
                        break;
                    default:
                        throw new NotSupportedException($"泛型定义【{newInstanceType}】的泛型个数需为1、2、4！");
                }
            }
            else
            {
                bool errorFlag = true;

                foreach (var interfaceType in newInstanceType.GetInterfaces())
                {
                    if (interfaceType.IsGenericType && NewInstance_T4_Type == interfaceType.GetGenericTypeDefinition())
                    {
                        errorFlag = false;

                        var genericArguments = interfaceType.GetGenericArguments();

                        instanceSlots.Add(new CreateInstanceOfNothingSlot(newInstanceType, genericArguments[0], genericArguments[2]));
                    }
                }

                if (errorFlag)
                {
                    throw new NotSupportedException($"泛型定义【{newInstanceType}】未实现【{NewInstance_T4_Type}】接口！");
                }
            }
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

                }

                mapSlots.Clear();

                instanceCachings.Clear();

                instanceSlots.Clear();

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
