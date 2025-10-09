using Inkslab.Map.Maps;
using Inkslab.Map.Visitors;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;

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
            public TypeCode(Type x, Type y)
            {
                X = x;
                Y = y;
            }

            public Type X { get; }
            public Type Y { get; }

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

                    return x.X == y.X && x.Y == y.Y;
                }

                public int GetHashCode(TypeCode obj)
                {
                    var h1 = obj.X.GetHashCode();
                    var h2 = obj.Y.GetHashCode();

                    return ((h1 << 5) + h1) ^ h2;
                }
            }

            public static readonly IEqualityComparer<TypeCode> InstanceComparer = new TypeCodeEqualityComparer();
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
            private readonly Type _typeConstraints;
            private readonly EsConstraints _constraints;

            public ConstraintsSlot(Type typeConstraints, EsConstraints constraints)
            {
                _typeConstraints = typeConstraints;
                _constraints = constraints;
            }

            public bool IsMatch(Type argumentType)
            {
                switch (_constraints)
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
                            var underlyingType = Nullable.GetUnderlyingType(argumentType)!;

                            return underlyingType.IsEnum;
                        }

                        return false;
                    case EsConstraints.Inherit:
                        return _typeConstraints.IsAssignableFrom(argumentType);
                    default:
                        return false;
                }
            }
        }

        private class MapConstraints
        {
            private readonly Type _typeDefinition;
            private readonly ConstraintsSlot[] _constraintSlots;

            public MapConstraints(Type typeDefinition, ConstraintsSlot[] constraintSlots)
            {
                _typeDefinition = typeDefinition;
                _constraintSlots = constraintSlots;
            }

            public bool IsMatch(Type destinationType)
            {
                if (!destinationType.IsGenericType)
                {
                    return false;
                }

                var typeDefinition = destinationType.GetGenericTypeDefinition();

                if (typeDefinition != _typeDefinition)
                {
                    return false;
                }

                var typeArguments = destinationType.GetGenericArguments();

                for (int i = 0; i < typeArguments.Length; i++)
                {
                    if (_constraintSlots[i].IsMatch(typeArguments[i]))
                    {
                        continue;
                    }

                    return false;
                }

                return true;
            }
        }

        private const BindingFlags BindingFlags = System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;

        private const ExpressionType NodeTypeMap = (ExpressionType)(-1024);
        private const ExpressionType NodeTypeConvertIf = (ExpressionType)(-1025);

        private class ConvertIfExpression : Expression
        {
            public ConvertIfExpression(Type destinationType)
            {
                Type = destinationType;
            }

            public override Type Type { get; }

            public override ExpressionType NodeType => NodeTypeConvertIf;

            public override bool CanReduce => false;

            public override Expression Reduce() => null!;
        }

        private class MapExpression : Expression
        {
            private readonly Expression _node;

            public MapExpression(Expression node, Type destinationType)
            {
                _node = node;
                Type = destinationType;
            }

            public override ExpressionType NodeType => NodeTypeMap;

            public override Type Type { get; }

            public override bool CanReduce => true;

            public override Expression Reduce() => _node;
        }

        private static Expression PrivateMap(Expression node, Type destinationType)
        {
            if (destinationType == node.Type && node.Type.IsSimple())
            {
                return node;
            }

            if (!node.Type.IsValueType && !destinationType.IsValueType && destinationType.IsAssignableFrom(node.Type))
            {
                if (IsSecurityNode(node))
                {
                    return node;
                }
            }

            if (node.NodeType == NodeTypeMap)
            {
                if (node.Type == destinationType)
                {
                    return node;
                }

                return new MapExpression(node.Reduce(), destinationType);
            }

            return new MapExpression(node, destinationType);
        }

        private static bool IsSecurityNode(Expression node)
        {
            if (node.NodeType == NodeTypeConvertIf)
            {
                return true;
            }

            while (node is MethodCallExpression methodCall)
            {
                bool isStatic = methodCall.Method.IsStatic;

                node = isStatic ? methodCall.Arguments[0] : methodCall.Object;

                if (node!.NodeType == NodeTypeConvertIf)
                {
                    throw new InvalidCastException("表达式“NewEnumerable<TSourceEnumerable, TDestinationEnumerable>((x, y) => {TDestinationEnumerable})”中，参数“y”不是使用方法二次处理！");
                }
            }

            if (node is MemberExpression member)
            {
                return IsSecurityNode(member.Expression);
            }

            return false;
        }

        private class PrepareMapExpressionVisitor : ExpressionVisitor
        {
            private readonly Type _originalSourceType;
            private readonly Type _originalDestinationType;
            private readonly Type _sourceType;
            private readonly Type _destinationType;
            private readonly Expression[] _originalParameters;
            private readonly Expression[] _parameters;

            public PrepareMapExpressionVisitor(Type originalSourceType, Type originalDestinationType, Type sourceType, Type destinationType, Expression[] originalParameters, Expression[] parameters)
            {
                _originalSourceType = originalSourceType;
                _originalDestinationType = originalDestinationType;
                _sourceType = sourceType;
                _destinationType = destinationType;
                _originalParameters = originalParameters;
                _parameters = parameters;
            }

            public override Expression Visit(Expression node)
            {
                var indexOf = Array.IndexOf(_originalParameters, node);

                if (indexOf > -1)
                {
                    return _parameters[indexOf];
                }

                return base.Visit(node);
            }

            private bool MemberIsRef(ref MemberInfo memberInfo)
            {
                var declaringType = memberInfo.DeclaringType;

                if (declaringType!.IsGenericType)
                {
                    if (_originalDestinationType.IsGenericType && (declaringType == _originalDestinationType || memberInfo.ReflectedType == _originalDestinationType))
                    {
                        memberInfo = memberInfo.MemberType switch
                        {
                            MemberTypes.Property => _destinationType.GetProperty(memberInfo.Name, BindingFlags),
                            MemberTypes.Field => _destinationType.GetField(memberInfo.Name, BindingFlags),
                            MemberTypes.Event => _destinationType.GetEvent(memberInfo.Name, BindingFlags),
                            MemberTypes.Method when memberInfo is MethodInfo methodInfo => MethodBase.GetMethodFromHandle(methodInfo.MethodHandle, _destinationType.TypeHandle),
                            _ => Member(memberInfo, _destinationType),
                        };

                        return true;
                    }
                }

                return false;

                static MemberInfo Member(MemberInfo memberInfo, Type destinationType)
                {
                    var memberInfos = destinationType.GetMember(memberInfo.Name, memberInfo.MemberType, BindingFlags);

                    if (memberInfos.Length == 1)
                    {
                        return memberInfos[0];
                    }

                    var declaringType = memberInfo.DeclaringType;

                    var typeDefinition = declaringType!.GetGenericTypeDefinition();

                    return memberInfos.Single(x => x.DeclaringType!.IsGenericType && x.DeclaringType.GetGenericTypeDefinition() == typeDefinition);
                }
            }

            protected override Expression VisitNew(NewExpression node)
            {
                if (node.Type == _originalDestinationType && _destinationType.IsGenericType)
                {
                    var constructorInfo = (ConstructorInfo)MethodBase.GetMethodFromHandle(node.Constructor.MethodHandle, _destinationType.TypeHandle)!;

                    var arguments = new List<Expression>(node.Arguments.Count);

                    arguments.AddRange(node.Arguments.Zip(constructorInfo.GetParameters(), (x, y) => PrivateMap(Visit(x), y.ParameterType)));

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

                var declaringType = memberInfo.DeclaringType!;

                var instanceExpression = Visit(node.Expression)!;

                if (declaringType.IsGenericType && _originalSourceType.IsGenericType && (declaringType == _originalSourceType || memberInfo.ReflectedType == _originalSourceType))
                {
                    return memberInfo.MemberType switch
                    {
                        MemberTypes.Property => Property(instanceExpression, _sourceType.GetProperty(memberInfo.Name, BindingFlags)!),
                        MemberTypes.Field => Field(instanceExpression, _sourceType.GetField(memberInfo.Name, BindingFlags)!),
                        MemberTypes.Method when memberInfo is MethodInfo method => Property(instanceExpression, (MethodInfo)MethodBase.GetMethodFromHandle(method.MethodHandle, _sourceType.TypeHandle)!),
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

                    var valueExpression = PrivateMap(Visit(node.Expression), destinationType);

                    return Bind(memberInfo, valueExpression);
                }

                return node.Update(PrivateMap(Visit(node.Expression), node.Expression.Type));
            }
        }

        private class MapExpressionVisitor : ExpressionVisitor
        {
            private readonly IMapApplication _application;
            private readonly Expression[] _originalParameters;
            private readonly Expression[] _parameters;

            public MapExpressionVisitor(IMapApplication application, Expression[] originalParameters, Expression[] parameters)
            {
                _application = application;
                _originalParameters = originalParameters;
                _parameters = parameters;
            }

            public override Expression Visit(Expression node)
            {
                var indexOf = Array.IndexOf(_originalParameters, node);

                if (indexOf > -1)
                {
                    return _parameters[indexOf];
                }

                if (node?.NodeType == NodeTypeMap)
                {
                    return _application.Map(Visit(node.Reduce()), node.Type);
                }

                return base.Visit(node);
            }

            protected override Expression VisitMethodCall(MethodCallExpression node)
            {
                if (node.Method.IsGenericMethod)
                {
                    // 访问方法调用的实例表达式（如果有）
                    var visitedObject = Visit(node.Object);

                    // 访问所有参数表达式
                    var visitedArguments = node.Arguments.Select(Visit).ToArray();

                    // 缓存反射信息
                    var genericMethodDefinition = node.Method.GetGenericMethodDefinition();
                    var originalTypeArguments = node.Method.GetGenericArguments();

                    // 全面推断泛型类型参数，寻找最优类型
                    var newTypeArguments = InferOptimalTypeArguments(
                        originalTypeArguments,
                        visitedObject,
                        visitedArguments,
                        node.Method,
                        genericMethodDefinition);

                    // 重新构造泛型方法
                    var newMethod = genericMethodDefinition.MakeGenericMethod(newTypeArguments);

                    // 返回更新后的方法调用表达式
                    return Call(visitedObject, newMethod, visitedArguments);
                }

                return base.VisitMethodCall(node);
            }

            /// <summary>
            /// 全面推断最优泛型类型参数
            /// </summary>
            private static Type[] InferOptimalTypeArguments(
                Type[] originalTypeArguments,
                Expression visitedObject,
                Expression[] visitedArguments,
                MethodInfo originalMethod,
                MethodInfo genericMethodDefinition)
            {
                var newTypeArguments = originalTypeArguments; // 默认使用原始类型
                bool hasChanges = false;

                // 缓存方法信息
                var isStatic = originalMethod.IsStatic;
                var isExtensionMethod = isStatic && IsExtensionMethodCached(originalMethod);
                var parameters = genericMethodDefinition.GetParameters();
                var genericParameters = genericMethodDefinition.GetGenericArguments();

                for (int i = 0; i < originalTypeArguments.Length; i++)
                {
                    // 对所有泛型参数进行最优类型推断
                    var inferredType = InferOptimalSingleType(
                        originalTypeArguments[i],
                        genericParameters[i],
                        visitedObject,
                        visitedArguments,
                        parameters,
                        isStatic,
                        isExtensionMethod);

                    // 如果推断出了更优的类型，则使用它
                    if (inferredType != null && ShouldUseInferredType(originalTypeArguments[i], inferredType))
                    {
                        if (!hasChanges)
                        {
                            // 延迟复制数组，仅在需要时创建
                            newTypeArguments = new Type[originalTypeArguments.Length];
                            Array.Copy(originalTypeArguments, newTypeArguments, originalTypeArguments.Length);
                            hasChanges = true;
                        }
                        newTypeArguments[i] = inferredType;
                    }
                }

                return newTypeArguments;
            }

            /// <summary>
            /// 判断是否应该使用推断的类型
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static bool ShouldUseInferredType(Type originalType, Type inferredType)
            {
                // 如果原始类型是 object，推断类型更具体，则使用推断类型
                if (originalType == typeof(object) && inferredType != typeof(object))
                {
                    return true;
                }

                // 如果推断类型是原始类型的更具体实现，则使用推断类型
                if (originalType.IsGenericParameter && !inferredType.IsGenericParameter)
                {
                    return true;
                }

                // 如果推断类型比原始类型更具体（继承关系）
                if (originalType.IsAssignableFrom(inferredType) && originalType != inferredType)
                {
                    return true;
                }

                // 如果原始类型是泛型定义，推断类型是具体的泛型实例
                if (originalType.IsGenericTypeDefinition && inferredType.IsGenericType && 
                    !inferredType.IsGenericTypeDefinition)
                {
                    return true;
                }

                return false;
            }

            /// <summary>
            /// 缓存的扩展方法检查
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static bool IsExtensionMethodCached(MethodInfo method)
            {
                return method.IsDefined(typeof(ExtensionAttribute), false);
            }

            /// <summary>
            /// 推断单个泛型参数的最优类型
            /// </summary>
            private static Type InferOptimalSingleType(
                Type originalType,
                Type targetGenericParameter,
                Expression visitedObject,
                Expression[] visitedArguments,
                ParameterInfo[] parameters,
                bool isStatic,
                bool isExtensionMethod)
            {
                Type bestInferredType = null;

                // 1. 实例方法：从对象类型推断
                if (!isStatic && visitedObject?.Type != null)
                {
                    var inferredType = ExtractOptimalTypeFromExpression(visitedObject, targetGenericParameter, originalType);
                    if (IsMoreSpecificType(bestInferredType, inferredType))
                    {
                        bestInferredType = inferredType;
                    }
                }

                // 2. 扩展方法：从第一个参数推断
                if (isExtensionMethod && visitedArguments.Length > 0 && visitedArguments[0]?.Type != null)
                {
                    var inferredType = ExtractOptimalTypeFromExpression(visitedArguments[0], targetGenericParameter, originalType);
                    if (IsMoreSpecificType(bestInferredType, inferredType))
                    {
                        bestInferredType = inferredType;
                    }
                }

                // 3. 从所有参数类型映射推断
                var minLength = Math.Min(parameters.Length, visitedArguments.Length);
                
                for (int i = 0; i < minLength; i++)
                {
                    var parameterType = parameters[i].ParameterType;
                    var argumentType = visitedArguments[i]?.Type;
                    
                    if (argumentType != null)
                    {
                        var inferredType = TryInferFromParameterTypeAdvanced(
                            parameterType, argumentType, targetGenericParameter, originalType);
                        if (IsMoreSpecificType(bestInferredType, inferredType))
                        {
                            bestInferredType = inferredType;
                        }
                    }
                }

                return bestInferredType;
            }

            /// <summary>
            /// 从表达式中提取最优类型
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static Type ExtractOptimalTypeFromExpression(Expression expression, Type targetGenericParameter, Type originalType)
            {
                var expressionType = expression.Type;

                // 如果表达式类型直接匹配目标泛型参数
                if (expressionType == targetGenericParameter)
                {
                    return expressionType;
                }

                // 数组类型优化
                if (expressionType.IsArray)
                {
                    var elementType = expressionType.GetElementType();
                    if (elementType != null && IsMoreSpecificThanOriginal(originalType, elementType))
                    {
                        return elementType;
                    }
                }

                // 泛型集合类型优化
                if (expressionType.IsGenericType)
                {
                    var genericArgs = expressionType.GetGenericArguments();
                    if (genericArgs.Length > 0)
                    {
                        var elementType = genericArgs[0];
                        if (IsMoreSpecificThanOriginal(originalType, elementType))
                        {
                            return elementType;
                        }
                    }
                    
                    // 返回更具体的泛型类型本身
                    if (IsMoreSpecificThanOriginal(originalType, expressionType))
                    {
                        return expressionType;
                    }
                }

                // 如果表达式类型比原始类型更具体，返回它
                if (IsMoreSpecificThanOriginal(originalType, expressionType))
                {
                    return expressionType;
                }

                return null;
            }

            /// <summary>
            /// 高级参数类型推断
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static Type TryInferFromParameterTypeAdvanced(
                Type parameterType, 
                Type argumentType, 
                Type targetGenericParameter,
                Type originalType)
            {
                // 直接类型匹配
                if (parameterType == targetGenericParameter)
                {
                    return argumentType;
                }

                // 泛型类型匹配
                if (parameterType.IsGenericType && argumentType.IsGenericType)
                {
                    var paramGenericArgs = parameterType.GetGenericArguments();
                    var argGenericArgs = argumentType.GetGenericArguments();
                    
                    var minLength = Math.Min(paramGenericArgs.Length, argGenericArgs.Length);
                    for (int i = 0; i < minLength; i++)
                    {
                        if (paramGenericArgs[i] == targetGenericParameter)
                        {
                            var candidateType = argGenericArgs[i];
                            if (IsMoreSpecificThanOriginal(originalType, candidateType))
                            {
                                return candidateType;
                            }
                        }
                    }
                }

                // 如果参数类型是泛型参数，但实参是具体类型
                if (parameterType == targetGenericParameter && IsMoreSpecificThanOriginal(originalType, argumentType))
                {
                    return argumentType;
                }

                return null;
            }

            /// <summary>
            /// 判断类型是否比另一个类型更具体
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static bool IsMoreSpecificType(Type currentBest, Type candidate)
            {
                if (candidate == null)
                {
                    return false;
                }
                if (currentBest == null)
                {
                    return true;
                }
                
                // 非泛型参数比泛型参数更具体
                if (currentBest.IsGenericParameter && !candidate.IsGenericParameter)
                {
                    return true;
                }

                // 具体类型比 object 更具体
                if (currentBest == typeof(object) && candidate != typeof(object))
                {
                    return true;
                }

                // 子类比父类更具体
                if (currentBest.IsAssignableFrom(candidate) && currentBest != candidate)
                {
                    return true;
                }

                return false;
            }

            /// <summary>
            /// 判断候选类型是否比原始类型更具体
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static bool IsMoreSpecificThanOriginal(Type originalType, Type candidateType)
            {
                if (candidateType == null)
                {
                    return false;
                }
                if (originalType == candidateType)
                {
                    return false;
                }

                // object 类型总是可以被更具体的类型替换
                if (originalType == typeof(object) && candidateType != typeof(object))
                {
                    return true;
                }

                // 泛型参数可以被具体类型替换
                if (originalType.IsGenericParameter && !candidateType.IsGenericParameter)
                {
                    return true;
                }

                // 父类可以被子类替换
                if (originalType.IsAssignableFrom(candidateType))
                {
                    return true;
                }

                // 泛型定义可以被具体泛型实例替换
                if (originalType.IsGenericTypeDefinition && candidateType.IsGenericType && 
                    !candidateType.IsGenericTypeDefinition)
                {
                    var candidateDefinition = candidateType.GetGenericTypeDefinition();
                    if (originalType.IsAssignableFrom(candidateDefinition))
                    {
                        return true;
                    }
                }

                return false;
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
            private readonly Dictionary<string, Slot> _memberExpressions = new Dictionary<string, Slot>();
            private bool disposedValue;

            public bool HasMemberSettings => _memberExpressions.Count > 0;
            public void Add(string memberName, Expression valueExpression) => _memberExpressions[memberName] = new Slot(valueExpression);

            public void Ignore(string memberName) => _memberExpressions[memberName] = new Slot();

            public bool TryGetSlot(string memberName, out Slot slot) => _memberExpressions.TryGetValue(memberName, out slot);

            protected virtual void Dispose(bool disposing)
            {
                if (!disposedValue)
                {
                    if (disposing)
                    {
                        _memberExpressions.Clear();
                    }

                    disposedValue = true;
                }
            }

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
            private readonly Type _sourceTypeDefinition;
            private readonly Type _destinationTypeDefinition;
            private readonly InstanceEnumerableFactory _instanceFactory;
            private readonly Func<Type, Type, bool> _bindingConstraints;

            public GenericMapSlot(Type sourceTypeDefinition, Type destinationTypeDefinition, InstanceEnumerableFactory instanceFactory, Func<Type, Type, bool> bindingConstraints)
            {
                _sourceTypeDefinition = sourceTypeDefinition;
                _destinationTypeDefinition = destinationTypeDefinition;
                _instanceFactory = instanceFactory;
                _bindingConstraints = bindingConstraints;
            }

            public bool IsInstanceSlot => true;

            public IInstanceMapSlot CreateMap(Type sourceType, Type destinationType) => _instanceFactory.CreateMap(sourceType, destinationType);

            public bool IsMatch(Type sourceType, Type destinationType)
            {
                if (sourceType.IsGenericType && destinationType.IsGenericType)
                {
                    var sourceTypeDefinition = sourceType.GetGenericTypeDefinition();
                    var destinationTypeDefinition = destinationType.GetGenericTypeDefinition();

                    if (destinationTypeDefinition == _destinationTypeDefinition && _sourceTypeDefinition.IsAmongOf(sourceTypeDefinition, TypeLikeKind.IsGenericTypeDefinition))
                    {
                        var sourceGenericArguments = sourceType.GetGenericArguments();
                        var destinationGenericArguments = destinationType.GetGenericArguments();

                        return _bindingConstraints(sourceGenericArguments[0], destinationGenericArguments[0]);
                    }
                }

                return false;
            }
        }

        private class MapSlot : BaseMapSlot, IMapSlot
        {
            private readonly Type _sourceType;
            private readonly Type _destinationType;
            private readonly List<IMapSlot> _mapSlots;
            private readonly IInstanceFactory _instanceFactory;
            private readonly HashSet<Type> _destinationTypes;

            private bool isSourceConstraints;
            private bool isDestinationConstraints;

            private bool hasMatchConstraints;
            private int destinationConstraintsCount;
            private MatchConstraints matchConstraints;
            private MapConstraints sourceConstraints;
            private List<MapConstraints> destinationConstraints;

            public MapSlot(Type sourceType, Type destinationType, List<IMapSlot> mapSlots)
            {
                _sourceType = sourceType;
                _destinationType = destinationType;
                _mapSlots = mapSlots;

                _destinationTypes = destinationType == MapConstants.ObjectType
                    ? new HashSet<Type>()
                    : new HashSet<Type> { destinationType };
            }

            public MapSlot(Type sourceType, Type destinationType, IInstanceFactory instanceFactory, List<IMapSlot> mapSlots) : this(sourceType, destinationType, mapSlots)
            {
                IsInstanceSlot = true;

                _instanceFactory = instanceFactory;
            }

            public bool IsInstanceSlot { get; }

            public void Include(Type destinationType) => _destinationTypes.Add(destinationType);

            public void IncludeConstraints(MatchConstraints matchConstraints)
            {
                hasMatchConstraints = true;

                this.matchConstraints = matchConstraints;

                foreach (var destinationType in _destinationTypes.Skip(destinationConstraintsCount))
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
                    if (TryMapConstraints(_sourceType, out MapConstraints mapConstraints))
                    {
                        sourceConstraints = mapConstraints;

                        isSourceConstraints = true;
                    }
                }

                destinationConstraintsCount = _destinationTypes.Count;
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
                        var underlyingType = Nullable.GetUnderlyingType(typeConstraints)!;

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

                mapConstraints = new MapConstraints(typeDefinition, constraintSlots);

                return true;
            }

            public bool IsMatch(Type sourceType, Type destinationType)
            {
                if (_sourceType == sourceType)
                {
                    return _destinationTypes.Contains(destinationType);
                }

                if (isDestinationConstraints)
                {
                    if (isSourceConstraints ? sourceConstraints.IsMatch(sourceType) : _sourceType.IsAssignableFrom(sourceType))
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

                return _destinationTypes.Contains(destinationType) && _sourceType.IsAssignableFrom(sourceType);
            }

            public IInstanceMapSlot CreateMap(Type sourceType, Type destinationType) => _instanceFactory.CreateMap(sourceType, destinationType);

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

                if (sourceGenericArguments[0] != _sourceType)
                {
                    throw new NotSupportedException($"{sourceTypeEnumerable}泛型参数必须是“{_sourceType}”！");
                }

                var destinationGenericArguments = destinationTypeEnumerable.GetGenericArguments();

                if (destinationGenericArguments.Length > 1)
                {
                    throw new NotSupportedException($"{destinationTypeEnumerable}泛型参数大于1个！");
                }

                if (destinationGenericArguments[0] != _destinationType)
                {
                    throw new NotSupportedException($"{destinationTypeEnumerable}泛型参数必须是“{_destinationType}”！");
                }

                var sourceTypeDefinition = sourceTypeEnumerable.GetGenericTypeDefinition();
                var destinationTypeDefinition = destinationTypeEnumerable.GetGenericTypeDefinition();

                var instanceFactory = new InstanceEnumerableFactory(body, parameter, parameterOfSet);

                Func<Type, Type, bool> bindingConstraints = _sourceType == MapConstants.ObjectType && _destinationType == MapConstants.ObjectType
                    ? (_, _) => true
                    : IsMatch;

                var mapSlot = new GenericMapSlot(sourceTypeDefinition, destinationTypeDefinition, instanceFactory, bindingConstraints);

                if (body is MemberInitExpression initExpression) //? 忽略已经初始化的属性，避免重复初始化。
                {
                    foreach (var binding in initExpression.Bindings)
                    {
                        mapSlot.Ignore(binding.Member.Name);
                    }
                }

                _mapSlots.Add(mapSlot);
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    _destinationTypes.Clear();
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
            private readonly Expression _body;
            private readonly ParameterExpression _parameter;

            public InstanceFactory(Expression body, ParameterExpression parameter)
            {
                _body = body;
                _parameter = parameter;
            }

            private class MapperSlot : IInstanceMapSlot
            {
                private readonly Expression _body;
                private readonly ParameterExpression _parameter;

                public MapperSlot(Expression body, ParameterExpression parameter)
                {
                    _body = body;
                    _parameter = parameter;
                }

                public Expression Map(Expression source, IMapApplication application)
                {
                    var visitor = new MapExpressionVisitor(application, new Expression[] { _parameter }, new Expression[] { source });

                    return visitor.Visit(_body);
                }
            }

            public IInstanceMapSlot CreateMap(Type sourceType, Type destinationType)
            {
                var source = Parameter(sourceType);

                var visitor = new PrepareMapExpressionVisitor(_parameter.Type, _body.Type, sourceType, destinationType, new Expression[] { _parameter }, new Expression[] { source });

                return new MapperSlot(visitor.Visit(_body), source);
            }
        }

        private class InstanceEnumerableFactory : IInstanceFactory
        {
            private readonly Expression _body;
            private readonly ParameterExpression _parameter;
            private readonly ParameterExpression _parameterOfSet;

            public InstanceEnumerableFactory(Expression body, ParameterExpression parameter, ParameterExpression parameterOfSet)
            {
                _body = body;
                _parameter = parameter;
                _parameterOfSet = parameterOfSet;
            }

            private class MapperSlot : IInstanceMapSlot
            {
                private readonly Expression _body;
                private readonly ParameterExpression _parameter;

                public MapperSlot(Expression body, ParameterExpression parameter)
                {
                    _body = body;
                    _parameter = parameter;
                }

                public Expression Map(Expression source, IMapApplication application)
                {
                    var visitor = new MapExpressionVisitor(application, new Expression[] { _parameter }, new Expression[] { source });

                    return visitor.Visit(_body);
                }
            }

            public IInstanceMapSlot CreateMap(Type sourceType, Type destinationType)
            {
                var source = Parameter(sourceType);

                var destinationListType = typeof(List<>).MakeGenericType(destinationType.GetGenericArguments());

                var originalSetCf = new ConvertIfExpression(_parameterOfSet.Type);

                var prepareVisitor = new ReplaceExpressionVisitor(_parameterOfSet, originalSetCf);

                //? 准备。
                var prepareBodyEx = prepareVisitor.Visit(_body)!;

                var destinationSetCf = new ConvertIfExpression(destinationListType);

                //? 表达式更换。
                var visitor = new PrepareMapExpressionVisitor(_parameter.Type, prepareBodyEx.Type, sourceType, destinationType, new Expression[] { _parameter, originalSetCf }, new Expression[] { source, destinationSetCf });

                var bodyEx = visitor.Visit(prepareBodyEx);

                //? 集合变量。
                var destinationSetVar = Variable(destinationListType);

                var completeVisitor = new ReplaceExpressionVisitor(destinationSetCf, destinationSetVar);

                var completeBodyEx = completeVisitor.Visit(bodyEx);

                var completeEx = Block(new ParameterExpression[] { destinationSetVar }, Assign(destinationSetVar, PrivateMap(source, destinationListType)), completeBodyEx);

                return new MapperSlot(completeEx, source);
            }
        }

        private static bool TryAnalysisInstanceExpression(Expression node, out Expression validExpression)
        {
            validExpression = node;

            return node switch
            {
                NewExpression => true,
                MemberInitExpression => true,
                GotoExpression { Kind: GotoExpressionKind.Return } gotoExpression => TryAnalysisInstanceExpression(gotoExpression.Value, out validExpression),
                BlockExpression { Variables: { Count: 0 }, Expressions: { Count: 1 } } blockExpression => TryAnalysisInstanceExpression(blockExpression.Expressions[0], out validExpression),
                _ => false,
            };
        }

        private class MemberConfigurationExpression<TSource, TMember> : IMemberConfigurationExpression<TSource, TMember>
        {
            private readonly string _memberName;
            private readonly MapSlot _mapSlot;

            public MemberConfigurationExpression(string memberName, MapSlot mapSlot)
            {
                _memberName = memberName;
                _mapSlot = mapSlot;
            }

            public void Auto() => _mapSlot.Add(_memberName, null);

            public void Constant(TMember member) => _mapSlot.Add(_memberName, Expression.Constant(member, typeof(TMember)));

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

                _mapSlot.Add(_memberName, Lambda(Invoke(convertExp, sourceMember.Body), sourceMember.Parameters));
            }

            public void From(Expression<Func<TSource, TMember>> sourceMember)
            {
                if (sourceMember is null)
                {
                    throw new ArgumentNullException(nameof(sourceMember));
                }

                _mapSlot.Add(_memberName, sourceMember);
            }

            public void From(IValueResolver<TSource, TMember> valueResolver)
            {
                if (valueResolver is null)
                {
                    throw new ArgumentNullException(nameof(valueResolver));
                }

                From(x => valueResolver.Resolve(x));
            }

            public void Ignore() => _mapSlot.Ignore(_memberName);
        }

        private class ProfileExpression<TSource, TDestination> : IProfileExpression<TSource, TDestination>
        {
            private readonly MapSlot _mapSlot;

            public ProfileExpression(MapSlot mapSlot)
            {
                _mapSlot = mapSlot;
            }

            public IIncludeProfileExpression<TSource, TDestination> Include<TAssignableToDestination>() where TAssignableToDestination : TDestination
            {
                _mapSlot.Include(typeof(TAssignableToDestination));

                return this;
            }

            private static string NameOfAnalysis(Expression node)
            {
                return node switch
                {
                    MemberExpression member => member.Member.Name,
                    InvocationExpression { Arguments: { Count: 1 } } invocation => NameOfAnalysis(invocation.Expression),
                    LambdaExpression { Parameters: { Count: 1 } } lambda => NameOfAnalysis(lambda.Body),
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

                var options = new MemberConfigurationExpression<TSource, TMember>(memberName, _mapSlot);

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

                _mapSlot.NewEnumerable(typeof(TSourceEnumerable), typeof(TDestinationEnumerable), body, destinationOptions.Parameters[0], destinationOptions.Parameters[1]);

                return this;
            }

            public void IncludeConstraints(MatchConstraints matchConstraints)
            {
                if (matchConstraints is null)
                {
                    throw new ArgumentNullException(nameof(matchConstraints));
                }

                _mapSlot.IncludeConstraints(matchConstraints);
            }
        }

        private bool disposedValue;

        private readonly object _lockObj = new object();
        private readonly List<IMapSlot> _mapSlots = new List<IMapSlot>();
        private readonly HashSet<TypeCode> _missCachings = new HashSet<TypeCode>(TypeCode.InstanceComparer);
        private readonly Dictionary<TypeCode, IMapSlot> _mapCachings = new Dictionary<TypeCode, IMapSlot>(TypeCode.InstanceComparer);
        private readonly Dictionary<TypeCode, IInstanceMapSlot> _instanceMapCachings = new Dictionary<TypeCode, IInstanceMapSlot>(TypeCode.InstanceComparer);

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
            if (_mapCachings.TryGetValue(new TypeCode(sourceType, destinationType), out IMapSlot mapSlot))
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

                    if (TrySolve(destinationPrt,
                            sourcePrt,
                            application,
                            out var destinationRs))
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

                        if (TrySolve(destinationPrt,
                                sourcePrt,
                                application,
                                out var destinationRs))
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

            if (_mapCachings.ContainsKey(typeCode))
            {
                return true;
            }

            if (_missCachings.Contains(typeCode))
            {
                return false;
            }

            lock (_lockObj)
            {
                if (_mapCachings.ContainsKey(typeCode))
                {
                    return true;
                }

                if (_missCachings.Contains(typeCode))
                {
                    return false;
                }

                foreach (var mapSlot in _mapSlots)
                {
                    if (mapSlot.IsMatch(typeCode.X, typeCode.Y))
                    {
                        if (mapSlot.IsInstanceSlot)
                        {
                            _instanceMapCachings[typeCode] = mapSlot.CreateMap(typeCode.X, typeCode.Y);
                        }

                        _mapCachings[typeCode] = mapSlot;

                        return true;
                    }
                }

                _missCachings.Add(typeCode);

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
        public Expression Map(Expression sourceExpression, Type destinationType, IMapApplication application) => ToSolve(sourceExpression, destinationType, application);

        /// <inheritdoc />
        protected override Expression ToSolveCore(Expression sourceExpression, Type destinationType, IMapApplication application)
        {
            var sourceType = sourceExpression.Type;

            var typeCode = new TypeCode(sourceType, destinationType);

            if (_instanceMapCachings.TryGetValue(typeCode, out IInstanceMapSlot mapSlot))
            {
                var destinationVariable = Variable(destinationType, "instance");

                List<ParameterExpression> variables = new List<ParameterExpression>(1)
                {
                    destinationVariable
                };

                List<Expression> expressions = new List<Expression>(3);

                var instanceExpression = mapSlot.Map(sourceExpression, application);

                expressions.Add(Assign(destinationVariable, instanceExpression));

                var bodyExp = base.ToSolve(sourceExpression, sourceType, destinationVariable, destinationType, application);

                expressions.Add(bodyExp);

                expressions.Add(destinationVariable);

                return Block(destinationType, variables, expressions);
            }

            return base.ToSolveCore(sourceExpression, destinationType, application);
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

            var mapSlot = new MapSlot(sourceType, destinationType, _mapSlots);

            _mapSlots.Add(mapSlot);

            if (!isContract)
            {
                //? 优先使用自定义。
                _mapCachings[new TypeCode(sourceType, destinationType)] = mapSlot;
            }

            return new ProfileExpression<TSource, TDestination>(mapSlot);
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

            var mapSlot = new MapSlot(sourceType, destinationType, instanceFactory, _mapSlots);

            if (body is MemberInitExpression initExpression) //? 忽略已经初始化的属性，避免重复初始化。
            {
                foreach (var binding in initExpression.Bindings)
                {
                    mapSlot.Ignore(binding.Member.Name);
                }
            }

            _mapSlots.Add(mapSlot);

            //? 优先使用自定义。
            var typeCode = new TypeCode(sourceType, destinationType);

            _mapCachings[typeCode] = mapSlot;

            _instanceMapCachings[typeCode] = instanceFactory.CreateMap(sourceType, destinationType);

            return new ProfileExpression<TSource, TDestination>(mapSlot);
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
                    foreach (var mapSlot in _mapSlots)
                    {
                        mapSlot.Dispose();
                    }

                    _mapSlots.Clear();

                    _missCachings.Clear();

                    _instanceMapCachings.Clear();
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