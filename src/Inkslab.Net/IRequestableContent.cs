using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;

namespace Inkslab.Net
{
    /// <summary>
    /// 数据包请求能力。
    /// </summary>
    public interface IRequestableContent : IRequestable<string>, IDeserializeRequestable, IStreamRequestable
    {
        /// <summary>
        /// 条件都满足时，重试一次请求。
        /// </summary>
        /// <param name="whenStatus">条件。</param>
        /// <returns>状态验证的请求能力。</returns>
        IWhenRequestable When(Predicate<HttpStatusCode> whenStatus);
    }

    /// <summary>
    /// 具备编码能力的请求能力。
    /// </summary>
    public interface IRequestableEncoding : IRequestableContent
    {
        /// <summary>
        /// body中传输。
        /// </summary>
        /// <param name="body">body内容。</param>
        /// <param name="contentType">Content-Type类型。</param>
        /// <returns></returns>
        IRequestableContent Body(string body, string contentType);

        /// <summary>
        /// content-type = "application/json"。
        /// </summary>
        /// <param name="json">参数。</param>
        /// <returns></returns>
        IRequestableContent Json(string json);

        /// <summary>
        ///  content-type = "application/json"。
        /// </summary>
        /// <typeparam name="T">参数类型。</typeparam>
        /// <param name="param">参数。</param>
        /// <param name="namingType">命名规则。</param>
        /// <returns></returns>
        IRequestableContent Json<T>(T param, NamingType namingType = NamingType.Normal) where T : class;

        /// <summary>
        /// content-type = "application/xml";。
        /// </summary>
        /// <param name="xml">参数。</param>
        /// <returns></returns>
        IRequestableContent Xml(string xml);

        /// <summary>
        /// content-type = "application/xml";。
        /// </summary>
        /// <typeparam name="T">参数类型。</typeparam>
        /// <param name="param">参数。</param>
        /// <returns></returns>
        IRequestableContent Xml<T>(T param) where T : class;

        /// <summary>
        /// content-type = "multipart/form-data";。
        /// </summary>
        /// <param name="body">参数。</param>
        /// <returns></returns>
        IRequestableContent Form(MultipartFormDataContent body);

        /// <summary>
        /// content-type = "application/x-www-form-urlencoded";。
        /// </summary>
        /// <param name="body">参数。</param>
        /// <returns></returns>
        IRequestableContent Form(FormUrlEncodedContent body);

        /// <summary>
        /// content-type = "application/x-www-form-urlencoded";。
        /// </summary>
        /// <param name="body">参数。</param>
        /// <returns></returns>
        IRequestableContent Form<TBody>(TBody body) where TBody : IEnumerable<KeyValuePair<string, string>>;

        /// <summary>
        /// 当集合的任意值为“<see cref="System.IO.FileInfo"/>”类型或“IEnumerable<see cref="System.IO.FileInfo"/>>”类型时，将“content-type”设置为"multipart/form-data"，否则设置为"application/x-www-form-urlencoded"。
        /// </summary>
        /// <param name="body">参数。</param>
        /// <param name="dateFormatString">如果值是日期时，格式化风格。</param>
        /// <returns></returns>
        IRequestableContent Form<TBody>(TBody body, string dateFormatString = "yyyy-MM-dd HH:mm:ss.FFFFFFFK") where TBody : IEnumerable<KeyValuePair<string, object>>;

        /// <summary>
        /// 当对象任意属性值为“<see cref="System.IO.FileInfo"/>”类型或“IEnumerable<see cref="System.IO.FileInfo"/>>”类型时，将“content-type”设置为"multipart/form-data"，否则设置为"application/x-www-form-urlencoded"。
        /// </summary>
        /// <param name="body">参数。</param>
        /// <param name="namingType">命名规则。</param>
        /// <param name="dateFormatString">日期格式化。</param>
        /// <returns></returns>
        IRequestableContent Form(object body, NamingType namingType, string dateFormatString = "yyyy-MM-dd HH:mm:ss.FFFFFFFK");
    }
}
