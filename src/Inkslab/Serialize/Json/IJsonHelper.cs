using Inkslab.Annotations;
using System;

namespace Inkslab.Serialize.Json
{
    /// <summary>
    /// JSON序列化。
    /// </summary>
    [Ignore]
    public interface IJsonHelper
    {
        /// <summary> Json序列化。 </summary>
        /// <typeparam name="T">数据类型。</typeparam>
        /// <param name="jsonObj">对象。</param>
        /// <param name="namingType">命名规则。</param>
        /// <param name="indented">是否缩进。</param>
        /// <returns></returns>
        string ToJson<T>(T jsonObj, NamingType namingType = NamingType.Normal, bool indented = false);

        /// <summary> Json序列化。 </summary>
        /// <param name="jsonObj">对象。</param>
        /// <param name="type">对象类型。</param>
        /// <param name="namingType">命名规则。</param>
        /// <param name="indented">是否缩进。</param>
        /// <returns></returns>
        string ToJson(object jsonObj, Type type, NamingType namingType = NamingType.Normal, bool indented = false);

        /// <summary> Json反序列化。 </summary>
        /// <typeparam name="T">结果类型。</typeparam>
        /// <param name="json">JSON字符串。</param>
        /// <param name="namingType">命名规则。</param>
        /// <returns></returns>
        T Json<T>(string json, NamingType namingType = NamingType.Normal);

        /// <summary> Json反序列化。 </summary>
        /// <param name="json">JSON字符串。</param>
        /// <param name="type">结果类型。</param>
        /// <param name="namingType">命名规则。</param>
        /// <returns></returns>
        object Json(string json, Type type, NamingType namingType = NamingType.Normal);
    }
}
