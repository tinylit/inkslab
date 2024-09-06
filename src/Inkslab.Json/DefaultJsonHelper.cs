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
        public DefaultJsonHelper(JsonSerializerSettings settings) => _settings = settings ?? throw new ArgumentNullException(nameof(settings));

        /// <summary>
        /// JSON序列化设置。
        /// </summary>
        /// <param name="settings">配置。</param>
        /// <param name="namingType">命名方式。</param>
        /// <param name="indented">是否缩进。</param>
        /// <returns></returns>
        private static JsonSerializerSettings LoadSetting(JsonSerializerSettings settings, NamingType namingType, bool indented = false)
        {
            settings.ContractResolver = _resolvers.TryGetValue(namingType, out var resolver)
                ? resolver
                : _contractResolver;

            settings.Formatting = indented
                    ? Formatting.Indented
                    : Formatting.None;

            return settings;
        }

        /// <inheritdoc/>
        public T Json<T>(string json, NamingType namingType = NamingType.Normal)
        {
            return JsonConvert.DeserializeObject<T>(json, LoadSetting(_settings, namingType));
        }

        /// <inheritdoc/>
        public object Json(string json, Type type, NamingType namingType = NamingType.Normal)
        {
            return JsonConvert.DeserializeObject(json, type, LoadSetting(_settings, namingType));
        }

        /// <inheritdoc/>
        public string ToJson<T>(T jsonObj, NamingType namingType = NamingType.Normal, bool indented = false)
        {
            return JsonConvert.SerializeObject(jsonObj, LoadSetting(_settings, namingType, indented));
        }

        /// <inheritdoc/>
        public string ToJson(object jsonObj, Type type, NamingType namingType = NamingType.Normal, bool indented = false)
        {
            return JsonConvert.SerializeObject(jsonObj, type, LoadSetting(_settings, namingType, indented));
        }
    }
}