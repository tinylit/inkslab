using Inkslab.Net.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Inkslab.Net
{
    public partial class RequestFactory
    {
        private abstract class Requestable<T> : IRequestable<T>
        {
            public Task<T> GetAsync(double timeout = 1000D, CancellationToken cancellationToken = default) => SendAsync(HttpMethod.Get, timeout, cancellationToken);

            public Task<T> HeadAsync(double timeout = 1000D, CancellationToken cancellationToken = default) => SendAsync(HttpMethod.Head, timeout, cancellationToken);

            public Task<T> PatchAsync(double timeout = 1000D, CancellationToken cancellationToken = default) => SendAsync(new HttpMethod("PATCH"), timeout, cancellationToken);

            public Task<T> PostAsync(double timeout = 1000D, CancellationToken cancellationToken = default) => SendAsync(HttpMethod.Post, timeout, cancellationToken);

            public Task<T> PutAsync(double timeout = 1000D, CancellationToken cancellationToken = default) => SendAsync(HttpMethod.Put, timeout, cancellationToken);

            public Task<T> DeleteAsync(double timeout = 1000D, CancellationToken cancellationToken = default) => SendAsync(HttpMethod.Delete, timeout, cancellationToken);

            public Task<T> SendAsync(string method, double timeout = 1000D, CancellationToken cancellationToken = default)
            {
                return method.ToUpper() switch
                {
                    "GET" => SendAsync(HttpMethod.Get, timeout, cancellationToken),
                    "POST" => SendAsync(HttpMethod.Post, timeout, cancellationToken),
                    "PUT" => SendAsync(HttpMethod.Put, timeout, cancellationToken),
                    "DELETE" => SendAsync(HttpMethod.Delete, timeout, cancellationToken),
                    "HEAD" => SendAsync(HttpMethod.Head, timeout, cancellationToken),
                    "OPTIONS" => SendAsync(HttpMethod.Options, timeout, cancellationToken),
                    "TRACE" => SendAsync(HttpMethod.Trace, timeout, cancellationToken),
                    _ => SendAsync(new HttpMethod(method.ToUpper()), timeout, cancellationToken),
                };
            }

            public abstract Task<T> SendAsync(HttpMethod method, double timeout = 1000D, CancellationToken cancellationToken = default);
        }

        private abstract class RequestableString : Requestable<string>, IStreamRequestable
        {
            public async Task<Stream> DownloadAsync(double timeout = 10000D, CancellationToken cancellationToken = default)
            {
                using var httpMsg = await PrimitiveSendAsync(HttpMethod.Get, timeout, cancellationToken);

                httpMsg.EnsureSuccessStatusCode();

#if NET6_0_OR_GREATER
                return await httpMsg.Content.ReadAsStreamAsync(cancellationToken);
#else
                return await httpMsg.Content.ReadAsStreamAsync();
#endif
            }

            public override async Task<string> SendAsync(HttpMethod method, double timeout = 1000D, CancellationToken cancellationToken = default)
            {
                using var httpMsg = await PrimitiveSendAsync(method, timeout, cancellationToken);

                httpMsg.EnsureSuccessStatusCode();

#if NET6_0_OR_GREATER
                return await httpMsg.Content.ReadAsStringAsync(cancellationToken);
#else
                return await httpMsg.Content.ReadAsStringAsync();
#endif
            }

            public Task<HttpResponseMessage> PrimitiveSendAsync(HttpMethod method, double timeout = 1000D, CancellationToken cancellationToken = default)
            {
                var options = GetOptions(method, timeout);

                return SendAsync(options, cancellationToken);
            }

            public abstract RequestOptions GetOptions(HttpMethod method, double timeout);

            public abstract Task<HttpResponseMessage> SendAsync(RequestOptions options, CancellationToken cancellationToken = default);
        }

        private class Requestable : RequestableEncoding, IRequestable, IRequestableBase
        {
            private readonly RequestFactory _factory;
            private readonly Dictionary<string, string> _headers;
            private readonly HashSet<string> _skipValidationHeaders;
            private readonly QueryString<Requestable> _queryString;
            private static readonly Encoding _encodingDefault = Encoding.UTF8;

            public Requestable(RequestFactory factory, string requestUri) : base(_encodingDefault)
            {
                _factory = factory;

                _headers = new Dictionary<string, string>();
                _skipValidationHeaders = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                _queryString = new QueryString<Requestable>(this, requestUri);
            }

            private Requestable(RequestFactory factory, Encoding encoding, QueryString<Requestable> queryString, Dictionary<string, string> headers, HashSet<string> skipValidationHeaders) : base(encoding)
            {
                _factory = factory;
                _headers = headers;
                _skipValidationHeaders = skipValidationHeaders;
                _queryString = queryString;
            }

            public IRequestable AppendQueryString(string param) => _queryString.AppendQueryString(param);

            public IRequestable AppendQueryString(string name, string value) => _queryString.AppendQueryString(name, value);

            public IRequestable AppendQueryString(string name, DateTime value, string dateFormatString = "yyyy-MM-dd HH:mm:ss.FFFFFFFK") => _queryString.AppendQueryString(name, value, dateFormatString);

            public IRequestable AppendQueryString(string name, object value, string dateFormatString = "yyyy-MM-dd HH:mm:ss.FFFFFFFK") => _queryString.AppendQueryString(name, value, dateFormatString);

            public IRequestable AppendQueryString<TParam>(TParam param) where TParam : IEnumerable<KeyValuePair<string, object>> => _queryString.AppendQueryString(param);

            public IRequestable AppendQueryString<TParam>(TParam param, string dateFormatString) where TParam : IEnumerable<KeyValuePair<string, object>> => _queryString.AppendQueryString(param, dateFormatString);

            public IRequestable AppendQueryString<TParam>(TParam param, NamingType namingType = NamingType.SnakeCase, string dateFormatString = "yyyy-MM-dd HH:mm:ss.FFFFFFFK") where TParam : class => _queryString.AppendQueryString(param, namingType, dateFormatString);

            public IRequestable AssignHeader(string header, string value)
            {
                if (header is null)
                {
                    throw new ArgumentNullException(nameof(header));
                }

                _headers[header] = value;

                return this;
            }

            public IRequestable AssignHeader(string header, string value, bool skipValidation)
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

            public IRequestable AssignHeaders<THeader>(THeader headers) where THeader : IEnumerable<KeyValuePair<string, string>>
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

            public override RequestOptions GetOptions(HttpMethod method, double timeout) => new RequestOptions(_queryString.ToString(), _headers, _skipValidationHeaders)
            {
                Method = method,
                Timeout = timeout,
            };

            public IRequestableEncoding UseEncoding(Encoding encoding)
            {
                if (encoding is null || Equals(_encodingDefault, encoding))
                {
                    return this;
                }

                return new Requestable(_factory, encoding, _queryString, _headers, _skipValidationHeaders);
            }

            IRequestableBase IRequestableBase<IRequestableBase>.AssignHeader(string header, string value)
            {
                AssignHeader(header, value);

                return this;
            }

            IRequestableBase IRequestableBase<IRequestableBase>.AssignHeader(string header, string value, bool skipValidation)
            {
                AssignHeader(header, value, skipValidation);

                return this;
            }

            IRequestableBase IRequestableBase<IRequestableBase>.AssignHeaders<THeader>(THeader headers)
            {
                AssignHeaders(headers);

                return this;
            }

            IRequestableBase IRequestableBase<IRequestableBase>.AppendQueryString(string param)
            {
                AppendQueryString(param);

                return this;
            }

            IRequestableBase IRequestableBase<IRequestableBase>.AppendQueryString(string name, string value)
            {
                AppendQueryString(name, value);

                return this;
            }

            IRequestableBase IRequestableBase<IRequestableBase>.AppendQueryString(string name, DateTime value, string dateFormatString)
            {
                AppendQueryString(name, value, dateFormatString);

                return this;
            }

            IRequestableBase IRequestableBase<IRequestableBase>.AppendQueryString(string name, object value, string dateFormatString)
            {
                AppendQueryString(name, value, dateFormatString);

                return this;
            }

            IRequestableBase IRequestableBase<IRequestableBase>.AppendQueryString<TParam>(TParam param)
            {
                AppendQueryString(param);

                return this;
            }

            IRequestableBase IRequestableBase<IRequestableBase>.AppendQueryString<TParam>(TParam param, string dateFormatString)
            {
                AppendQueryString(param, dateFormatString);

                return this;
            }

            IRequestableBase IRequestableBase<IRequestableBase>.AppendQueryString<TParam>(TParam param, NamingType namingType, string dateFormatString)
            {
                AppendQueryString(param, namingType, dateFormatString);

                return this;
            }

            public override Task<HttpResponseMessage> SendAsync(RequestOptions options, CancellationToken cancellationToken = default) => _factory.SendAsync(options, cancellationToken);
        }

        private class RequestableContent : RequestableString, IRequestableContent
        {
            private readonly RequestableString _requestable;
            private readonly Encoding _encoding;
            private readonly IToContent _content;

            public RequestableContent(RequestableString requestable, Encoding encoding, IToContent content)
            {
                _requestable = requestable;
                _encoding = encoding;
                _content = content;
            }

            public IJsonDeserializeRequestable<T> JsonCast<T>(NamingType namingType = NamingType.Normal) where T : class => new JsonDeserializeRequestable<T>(this, namingType);

            public IXmlDeserializeRequestable<T> XmlCast<T>() where T : class => new XmlDeserializeRequestable<T>(this, _encoding);

            public IJsonDeserializeRequestable<T> JsonCast<T>(T anonymousTypeObject, NamingType namingType = NamingType.Normal) where T : class => JsonCast<T>(namingType);

            public IXmlDeserializeRequestable<T> XmlCast<T>(T anonymousTypeObject) where T : class => XmlCast<T>();

            public override RequestOptions GetOptions(HttpMethod method, double timeout)
            {
                var options = _requestable.GetOptions(method, timeout);

                options.Content = _content.Content;

                return options;
            }

            public sealed override Task<HttpResponseMessage> SendAsync(RequestOptions options, CancellationToken cancellationToken = default) => _requestable.SendAsync(options, cancellationToken);

            public IWhenRequestable When(Predicate<HttpStatusCode> whenStatus)
            {
                if (whenStatus is null)
                {
                    throw new ArgumentNullException(nameof(whenStatus));
                }

                return new WhenRequestable(this, _encoding, whenStatus);
            }

            public ICustomDeserializeRequestable<T> CustomCast<T>(Func<HttpResponseMessage, CancellationToken, Task<T>> customFactory) where T : class
            {
                if (customFactory is null)
                {
                    throw new ArgumentNullException(nameof(customFactory));
                }

                return new CustomDeserializeRequestable<T>(this, customFactory);
            }

            public ICustomDeserializeRequestable<T> CustomCast<T>(Func<string, T> customFactory) where T : class
            {
                if (customFactory is null)
                {
                    throw new ArgumentNullException(nameof(customFactory));
                }

                return new CustomByStringDeserializeRequestable<T>(this, customFactory);
            }
        }
    }
}
