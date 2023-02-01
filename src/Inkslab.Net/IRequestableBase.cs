using System;
using System.Collections.Generic;

namespace Inkslab.Net
{
    /// <summary>
    /// 基础请求能力。
    /// </summary>
    public interface IRequestableBase : IRequestableBase<IRequestableBase>
    {

    }

    /// <summary>
    /// 基础请求能力。
    /// </summary>
    public interface IRequestableBase<TRequestable>
    {
        /// <summary>
        /// 指定包含与请求或响应相关联的协议头。
        /// </summary>
        /// <param name="header">协议头。</param>
        /// <param name="value">内容。</param>
        /// <returns>请求能力。</returns>
        TRequestable AssignHeader(string header, string value);

        /// <summary>
        /// 指定包含与请求或响应相关联的协议头。
        /// </summary>
        /// <typeparam name="THeader">请求头约束。</typeparam>
        /// <param name="headers">协议头。</param>
        /// <returns>请求能力。</returns>
        TRequestable AssignHeaders<THeader>(THeader headers) where THeader : IEnumerable<KeyValuePair<string, string>>;

        /// <summary>
        /// 请求参数。
        /// </summary>
        /// <param name="param">参数。</param>
        /// <returns>请求能力。</returns>
        TRequestable AppendQueryString(string param);

        /// <summary>
        /// 请求参数。?id=1&amp;name="yep"。
        /// </summary>
        /// <param name="name">参数名称。</param>
        /// <param name="value">参数值。</param>
        /// <returns>请求能力。</returns>
        TRequestable AppendQueryString(string name, string value);

        /// <summary>
        /// 请求参数。例如：?id=1&amp;name="yep"。
        /// </summary>
        /// <param name="name">参数名称。</param>
        /// <param name="value">参数值。</param>
        /// <param name="dateFormatString">日期格式化。</param>
        /// <returns>请求能力。</returns>
        TRequestable AppendQueryString(string name, DateTime value, string dateFormatString = "yyyy-MM-dd HH:mm:ss.FFFFFFFK");

        /// <summary>
        /// 请求参数。?id=1&amp;name="yep"。
        /// </summary>
        /// <param name="name">参数名称。</param>
        /// <param name="value">参数值。</param>
        /// <param name="dateFormatString">如果值是日期时，格式化风格。</param>
        /// <returns>请求能力。</returns>
        TRequestable AppendQueryString(string name, object value, string dateFormatString = "yyyy-MM-dd HH:mm:ss.FFFFFFFK");

        /// <summary>
        /// 请求参数。?id=1&amp;name="yep"。
        /// </summary>
        /// <typeparam name="TParam">对象类型。</typeparam>
        /// <param name="param">参数。</param>
        /// <remarks>日期类型，默认转字符串格式为：yyyy-MM-dd HH:mm:ss.FFFFFFFK</remarks>
        /// <returns>请求能力。</returns>
        TRequestable AppendQueryString<TParam>(TParam param) where TParam : IEnumerable<KeyValuePair<string, object>>;

        /// <summary>
        /// 请求参数。?id=1&amp;name="yep"。
        /// </summary>
        /// <typeparam name="TParam">对象类型。</typeparam>
        /// <param name="param">参数。</param>
        /// <param name="dateFormatString">日期格式化。</param>
        /// <returns>请求能力。</returns>
        TRequestable AppendQueryString<TParam>(TParam param, string dateFormatString) where TParam : IEnumerable<KeyValuePair<string, object>>;

        /// <summary>
        /// 请求参数。?id=1&amp;name="yep"。
        /// </summary>
        /// <typeparam name="TParam">对象类型。</typeparam>
        /// <param name="param">参数对象（反射对象公共可读属性名称和属性值）。</param>
        /// <param name="dateFormatString">日期格式化。</param>
        /// <param name="namingType">命名规范。</param>
        /// <returns>请求能力。</returns>
        TRequestable AppendQueryString<TParam>(TParam param, NamingType namingType = NamingType.UrlCase, string dateFormatString = "yyyy-MM-dd HH:mm:ss.FFFFFFFK") where TParam : class;
    }
}
