using Inkslab.Net.Options;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Inkslab.Net
{
    public partial class RequestFactory
    {
        private class WhenRequestable : IWhenRequestable
        {
            private readonly RequestableString _requestable;
            private readonly Encoding _encoding;
            private readonly Predicate<HttpStatusCode> _whenStatus;

            public WhenRequestable(RequestableString requestable, Encoding encoding, Predicate<HttpStatusCode> whenStatus)
            {
                _requestable = requestable;
                _encoding = encoding;
                _whenStatus = whenStatus;
            }

            public IThenRequestable ThenAsync(Func<IRequestableBase, Task> thenAsync)
            {
                if (thenAsync is null)
                {
                    throw new ArgumentNullException(nameof(thenAsync));
                }

                return new ThenRequestable(_requestable, _encoding, _whenStatus, thenAsync);
            }
        }

        private class ThenRequestable : RequestableEncoding, IThenRequestable
        {
            private volatile bool initializedStatusCode;
            private readonly RequestableString _requestable;
            private readonly Predicate<HttpStatusCode> _whenStatus;
            private readonly Func<IRequestableBase, Task> _thenAsync;

            public ThenRequestable(RequestableString requestable, Encoding encoding, Predicate<HttpStatusCode> whenStatus, Func<IRequestableBase, Task> thenAsync) : base(encoding)
            {
                _requestable = requestable;
                _whenStatus = whenStatus;
                _thenAsync = thenAsync;
            }

            public override RequestOptions GetOptions(HttpMethod method, double timeout) => _requestable.GetOptions(method, timeout);

            public sealed override async Task<HttpResponseMessage> SendAsync(RequestOptions options, CancellationToken cancellationToken = default)
            {
                var httpMsg = await _requestable.SendAsync(options, cancellationToken);

                if (initializedStatusCode)
                {
                    return httpMsg;
                }

                if (_whenStatus(httpMsg.StatusCode))
                {
                    initializedStatusCode = true;

                    //? 重试前释放首个响应，避免连接/流泄漏。
                    httpMsg.Dispose();

                    var requestableRef = new RequestableBase(_requestable);

                    await _thenAsync(requestableRef);

                    //? 重建请求配置（含全新 Content），首个 options 的 Content 已随请求消息释放，不能复用。
                    var retryOptions = _requestable.GetOptions(options.Method, options.Timeout);

                    return await requestableRef.SendAsync(retryOptions, cancellationToken);
                }

                return httpMsg;
            }

            private class RequestableBase : IRequestableBase
            {
                private readonly RequestableString _requestable;
                private readonly QueryString<RequestableBase> _queryString;
                private readonly Dictionary<string, string> _headers = new Dictionary<string, string>();
                private readonly HashSet<string> _skipValidationHeaders = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                public RequestableBase(RequestableString requestable)
                {
                    _requestable = requestable;

                    _queryString = new QueryString<RequestableBase>(this, string.Empty);
                }

                public IRequestableBase AppendQueryString(string param) => _queryString.AppendQueryString(param);

                public IRequestableBase AppendQueryString(string name, string value) => _queryString.AppendQueryString(name, value);

                public IRequestableBase AppendQueryString(string name, DateTime value, string dateFormatString = "yyyy-MM-dd HH:mm:ss.FFFFFFFK") => _queryString.AppendQueryString(name, value, dateFormatString);

                public IRequestableBase AppendQueryString(string name, object value, string dateFormatString = "yyyy-MM-dd HH:mm:ss.FFFFFFFK") => _queryString.AppendQueryString(name, value, dateFormatString);

                public IRequestableBase AppendQueryString<TParam>(TParam param) where TParam : IEnumerable<KeyValuePair<string, object>> => _queryString.AppendQueryString(param);

                public IRequestableBase AppendQueryString<TParam>(TParam param, string dateFormatString) where TParam : IEnumerable<KeyValuePair<string, object>> => _queryString.AppendQueryString(param, dateFormatString);

                public IRequestableBase AppendQueryString<TParam>(TParam param, NamingType namingType = NamingType.SnakeCase, string dateFormatString = "yyyy-MM-dd HH:mm:ss.FFFFFFFK") where TParam : class => _queryString.AppendQueryString(param, namingType, dateFormatString);

                public IRequestableBase AssignHeader(string header, string value)
                {
                    if (header is null)
                    {
                        throw new ArgumentNullException(nameof(header));
                    }

                    _headers[header] = value;

                    return this;
                }

                public IRequestableBase AssignHeader(string header, string value, bool skipValidation)
                {
                    if (header is null)
                    {
                        throw new ArgumentNullException(nameof(header));
                    }

                    _headers[header] = value;

                    if (skipValidation)
                    {
                        _skipValidationHeaders.Add(header);
                    }
                    else
                    {
                        _skipValidationHeaders.Remove(header);
                    }

                    return this;
                }

                public IRequestableBase AssignHeaders<THeader>(THeader headers) where THeader : IEnumerable<KeyValuePair<string, string>>
                {
                    if (headers is null)
                    {
                        throw new ArgumentNullException(nameof(headers));
                    }

                    foreach (var kv in headers)
                    {
                        AssignHeader(kv.Key, kv.Value);
                    }

                    return this;
                }

                private string RequestUriRef(string requestUri)
                {
                    if (_queryString.Length == 0)
                    {
                        return requestUri;
                    }

                    int indexOf = requestUri.IndexOf('?');

                    var queryStrings = _queryString.ToString();

                    if (indexOf == -1)
                    {
                        return string.Concat(requestUri, "?", queryStrings);
                    }

                    var sb = new StringBuilder(requestUri.Length + queryStrings.Length);

                    sb.Append(requestUri, 0, indexOf + 1)
                        .Append(queryStrings);

                    var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                    foreach (var param in queryStrings
                                 .Split('&'))
                    {
                        keys.Add(param.Split('=')[0]);
                    }

                    foreach (var param in requestUri
                                 .Substring(indexOf + 1)
                                 .Split('&'))
                    {
                        if (keys.Contains(param.Split('=')[0]))
                        {
                            continue;
                        }

                        sb.Append('&')
                            .Append(param);
                    }

                    return sb.ToString();
                }

                public Task<HttpResponseMessage> SendAsync(RequestOptions options, CancellationToken cancellationToken = default)
                {
                    options.RequestUri = RequestUriRef(options.RequestUri);

                    if (_headers.Count > 0)
                    {
                        foreach (var header in _headers)
                        {
                            options.Headers[header.Key] = header.Value;
                        }
                    }

                    if (_skipValidationHeaders.Count > 0)
                    {
                        foreach (var header in _skipValidationHeaders)
                        {
                            options.SkipValidationHeaders.Add(header);
                        }
                    }

                    return _requestable.SendAsync(options, cancellationToken);
                }
            }
        }
    }
}
