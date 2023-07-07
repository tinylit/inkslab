using Inkslab.Map.Maps;
using Inkslab.Map.Visitors;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

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

        /// <summary>
        /// 映射。
        /// </summary>
        /// <param name="sourceExpression">源。</param>
        /// <param name="destinationType">目标。</param>
        /// <returns>映射表达式。</returns>
        /// <exception cref="InvalidCastException">无法转换。</exception>
        public Expression Map(Expression sourceExpression, Type destinationType)
        {
            var conversionType = destinationType;

            var sourceType = sourceExpression.Type;

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
                if (sourceType.IsValueType)
                {
                    return Convert(Map(sourceExpression, sourceType), destinationType);
                }

                conversionType = sourceType;
            }

            if (conversionType.IsAbstract)
            {
                throw new InvalidCastException($"无法从源【{sourceType}】分析到目标【{destinationType}】的可实列化类型！");
            }

            if (sourceType == MapConstants.ObjectType)
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
                        return Condition(Equal(sourceExpression, Default(sourceType)), Default(conversionType), Map(Convert(sourceExpression, destinationType), destinationType, destinationType, conversionType));
                    }

                    return Map(Convert(IgnoreIfNull(sourceExpression), destinationType), destinationType, destinationType, conversionType);
                }

                if (AllowPropagationNullValues)
                {
                    return Convert(sourceExpression, destinationType);
                }

                return Convert(IgnoreIfNull(sourceExpression), destinationType);
            }

            if (sourceType.IsValueType || conversionType.IsValueType)
            {
                return MapAnyOfValueType(sourceExpression, sourceType, destinationType, conversionType);
            }

            if (conversionType.IsAssignableFrom(sourceType))
            {
                if (IsDepthMapping)
                {
                    if (AllowPropagationNullValues)
                    {
                        return Condition(Equal(sourceExpression, Default(sourceType)), Default(conversionType), Map(sourceExpression, sourceType, destinationType, conversionType));
                    }

                    return Map(IgnoreIfNull(sourceExpression), sourceType, destinationType, conversionType);
                }

                if (AllowPropagationNullValues)
                {
                    return sourceExpression;
                }

                return IgnoreIfNull(sourceExpression);
            }

            if (AllowPropagationNullValues)
            {
                return Condition(Equal(sourceExpression, Default(sourceType)), Default(conversionType), Map(sourceExpression, sourceType, destinationType, conversionType));
            }

            return Map(IgnoreIfNull(sourceExpression), sourceType, destinationType, conversionType);
        }

        /// <summary>
        /// 含值类型。
        /// </summary>
        private Expression MapAnyOfValueType(Expression sourceExpression, Type sourceType, Type destinationType, Type conversionType)
        {
            if (sourceType == conversionType)
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
                        if (conversionType.IsNullable())
                        {
                            var underlyingDestinationType = Nullable.GetUnderlyingType(conversionType);

                            var destinationExpression = MapGeneral(Property(sourceExpression, "Value"), sourceType, destinationType, underlyingDestinationType);

                            return Condition(Property(sourceExpression, "HasValue"), New(conversionType.GetConstructor(new Type[] { underlyingDestinationType }), destinationExpression), Default(conversionType));
                        }

                        return Condition(Property(sourceExpression, "HasValue"), MapGeneral(Property(sourceExpression, "Value"), underlyingSourceType, destinationType, conversionType), Default(conversionType));
                    }

                    if (conversionType.IsNullable())
                    {
                        var underlyingDestinationType = Nullable.GetUnderlyingType(conversionType);

                        var destinationExpression = MapGeneral(IgnoreIfNull(sourceExpression), sourceType, destinationType, underlyingDestinationType);

                        return New(conversionType.GetConstructor(new Type[] { underlyingDestinationType }), destinationExpression);
                    }

                    return MapGeneral(IgnoreIfNull(sourceExpression), underlyingSourceType, destinationType, conversionType);
                }

                //? 非可空的值类型，不存在 null 值，不存在【AllowPropagationNullValues】的约束，均可映射。
                if (conversionType.IsNullable())
                {
                    var underlyingDestinationType = Nullable.GetUnderlyingType(conversionType);

                    var destinationExpression = sourceType == underlyingDestinationType
                            ? sourceExpression
                            : MapGeneral(sourceExpression, sourceType, destinationType, underlyingDestinationType);

                    return New(conversionType.GetConstructor(new Type[] { underlyingDestinationType }), destinationExpression);
                }

                return MapGeneral(sourceExpression, sourceType, destinationType, conversionType);
            }

            if (AllowPropagationNullValues)
            {
                if (conversionType.IsNullable())
                {
                    var underlyingDestinationType = Nullable.GetUnderlyingType(conversionType);

                    return Condition(Equal(sourceExpression, Default(sourceType)), Default(conversionType), New(conversionType.GetConstructor(new Type[] { underlyingDestinationType }), MapGeneral(sourceExpression, sourceType, destinationType, underlyingDestinationType)));
                }

                return Condition(Equal(sourceExpression, Default(sourceType)), Default(conversionType), MapGeneral(sourceExpression, sourceType, destinationType, conversionType));
            }

            if (conversionType.IsNullable())
            {
                var underlyingDestinationType = Nullable.GetUnderlyingType(conversionType);

                return New(conversionType.GetConstructor(new Type[] { underlyingDestinationType }), MapGeneral(IgnoreIfNull(sourceExpression), sourceType, destinationType, underlyingDestinationType));
            }

            return MapGeneral(IgnoreIfNull(sourceExpression), sourceType, destinationType, conversionType);
        }

        private Expression Map(Expression sourceExpression, Type sourceType, Type destinationType, Type conversionType)
        {
            if (sourceType.IsClass && conversionType.IsClass)
            {
                foreach (var profile in profiles)
                {
                    if (profile.IsMatch(sourceType, conversionType))
                    {
                        return profile.Map(sourceExpression, conversionType, this);
                    }
                }
            }

            return MapGeneral(sourceExpression, sourceType, destinationType, conversionType);
        }

        private Expression MapGeneral(Expression sourceExpression, Type sourceType, Type destinationType, Type conversionType)
        {
            foreach (var map in maps)
            {
                if (map.IsMatch(sourceType, conversionType))
                {
                    return map.ToSolve(sourceExpression, sourceType, conversionType, this);
                }
            }

            throw new InvalidCastException($"无法从源【{sourceType}】源映射到目标【{destinationType}】的类型！");
        }

        private static Expression IgnoreIfNull(Expression node, bool keepNullable = false) => new IgnoreIfNullExpression(node, keepNullable);

        /// <summary>
        /// 忽略表达式。
        /// </summary>
        private class IgnoreIfNullExpression : Expression
        {
            private readonly Type type;
            private readonly Expression node;
            private readonly bool keepNullable;

            /// <summary>
            /// 忽略表达式枚举值。
            /// </summary>
            private const ExpressionType IgnoreIfNull = (ExpressionType)(-1);

            /// <summary>
            /// 构造函数。
            /// </summary>
            /// <param name="node">表达式节点。</param>
            /// <param name="keepNullable"></param>
            public IgnoreIfNullExpression(Expression node, bool keepNullable)
            {
                this.node = node ?? throw new ArgumentNullException(nameof(node));
                this.keepNullable = keepNullable;

                if (node is IgnoreIfNullExpression ignoreNode)
                {
                    this.node = ignoreNode.node;
                    type = ignoreNode.type;
                }
                else if (keepNullable || !node.Type.IsValueType)
                {
                    this.node = node;
                    type = node.Type;
                }
                else if (node.Type.IsNullable())
                {
                    this.node = node;
                    type = keepNullable ? node.Type : Nullable.GetUnderlyingType(node.Type);
                }
                else
                {
                    throw new ArgumentException($"类型【{node.Type}】的值不可能为null！");
                }
            }

            /// <summary>
            /// 类型。
            /// </summary>
            public override Type Type => type;

            /// <summary>
            /// 节点类型。
            /// </summary>
            public override ExpressionType NodeType => IgnoreIfNull;

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
    }
}
