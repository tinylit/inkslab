using System.Collections.Generic;
using System.Net.Http;

namespace Inkslab.Net.Options
{
    /// <summary>
    /// 请求配置。
    /// </summary>
    public class RequestOptions
    {
        /// <summary>
        /// 请求配置。
        /// </summary>
        /// <param name="requestUri">请求地址。</param>
        /// <param name="headers">请求头。</param>
        /// <param name="skipValidationHeaders">跳过 .NET HttpHeaders 格式验证的请求头名集合。</param>
        public RequestOptions(string requestUri, Dictionary<string, string> headers, HashSet<string> skipValidationHeaders = null)
        {
            RequestUri = requestUri;
            Headers = headers ?? new Dictionary<string, string>();
            SkipValidationHeaders = skipValidationHeaders ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 请求地址。
        /// </summary>
        public string RequestUri { internal set; get; }

        /// <summary>
        /// 请求头。
        /// </summary>
        public Dictionary<string, string> Headers { get; }

        /// <summary>
        /// 跳过 .NET HttpHeaders 格式验证的请求头名集合（使用 TryAddWithoutValidation）。
        /// </summary>
        public HashSet<string> SkipValidationHeaders { get; }

        /// <summary>
        /// 获取或设置 HTTP 请求消息使用的 HTTP 方法。
        /// </summary>
        public HttpMethod Method { get; set; }

        /// <summary>
        /// 超时时间，单位：毫秒。
        /// </summary>
        public double Timeout { get; set; }

        /// <summary>
        /// 请求内容。
        /// </summary>
        public HttpContent Content { get; set; }
    }
}
