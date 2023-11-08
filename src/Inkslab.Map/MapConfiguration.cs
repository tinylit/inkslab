﻿using Inkslab.Map.Maps;
using Inkslab.Map.Visitors;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Security.AccessControl;

namespace Inkslab.Map
{
    using static Expression;

    /// <summary>
    /// 映射配置。
    /// </summary>
    public class MapConfiguration : Singleton<MapConfiguration>, IMapConfiguration, IConfiguration
    {
        private readonly IList<IMap> maps;
        private readonly List<Profile> profiles = new List<Profile>();

        /// <summary>
        /// 默认映射规则集合。
        /// </summary>
        public readonly static IList<IMap> DefaultMaps = new List<IMap>
        {
            new EnumerableMap(),
            new ConvertMap(),
            new ToStringMap(),
            new ParseStringMap(),
            new EnumUnderlyingTypeMap(),
            new StringToEnumMap(),
            new KeyValueMap(),
            new ConstructorMap(),
            new FromKeyIsStringValueIsObjectMap(),
            new ToKeyIsStringValueIsObjectMap(),
            new CloneableMap(),
            new DefaultMap()
        };

        /// <summary>
        /// 映射配置。
        /// </summary>
        private MapConfiguration() : this(new Configuration
        {
            IsDepthMapping = true,
            AllowPropagationNullValues = false
        })
        {
        }

        /// <summary>
        /// 映射配置。
        /// </summary>
        public MapConfiguration(IConfiguration configuration) : this(DefaultMaps, configuration)
        {
        }

