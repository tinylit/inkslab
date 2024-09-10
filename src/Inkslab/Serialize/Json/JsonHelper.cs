using System;

namespace Inkslab.Serialize.Json
{
    /// <summary>
    /// JSON 助手。
    /// </summary>
    public static class JsonHelper
    {
        private static readonly IJsonHelper _jsonHelper;

        /// <summary>
        /// 静态构造函数。
        /// </summary>
        static JsonHelper() => _jsonHelper = SingletonPools.Singleton<IJsonHelper>();

        /// <summary> Json序列化。 </summary>
        /// <typeparam name="T">对象类型。</typeparam>
        /// <param name="jsonObj">对象。</param>
        /// <param name="namingType">命名规则。</param>
        /// <param name="indented">是否缩进。</param>
        /// <returns></returns>
        public static string ToJson<T>(T jsonObj, NamingType namingType = NamingType.Normal, bool indented = false)
            => _jsonHelper.ToJson(jsonObj, namingType, indented);

        /// <summary> Json序列化。 </summary>
        /// <param name="jsonObj">对象。</param>
        /// <param name="type">对象类型。</param>
        /// <param name="namingType">命名规则。</param>
        /// <param name="indented">是否缩进。</param>
        /// <returns></returns>
        public static string ToJson(object jsonObj, Type type, NamingType namingType = NamingType.Normal, bool indented = false)
            => _jsonHelper.ToJson(jsonObj, type, namingType, indented);

        /// <summary> Json反序列化。 </summary>
        /// <typeparam name="T">结果类型。</typeparam>
        /// <param name="json">JSON 字符串。</param>
        /// <param name="namingType">命名规则。</param>
        /// <returns></returns>
        public static T Json<T>(string json, NamingType namingType = NamingType.Normal)
            => _jsonHelper.Json<T>(json, namingType);

        /// <summary> 匿名对象反序列化。 </summary>
        /// <typeparam name="T">结果类型。</typeparam>
        /// <param name="json">JSON 字符串。</param>
        /// <param name="anonymousTypeObject">匿名对象类型。</param>
        /// <param name="namingType">命名规则。</param>
        /// <returns></returns>
#pragma warning disable IDE0060 // 删除未使用的参数
        public static T Json<T>(string json, T anonymousTypeObject, NamingType namingType = NamingType.Normal)
#pragma warning restore IDE0060 // 删除未使用的参数
            => Json<T>(json, namingType);

        /// <summary> Json反序列化。 </summary>
        /// <param name="json">JSON字符串。</param>
        /// <param name="type">结果类型。</param>
        /// <param name="namingType">命名规则。</param>
        /// <returns></returns>
        public static object Json(string json, Type type, NamingType namingType = NamingType.Normal)
            => _jsonHelper.Json(json, type, namingType);
    }
}
