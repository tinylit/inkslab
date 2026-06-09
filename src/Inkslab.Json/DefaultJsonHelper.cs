using Inkslab.Serialize.Json;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Inkslab.Json
{
    /// <summary>
    /// 牛顿 JSON 序列化帮助类。
    /// </summary>
    public class DefaultJsonHelper : IJsonHelper
    {
        /// <summary>
        ///JSON序列化解析协议。
        /// </summary>
        private class JsonContractResolver : DefaultContractResolver
        {
            private readonly NamingType _camelCase;
            /// <summary>
            /// 构造定义命名解析风格。
            /// </summary>
            /// <param name="namingCase">命名规则。</param>
            public JsonContractResolver(NamingType namingCase) => _camelCase = namingCase;

            /// <summary>
            /// 属性名解析。
            /// </summary>
            /// <param name="propertyName">属性名称。</param>
            /// <returns></returns>
            protected override string ResolvePropertyName(string propertyName)
                => _camelCase == NamingType.Normal
                    ? base.ResolvePropertyName(propertyName)
                    : propertyName.ToNamingCase(_camelCase);

            /// <summary>
            /// 属性。
            /// </summary>
            protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
            {
                JsonProperty property = base.CreateProperty(member, memberSerialization);

                if (property.Ignored)
                {

                }
                else if (member.IsIgnore())
                {
                    property.Ignored = true;
                }

                var nameAttr = member.GetCustomAttribute<Annotations.JsonPropertyAttribute>();

                if (nameAttr is null)
                {

                }
                else
                {
                    property.PropertyName = nameAttr.Name;
                }

                return property;
            }
        }

        private static readonly DefaultContractResolver _contractResolver;

        private static readonly Dictionary<NamingType, IContractResolver> _resolvers;

        static DefaultJsonHelper()
        {
            var namingTypes = Enum.GetValues(typeof(NamingType));

            _resolvers = new Dictionary<NamingType, IContractResolver>(namingTypes.Length);

            foreach (NamingType namingType in namingTypes)
            {
                _resolvers.Add(namingType, new JsonContractResolver(namingType));
            }

            _contractResolver = new DefaultContractResolver();
        }

        private readonly JsonSerializerSettings _settings;

        private readonly Dictionary<int, JsonSerializerSettings> _settingsCache;

        /// <summary>
        /// 构造函数。
        /// </summary>
        public DefaultJsonHelper() : this(new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore
        })
        {
        }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="settings">配置。</param>
        public DefaultJsonHelper(JsonSerializerSettings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _settingsCache = BuildSettingsCache(settings);
        }

        /// <summary>
        /// 预构建各 <see cref="NamingType"/> 与缩进组合的不可变序列化配置（克隆自基准配置）。
        /// 避免在并发调用中改写共享的 <see cref="JsonSerializerSettings"/>。
        /// </summary>
        /// <param name="settings">基准配置。</param>
        /// <returns>只读的配置缓存。</returns>
        private static Dictionary<int, JsonSerializerSettings> BuildSettingsCache(JsonSerializerSettings settings)
        {
            var namingTypes = Enum.GetValues(typeof(NamingType));

            var cache = new Dictionary<int, JsonSerializerSettings>(namingTypes.Length * 2);

            foreach (NamingType namingType in namingTypes)
            {
                var resolver = _resolvers.TryGetValue(namingType, out var value)
                    ? value
                    : _contractResolver;

                foreach (var indented in new[] { false, true })
                {
                    cache[CacheKey(namingType, indented)] = new JsonSerializerSettings(settings)
                    {
                        ContractResolver = resolver,
                        Formatting = indented ? Formatting.Indented : Formatting.None
                    };
                }
            }

            return cache;
        }

        /// <summary>
        /// 由命名方式与缩进组合出缓存键（避免 net461 缺失 ValueTuple）。
        /// </summary>
        private static int CacheKey(NamingType namingType, bool indented) => ((int)namingType << 1) | (indented ? 1 : 0);

        /// <summary>
        /// 获取指定命名方式与缩进对应的序列化配置（只读，多线程安全）。
        /// </summary>
        /// <param name="namingType">命名方式。</param>
        /// <param name="indented">是否缩进。</param>
        /// <returns></returns>
        private JsonSerializerSettings LoadSetting(NamingType namingType, bool indented = false)
            => _settingsCache.TryGetValue(CacheKey(namingType, indented), out var settings)
                ? settings
                : _settings;

        /// <inheritdoc/>
        public T Json<T>(string json, NamingType namingType = NamingType.Normal)
        {
            return JsonConvert.DeserializeObject<T>(json, LoadSetting(namingType));
        }

        /// <inheritdoc/>
        public object Json(string json, Type type, NamingType namingType = NamingType.Normal)
        {
            return JsonConvert.DeserializeObject(json, type, LoadSetting(namingType));
        }

        /// <inheritdoc/>
        public string ToJson<T>(T jsonObj, NamingType namingType = NamingType.Normal, bool indented = false)
        {
            return JsonConvert.SerializeObject(jsonObj, LoadSetting(namingType, indented));
        }

        /// <inheritdoc/>
        public string ToJson(object jsonObj, Type type, NamingType namingType = NamingType.Normal, bool indented = false)
        {
            return JsonConvert.SerializeObject(jsonObj, type, LoadSetting(namingType, indented));
        }
    }
}