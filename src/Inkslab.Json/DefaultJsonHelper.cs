using Newtonsoft.Json.Serialization;
using Newtonsoft.Json;
using System;
using Inkslab.Serialize.Json;
using System.Collections.Generic;
using Inkslab.Annotations;
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
            private readonly NamingType camelCase;
            /// <summary>
            /// 构造定义命名解析风格。
            /// </summary>
            /// <param name="namingCase">命名规则。</param>
            public JsonContractResolver(NamingType namingCase) => camelCase = namingCase;

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

                var nameAtti = member.GetCustomAttribute<JsonElementAttribute>();

                if (nameAtti is null)
                {
                    if (camelCase == NamingType.Normal)
                    {

                    }
                    else
                    {
                        property.PropertyName = member.Name.ToNamingCase(camelCase);
                    }
                }
                else
                {
                    property.PropertyName = nameAtti.Name;
                }

                return property;
            }
        }


        private static readonly Dictionary<NamingType, IContractResolver> resolvers;

        static DefaultJsonHelper()
        {
            var namingTypes = Enum.GetValues(typeof(NamingType));

            resolvers = new Dictionary<NamingType, IContractResolver>(namingTypes.Length);

            foreach (NamingType namingType in namingTypes)
            {
                resolvers.Add(namingType, new JsonContractResolver(namingType));
            }
        }

        private readonly JsonSerializerSettings settings;

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
        public DefaultJsonHelper(JsonSerializerSettings settings) => this.settings = settings ?? throw new ArgumentNullException(nameof(settings));

        /// <summary>
        /// JSON序列化设置。
        /// </summary>
        /// <param name="settings">配置。</param>
        /// <param name="namingType">命名方式。</param>
        /// <param name="indented">是否缩进。</param>
        /// <returns></returns>
        private static JsonSerializerSettings LoadSetting(JsonSerializerSettings settings, NamingType namingType, bool indented = false)
        {
            if (resolvers.TryGetValue(namingType, out var resolver))
            {
                settings.ContractResolver = resolver;
            }

            if (indented)
            {
                settings.Formatting = Formatting.Indented;
            }

            return settings;
        }

        /// <summary>
        /// 将JSON反序列化为对象。
        /// </summary>
        /// <typeparam name="T">结果类型。</typeparam>
        /// <param name="json">JSON字符串。</param>
        /// <param name="namingType">命名规则。</param>
        /// <returns></returns>
        public T Json<T>(string json, NamingType namingType = NamingType.Normal)
        {
            return JsonConvert.DeserializeObject<T>(json, LoadSetting(settings, namingType));
        }

        /// <summary>
        /// 对象序列化为JSON。
        /// </summary>
        /// <typeparam name="T">数据类型。</typeparam>
        /// <param name="jsonObj">对象。</param>
        /// <param name="namingType">命名规则。</param>
        /// <param name="indented">是否缩进。</param>
        /// <returns></returns>
        public string ToJson<T>(T jsonObj, NamingType namingType = NamingType.Normal, bool indented = false)
        {
            return JsonConvert.SerializeObject(jsonObj, LoadSetting(settings, namingType, indented));
        }
    }
}