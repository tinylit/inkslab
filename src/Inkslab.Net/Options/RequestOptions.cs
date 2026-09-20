using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;

namespace Inkslab.Net.Options
{
    /// <summary>
    /// 请求配置。
    /// </summary>
    public class RequestOptions
    {
        private HttpContent _content;
        private bool _contentCreated;
        private bool _contentTransferred;
        private CancellationToken _contentCancellation;
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
        /// 当前发送的请求内容；首次读取时按需创建，同次发送内保持同一实例。
        /// </summary>
        public HttpContent Content
        {
            get
            {
                if (!_contentCreated)
                {
                    _contentCancellation.ThrowIfCancellationRequested();
                    _content = CreateContent?.Invoke();
                    _contentCreated = true;
                }
                return _content;
            }
            set { _content = value; _contentCreated = true; }
        }

        /// <summary>选择收到响应头或全部正文时完成；普通发送默认缓冲正文。</summary>
        public HttpCompletionOption CompletionOption { get; set; } = HttpCompletionOption.ResponseContentRead;

        internal Func<HttpContent> CreateContent { get; set; }
        internal bool CanReplay { get; set; } = true;
        internal HashSet<object> ExecutedStrategies { get; set; } = new HashSet<object>();

        internal RequestOptions CreateAttempt(CancellationToken cancellationToken)
        {
            return new RequestOptions(RequestUri, Headers, SkipValidationHeaders)
            {
                Method = Method,
                Timeout = Timeout,
                CompletionOption = CompletionOption,
                CanReplay = CanReplay,
                ExecutedStrategies = ExecutedStrategies,
                CreateContent = CreateContent,
                _content = CreateContent == null ? _content : null,
                _contentCreated = CreateContent == null && _contentCreated,
                _contentCancellation = cancellationToken
            };
        }

        internal HttpContent TakeContent()
        {
            var content = Content;
            _contentTransferred = true;
            return content;
        }

        internal void DisposeUntransferredContent()
        {
            if (!_contentTransferred) { _content?.Dispose(); }
        }
    }
}
