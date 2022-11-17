using Inkslab.Map.Expressions;
using System.Collections;
using System.Linq.Expressions;

namespace Inkslab.Map
{
    using static Expression;

    /// <summary>
    /// 映射配置。
    /// </summary>
    public class MapConfiguration : Singleton<MapConfiguration>, IMapConfiguration, IConfiguration
    {
        private readonly MapCollection maps;
        private readonly List<Profile> profiles = new List<Profile>();

        /// <summary>
        /// 映射配置。
        /// </summary>
        private MapConfiguration() : this(new Configuration())
        {
        }

        /// 映射配置。
        /// </summary>
        private MapConfiguration(IConfiguration configuration) : this(new MapCollection(), configuration)
        {
            if (configuration is null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            IsDepthMapping = configuration.IsDepthMapping;
            AllowPropagationNullValues = configuration.AllowPropagationNullValues;
        }

        /// 映射配置。
        /// </summary>
        private MapConfiguration(MapCollection maps, IConfiguration configuration)
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
        public bool IsMatch(Type sourceType, Type destinationType) => profiles.Any(x => x.IsMatch(sourceType, destinationType)) || maps.Any(x => x.IsMatch(sourceType, destinationType));

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
        /// 映射。
        /// </summary>
        /// <param name="sourceExpression">源。</param>
        /// <param name="destinationType">目标。</param>
        /// <returns>映射表达式。</returns>
        /// <exception cref="InvalidCastException">无法转换。</exception>
        public Expression Map(Expression sourceExpression, Type destinationType)
        {
            var sourceType = sourceExpression.Type;

            var conversionType = destinationType.IsAbstract
                ? destinationType
                : sourceType;

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
                else if (destinationType == typeof(IEnumerable)
                    || destinationType == typeof(ICollection)
                    || destinationType == typeof(IList))
                {
                    conversionType = typeof(List<object>);
                }
            }

            if (conversionType.IsAbstract)
            {
                throw new InvalidCastException($"无法从源【{sourceType}】分析到目标【{destinationType}】的可实列化类型！");
            }

            if (sourceType.IsValueType || conversionType.IsValueType)
            {
                if (sourceType == conversionType)
                {
                    if (AllowPropagationNullValues || !conversionType.IsNullable())
                    {
                        return sourceExpression;
                    }

                    return IgnoreIfNull(sourceExpression);
                }

                if (sourceType.IsValueType && conversionType.IsValueType)
                {
                    if (AllowPropagationNullValues)
                    {
                        goto label_valueType;
                    }

                    if (sourceType.IsNullable() && conversionType.IsNullable())
                    {
                        sourceExpression = IgnoreIfNull(sourceExpression);
                    }
                }

                goto label_valueType;
            }
            else if (!IsDepthMapping && conversionType.IsAssignableFrom(sourceType))
            {
                if (AllowPropagationNullValues)
                {
                    return sourceExpression;
                }

                return IgnoreIfNull(sourceExpression);
            }
            else if (!AllowPropagationNullValues)
            {
                sourceExpression = IgnoreIfNull(sourceExpression);
            }

            if (sourceType == typeof(object))
            {
                return Convert(sourceExpression, conversionType);
            }

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

label_valueType:

            foreach (var map in maps)
            {
                if (map.IsMatch(sourceType, conversionType))
                {
                    return map.ToSolve(sourceExpression, sourceType, conversionType, this);
                }
            }

            throw new InvalidCastException($"无法从源【{sourceType}】源映射到目标【{destinationType}】的类型！");
        }

        private static Expression IgnoreIfNull(Expression node) => new IgnoreIfNullExpression(node);
    }
}