        /// <summary>
        /// 映射配置。
        /// </summary>
        public MapConfiguration(IList<IMap> maps, IConfiguration configuration)
        {
            if (configuration is null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            IsDepthMapping = configuration.IsDepthMapping;
            AllowPropagationNullValues = configuration.AllowPropagationNullValues;

            this.maps = maps ?? throw new ArgumentNullException(nameof(maps));
        }

        /// <summary>
        /// 深度映射。
        /// </summary>
        public bool IsDepthMapping { get; }

        /// <summary>
        /// 允许空值传播。
        /// </summary>
        public bool AllowPropagationNullValues { get; }

        /// <summary>
        /// 是否匹配。
        /// </summary>
        /// <param name="sourceType">源。</param>
        /// <param name="destinationType">目标。</param>
        /// <returns>是否匹配。</returns>
        public bool IsMatch(Type sourceType, Type destinationType)
        {
            if (sourceType.IsNullable())
            {
                sourceType = Nullable.GetUnderlyingType(sourceType);
            }

            if (destinationType.IsNullable())
            {
                destinationType = Nullable.GetUnderlyingType(destinationType);
            }

            if (sourceType.IsValueType && sourceType == destinationType)
            {
                return true;
            }

            return profiles.Any(x => x.IsMatch(sourceType, destinationType)) || maps.Any(x => x.IsMatch(sourceType, destinationType));
        }

        /// <summary>
        /// 添加配置。
        /// </summary>
        /// <param name="profile">配置。</param>
        public void AddProfile(Profile profile)
        {
            if (profile is null)
            {
                throw new ArgumentNullException(nameof(profile));
            }

            profiles.Add(profile);
        }

        /// <summary>
        /// 添加配置。
        /// </summary>
        /// <param name="profileType">配置类。</param>
        public void AddProfile(Type profileType)
        {
            if (profileType is null)
            {
                throw new ArgumentNullException(nameof(profileType));
            }

            if (profileType.IsAbstract)
            {
                throw new NotSupportedException($"不支持“{profileType}”抽象类！");
            }

            if (!profileType.IsSubclassOf(typeof(Profile)))
            {
                throw new NotSupportedException($"“{profileType}”不是“{typeof(Profile)}”的子类！");
            }

            bool throwError = true;

            var constructorInfos = profileType.GetConstructors();

            foreach (var constructorInfo in constructorInfos)
            {
                var parameterInfos = constructorInfo.GetParameters();

                if (parameterInfos.Length == 0 || parameterInfos.All(x => x.IsOptional || x.ParameterType == typeof(IMapConfiguration) || x.ParameterType == typeof(IConfiguration)))
                {
                    AddProfile((Profile)constructorInfo.Invoke(Array.ConvertAll(parameterInfos, x =>
                    {
                        if (x.ParameterType == typeof(IConfiguration) || x.ParameterType == typeof(IMapConfiguration))
                        {
                            return this;
                        }

                        return x.DefaultValue;
                    })));

                    throwError = false;

                    break;
                }
            }

            if (throwError)
            {
                throw new NotSupportedException($"未找到“{profileType}”类型的有效构造函数！");
            }
        }

        /// <inheritdoc/>
        public Expression Map(Expression sourceExpression, Type destinationType, IMapApplication application)
        {
            var sourceType = sourceExpression.Type;

            if (application is IMap map && map.IsMatch(sourceType, destinationType))
            {
                return Ignore(map.ToSolve(sourceExpression, destinationType, application));
            }

            var conversionType = destinationType;

            if (conversionType.IsInterface)
            {
                if (conversionType.IsGenericType)
                {
                    var typeDefinition = conversionType.GetGenericTypeDefinition();

                    if (typeDefinition == typeof(IList<>)
                        || typeDefinition == typeof(IReadOnlyList<>)
                        || typeDefinition == typeof(ICollection<>)
                        || typeDefinition == typeof(IReadOnlyCollection<>)
                        || typeDefinition == typeof(IEnumerable<>))
                    {
                        conversionType = typeof(List<>).MakeGenericType(conversionType.GetGenericArguments());
                    }
                    else if (typeDefinition == typeof(IDictionary<,>)
                        || typeDefinition == typeof(IReadOnlyDictionary<,>))
                    {
                        conversionType = typeof(Dictionary<,>).MakeGenericType(conversionType.GetGenericArguments());
                    }
                }
                else if (conversionType == typeof(IEnumerable)
                    || conversionType == typeof(ICollection)
                    || conversionType == typeof(IList))
                {
                    conversionType = typeof(List<object>);
                }
            }
            else if (conversionType == MapConstants.ObjectType)
            {
                if (sourceType == conversionType)
                {
                    return sourceExpression;
                }

                if (sourceType.IsValueType)
                {
                    return Convert(Map(sourceExpression, sourceType, application), destinationType);
                }

                conversionType = sourceType;
            }

            if (conversionType.IsAbstract)
            {
                throw new InvalidCastException($"无法从源【{sourceType}】分析到目标【{destinationType}】的可实列化类型！");
            }

            return MapCore(sourceExpression, sourceType, conversionType, application);
        }

        private Expression MapCore(Expression sourceExpression, Type sourceType, Type destinationType, IMapApplication application)
        {
            if (application is IMap map && map.IsMatch(sourceType, destinationType))
            {
                return Ignore(map.ToSolve(sourceExpression, destinationType, application));
            }

            if (sourceType == MapConstants.ObjectType)
            {
                return MapAnyOfObject(sourceExpression, sourceType, destinationType, application);
            }

            if (sourceType.IsValueType || destinationType.IsValueType)
            {
                return MapAnyOfValueType(sourceExpression, sourceType, destinationType, application);
            }

            if (sourceType == destinationType && sourceType == MapConstants.StirngType)
            {
                if (AllowPropagationNullValues)
                {
                    return sourceExpression;
                }

                return IgnoreIfNull(sourceExpression, true);
            }

            if (destinationType.IsAssignableFrom(sourceType))
            {
                if (IsDepthMapping)
                {
                    if (AllowPropagationNullValues)
                    {
                        return Condition(Equal(sourceExpression, DefaultValue(sourceType)), DefaultValue(destinationType), Map(sourceExpression, sourceType, destinationType, application));
                    }

                    return Map(IgnoreIfNull(sourceExpression), sourceType, destinationType, application);
                }

                if (AllowPropagationNullValues)
                {
                    return sourceExpression;
                }

                return IgnoreIfNull(sourceExpression);
            }

            if (AllowPropagationNullValues)
            {
                return Condition(Equal(sourceExpression, DefaultValue(sourceType)), DefaultValue(destinationType), Map(sourceExpression, sourceType, destinationType, application));
            }

            return Map(IgnoreIfNull(sourceExpression), sourceType, destinationType, application);
        }

        private Expression MapAnyOfObject(Expression sourceExpression, Type sourceType, Type destinationType, IMapApplication application)
        {
            if (destinationType.IsNullable())
            {
                var conversionType = Nullable.GetUnderlyingType(destinationType);

                return Condition(TypeIs(sourceExpression, conversionType),
                        TypeAs(sourceExpression, destinationType),
                        New(destinationType.GetConstructor(new Type[] { conversionType }), MapAnyOfObject(sourceExpression, sourceType, conversionType, application))
                    );
            }

            if (destinationType.IsEnum)
            {
                var conversionType = Enum.GetUnderlyingType(destinationType);

                return Condition(TypeIs(sourceExpression, conversionType),
                        Map(Convert(sourceExpression, conversionType), destinationType, application),
                        Map(MapAnyOfObject(sourceExpression, sourceType, conversionType, application), destinationType, application)
                    );
            }

            if (destinationType.IsPrimitive ||
                destinationType.IsValueType && (
                    destinationType == typeof(decimal)
                    || destinationType == typeof(DateTime)
                    || destinationType == typeof(Guid)
                    || destinationType == typeof(TimeSpan)
                    || destinationType == typeof(DateTimeOffset))
                || destinationType == typeof(Version))
            {
                return Condition(TypeIs(sourceExpression, MapConstants.StirngType),
                        Map(TypeAs(sourceExpression, MapConstants.StirngType), destinationType, application),
                        MapAnyByConvert(sourceExpression, sourceType, destinationType, application)
                    );
            }

            return MapAnyByConvert(sourceExpression, sourceType, destinationType, application);
        }

        private Expression MapAnyByConvert(Expression sourceExpression, Type sourceType, Type destinationType, IMapApplication application)
        {
            if (destinationType.IsValueType)
            {
                if (AllowPropagationNullValues)
                {
                    return Convert(sourceExpression, destinationType);
                }

                return Convert(IgnoreIfNull(sourceExpression), destinationType);
            }

            if (IsDepthMapping)
            {
                if (AllowPropagationNullValues)
                {
                    return Condition(Equal(sourceExpression, DefaultValue(sourceType)), DefaultValue(destinationType), Map(Convert(sourceExpression, destinationType), destinationType, destinationType, application));
                }

                return Map(Convert(IgnoreIfNull(sourceExpression), destinationType), destinationType, destinationType, application);
            }

            if (AllowPropagationNullValues)
            {
                return Convert(sourceExpression, destinationType);
            }

            return Convert(IgnoreIfNull(sourceExpression), destinationType);
        }

        /// <summary>
        /// 含值类型。
        /// </summary>
        private Expression MapAnyOfValueType(Expression sourceExpression, Type sourceType, Type destinationType, IMapApplication application)
        {
            if (sourceType == destinationType)
            {
                if (AllowPropagationNullValues || !sourceType.IsNullable())
                {
                    return sourceExpression;
                }

                return IgnoreIfNull(sourceExpression, true);
            }

            if (sourceType.IsValueType)
            {
                if (sourceType.IsNullable())
                {
                    var underlyingSourceType = Nullable.GetUnderlyingType(sourceType);

                    if (AllowPropagationNullValues)
                    {
                        if (destinationType.IsNullable())
                        {
                            var underlyingDestinationType = Nullable.GetUnderlyingType(destinationType);

                            var destinationExpression = MapAnyOfValueType(Property(sourceExpression, "Value"), sourceType, underlyingDestinationType, application);

                            return Condition(Property(sourceExpression, "HasValue"), New(destinationType.GetConstructor(new Type[] { underlyingDestinationType }), destinationExpression), DefaultValue(destinationType));
                        }

                        return Condition(Property(sourceExpression, "HasValue"), MapAnyOfValueType(Property(sourceExpression, "Value"), underlyingSourceType, destinationType, application), DefaultValue(destinationType));
                    }

                    if (destinationType.IsNullable())
                    {
                        var underlyingDestinationType = Nullable.GetUnderlyingType(destinationType);

                        var destinationExpression = MapGeneral(IgnoreIfNull(sourceExpression), sourceType, underlyingDestinationType, application);

                        return New(destinationType.GetConstructor(new Type[] { underlyingDestinationType }), destinationExpression);
                    }

                    return MapAnyOfValueType(IgnoreIfNull(sourceExpression), underlyingSourceType, destinationType, application);
                }

                //? 非可空的值类型，不存在 null 值，不存在【AllowPropagationNullValues】的约束，均可映射。
                if (destinationType.IsNullable())
                {
                    var underlyingDestinationType = Nullable.GetUnderlyingType(destinationType);

                    var destinationExpression = sourceType == underlyingDestinationType
                            ? sourceExpression
                            : MapAnyOfValueType(sourceExpression, sourceType, underlyingDestinationType, application);

                    return New(destinationType.GetConstructor(new Type[] { underlyingDestinationType }), destinationExpression);
                }

                return MapGeneral(sourceExpression, sourceType, destinationType, application);
            }

            if (AllowPropagationNullValues)
            {
                if (destinationType.IsNullable())
                {
                    var underlyingDestinationType = Nullable.GetUnderlyingType(destinationType);

                    return Condition(Equal(sourceExpression, DefaultValue(sourceType)), DefaultValue(destinationType), New(destinationType.GetConstructor(new Type[] { underlyingDestinationType }), MapGeneral(sourceExpression, sourceType, underlyingDestinationType, application)));
                }

                return Condition(Equal(sourceExpression, DefaultValue(sourceType)), DefaultValue(destinationType), MapGeneral(sourceExpression, sourceType, destinationType, application));
            }

            if (destinationType.IsNullable())
            {
                var underlyingDestinationType = Nullable.GetUnderlyingType(destinationType);

                return New(destinationType.GetConstructor(new Type[] { underlyingDestinationType }), MapGeneral(IgnoreIfNull(sourceExpression), sourceType, underlyingDestinationType, application));
            }

            return MapGeneral(IgnoreIfNull(sourceExpression), sourceType, destinationType, application);
        }

        private Expression Map(Expression sourceExpression, Type sourceType, Type destinationType, IMapApplication application)
        {
            var visitor = new IgnoreIfNullExpressionVisitor();

            var sourceIgnore = visitor.Visit(sourceExpression);

            if (visitor.HasIgnore)
            {
                if (sourceType.IsSimple())
                {
                    return IgnoreIfNull(Ignore(MapIgnore(sourceIgnore, sourceType, destinationType, application)), visitor.Test);
                }

                var sourceVariable = Variable(sourceIgnore.Type);

                var destinationExp = Ignore(MapIgnore(sourceVariable, sourceType, destinationType, application));

                return IgnoreIfNull(Block(new ParameterExpression[] { sourceVariable }, Assign(sourceVariable, sourceIgnore), destinationExp), visitor.Test);
            }

            return Ignore(MapIgnore(sourceIgnore, sourceType, destinationType, application));
        }

        private Expression MapIgnore(Expression sourceExpression, Type sourceType, Type destinationType, IMapApplication application)
        {
            if (sourceType.IsClass && destinationType.IsClass)
            {
                foreach (var profile in profiles)
                {
                    if (profile.IsMatch(sourceType, destinationType))
                    {
                        return profile.Map(sourceExpression, destinationType, application);
                    }
                }
            }

            return MapGeneral(sourceExpression, sourceType, destinationType, application);
        }

        private Expression MapGeneral(Expression sourceExpression, Type sourceType, Type destinationType, IMapApplication application)
        {
            foreach (var map in maps)
            {
                if (map.IsMatch(sourceType, destinationType))
                {
                    return map.ToSolve(sourceExpression, destinationType, application);
                }
            }

            throw new InvalidCastException($"无法从源【{sourceType}】源映射到目标【{destinationType}】的类型！");
        }

        private static Expression DefaultValue(Type destinationType) => destinationType.IsValueType ? Default(destinationType) : Constant(null, destinationType);

        #region ExpressionVisitor

        private static Expression Ignore(Expression node) => MapperExpressionVisitor.Instance.Visit(node);

        private static Expression IgnoreIfNull(Expression node, bool keepNullable = false)
        {
            if (node.NodeType == IgnoreIf)
            {
                return node;
            }

            if (node.NodeType == ExpressionType.TypeAs)
            {
                return node;
            }

            if (node.NodeType == ExpressionType.Parameter)
            {
                if (keepNullable || !node.Type.IsNullable())
                {
                    return node;
                }

                return Property(node, "Value");
            }

            Expression ignoreIf = node;

            while (ignoreIf is MethodCallExpression methodCall)
            {
                if (methodCall.Arguments.Count > (methodCall.Method.IsStatic ? 1 : 0))
                {
                    break;
                }

                ignoreIf = methodCall.Object ?? methodCall.Arguments[0];
            }

            if (ignoreIf.Type.IsValueType)
            {
                if (node.Type.IsNullable())
                {
                    return new IgnoreIfNullExpression(node, Property(ignoreIf, "HasValue"), keepNullable);
                }

                return node;
            }

            return new IgnoreIfNullExpression(node, NotEqual(ignoreIf, Constant(null, ignoreIf.Type)), keepNullable);
        }

        private static Expression IgnoreIfNull(Expression node, Expression test) => new IgnoreIfNullExpression(node, test, true);

        private class MapperExpressionVisitor : ExpressionVisitor
        {
            private MapperExpressionVisitor() { }

            protected override Expression VisitBinary(BinaryExpression node)
            {
                var visitor = new IgnoreIfNullExpressionVisitor();

                var body = visitor.Visit(node.Right);

                if (visitor.HasIgnore)
                {
                    return IfThen(visitor.Test, node.Update(node.Left, node.Conversion, body));
                }

                return node;
            }

            protected override Expression VisitMemberInit(MemberInitExpression node)
            {
                var bindings = new List<MemberBinding>(node.Bindings.Count);
                var expressionTests = new List<Tuple<Expression, MemberInfo, Expression>>();

                foreach (MemberAssignment assignment in node.Bindings.Cast<MemberAssignment>())
                {
                    var visitor = new IgnoreIfNullExpressionVisitor();

                    var body = visitor.Visit(assignment.Expression);

                    if (visitor.HasIgnore)
                    {
                        expressionTests.Add(Tuple.Create(visitor.Test, assignment.Member, body));
                    }
                    else
                    {
                        bindings.Add(assignment);
                    }
                }

                var instance = (NewExpression)base.VisitNew(node.NewExpression);

                if (expressionTests.Count == 0)
                {
                    return node.Update(instance, bindings);
                }

                var instanceVar = Variable(instance.Type);

                var expressions = new List<Expression>(expressionTests.Count + 1)
                {
                    Assign(instanceVar, node.Update(instance, bindings))
                };

                foreach (var tuple in expressionTests)
                {
                    expressions.Add(IfThen(tuple.Item1, Assign(MakeMemberAccess(instanceVar, tuple.Item2), tuple.Item3)));
                }

                return Block(new ParameterExpression[] { instanceVar }, expressions);
            }

            public static MapperExpressionVisitor Instance = new MapperExpressionVisitor();
        }

        /// <summary>
        /// 忽略表达式枚举值。
        /// </summary>
        private const ExpressionType IgnoreIf = (ExpressionType)(-1);

        /// <summary>
        /// 为 null 时，忽略。
        /// </summary>
        private class IgnoreIfNullExpressionVisitor : ExpressionVisitor
        {
            private readonly HashSet<Expression> ignoreIfNull = new HashSet<Expression>();

            public IgnoreIfNullExpressionVisitor()
            {
            }

            /// <summary>
            /// 是否含有忽略条件。
            /// </summary>
            public bool HasIgnore { private set; get; }

            /// <summary>
            /// 是否不为空。
            /// </summary>
            public Expression Test { private set; get; }

            /// <inheritdoc/>
            protected override Expression VisitSwitch(SwitchExpression node)
            {
                return node.Update(base.Visit(node.SwitchValue), node.Cases, node.DefaultBody);
            }

            /// <inheritdoc/>
            protected override Expression VisitBinary(BinaryExpression node) => node;

            /// <inheritdoc/>
            protected override MemberAssignment VisitMemberAssignment(MemberAssignment node) => node;

            /// <summary>
            /// 访问为空忽略表达式。
            /// </summary>
            /// <param name="node">判空节点。</param>
            /// <returns>新的表达式。</returns>
            public Expression VisitIgnoreIfNull(IgnoreIfNullExpression node)
            {
                if (ignoreIfNull.Add(node.Test))
                {
                    if (HasIgnore)
                    {
                        Test = AndAlso(Test, node.Test);
                    }
                    else
                    {
                        HasIgnore = true;

                        Test = node.Test;
                    }
                }

                return node.CanReduce ? node.Reduce() : node;
            }
        }

        /// <summary>
        /// 忽略表达式。
        /// </summary>
        private class IgnoreIfNullExpression : Expression
        {
            private readonly Type type;
            private readonly Expression node;
            private readonly Expression test;
            private readonly bool keepNullable;

            /// <summary>
            /// 构造函数。
            /// </summary>
            /// <param name="node">数据节点。</param>
            /// <param name="test">条件节点。</param>
            /// <param name="keepNullable">是否保持原可空类型。</param>
            /// <exception cref="ArgumentNullException"></exception>
            public IgnoreIfNullExpression(Expression node, Expression test, bool keepNullable)
            {
                this.node = node ?? throw new ArgumentNullException(nameof(node));
                this.test = test ?? throw new ArgumentNullException(nameof(test));
                this.keepNullable = keepNullable;

                if (node.Type.IsNullable())
                {
                    type = keepNullable ? node.Type : Nullable.GetUnderlyingType(node.Type);
                }
                else
                {
                    type = node.Type;
                }
            }

            /// <summary>
            /// 非空条件。
            /// </summary>
            public Expression Test => test;

            /// <summary>
            /// 类型。
            /// </summary>
            public override Type Type => type;

            /// <summary>
            /// 节点类型。
            /// </summary>
            public override ExpressionType NodeType => IgnoreIf;

            public override bool CanReduce => true;

            public override Expression Reduce()
            {
                if (keepNullable)
                {
                    goto label_original;
                }

                if (node.Type.IsNullable())
                {
                    return Property(node, "Value");
                }

label_original:
                return node;
            }

            /// <summary>
            /// 分配为默认值。
            /// </summary>
            /// <param name="visitor">访问器。</param>
            /// <returns></returns>
            protected override Expression Accept(ExpressionVisitor visitor)
            {
                if (visitor is IgnoreIfNullExpressionVisitor ignoreVisitor)
                {
                    return ignoreVisitor.VisitIgnoreIfNull(this);
                }

                return visitor.Visit(Reduce());
            }
        }

        #endregion
    }
}
