using Inkslab.Collections;
using Inkslab.Net.Options;
using Inkslab.Serialize.Json;
using Inkslab.Serialize.Xml;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Xml;

namespace Inkslab.Net
{
    using static Expression;

    /// <summary>
    /// 请求初始化。
    /// </summary>
    public class RequestInitialize : IRequestInitialize
    {
        /// <inheritdoc/>
        public void Initialize(IRequestableBase requestable)
        {
        }
    }

    /// <summary>
    /// 请求工厂。
    /// </summary>
    public class RequestFactory : IRequestFactory
    {
        private static readonly RequestFactory factory = new RequestFactory();

        private static readonly Type kvType = typeof(KeyValuePair<string, object>);
        private static readonly ConstructorInfo kvCtor = kvType.GetConstructor(new Type[] { typeof(string), typeof(object) });

        private static readonly Type listKvType = typeof(List<KeyValuePair<string, object>>);
        private static readonly ConstructorInfo listKvCtor = listKvType.GetConstructor(new Type[] { typeof(int) });
        private static readonly MethodInfo listKvAddFn = listKvType.GetMethod("Add", new Type[] { kvType });

        private static readonly Type dateType = typeof(DateTime);
        private static readonly MethodInfo dateToStringFn = dateType.GetMethod("ToString", new Type[] { typeof(string) });

#if NET_Traditional
        private static readonly LFU<double, HttpClient> clients = new LFU<double, HttpClient>(100, timeout => new HttpClient
        {
            Timeout = TimeSpan.FromMilliseconds(timeout)
        });
#else
        private static readonly LFU<double, HttpClient> clients = new LFU<double, HttpClient>(100, timeout => new HttpClient(new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        })
        {
            Timeout = TimeSpan.FromMilliseconds(timeout)
        });
#endif
        private static readonly ConcurrentDictionary<Type, Func<object, string, List<KeyValuePair<string, object>>>> cachings = new ConcurrentDictionary<Type, Func<object, string, List<KeyValuePair<string, object>>>>();

        private static readonly Dictionary<string, MediaTypeHeaderValue> mediaTypes = new Dictionary<string, MediaTypeHeaderValue>
        {
            [".apk"] = new MediaTypeHeaderValue("application/vnd.android.package-archive"),
            [".avi"] = new MediaTypeHeaderValue("video/x-msvideo"),
            [".buffer"] = new MediaTypeHeaderValue("application/octet-stream"),
            [".cer"] = new MediaTypeHeaderValue("application/pkix-cert"),
            [".chm"] = new MediaTypeHeaderValue("application/vnd.ms-htmlhelp"),
            [".conf"] = new MediaTypeHeaderValue("text/plain"),
            [".cpp"] = new MediaTypeHeaderValue("text/x-c"),
            [".crt"] = new MediaTypeHeaderValue("application/x-x509-ca-cert"),
            [".css"] = new MediaTypeHeaderValue("text/css"),
            [".csv"] = new MediaTypeHeaderValue("text/csv"),
            [".doc"] = new MediaTypeHeaderValue("application/msword"),
            [".docx"] = new MediaTypeHeaderValue("application/vnd.openxmlformats-officedocument.wordprocessingml.document"),
            [".exe"] = new MediaTypeHeaderValue("application/x-msdownload"),
            [".flac"] = new MediaTypeHeaderValue("audio/x-flac"),
            [".flv"] = new MediaTypeHeaderValue("video/x-flv"),
            [".gif"] = new MediaTypeHeaderValue("image/gif"),
            [".h263"] = new MediaTypeHeaderValue("video/h263"),
            [".h264"] = new MediaTypeHeaderValue("video/h264"),
            [".htm"] = new MediaTypeHeaderValue("text/html"),
            [".html"] = new MediaTypeHeaderValue("text/html"),
            [".ico"] = new MediaTypeHeaderValue("image/x-icon"),
            [".ini"] = new MediaTypeHeaderValue("text/plain"),
            [".ink"] = new MediaTypeHeaderValue("application/inkml+xml"),
            [".iso"] = new MediaTypeHeaderValue("application/x-iso9660-image"),
            [".jar"] = new MediaTypeHeaderValue("application/java-archive"),
            [".java"] = new MediaTypeHeaderValue("text/x-java-source"),
            [".jpeg"] = new MediaTypeHeaderValue("image/jpeg"),
            [".jpg"] = new MediaTypeHeaderValue("image/jpeg"),
            [".js"] = new MediaTypeHeaderValue("application/javascript"),
            [".json"] = new MediaTypeHeaderValue("application/json"),
            [".json5"] = new MediaTypeHeaderValue("application/json5"),
            [".jsx"] = new MediaTypeHeaderValue("text/jsx"),
            [".list"] = new MediaTypeHeaderValue("text/plain"),
            [".lnk"] = new MediaTypeHeaderValue("application/x-ms-shortcut"),
            [".log"] = new MediaTypeHeaderValue("text/plain"),
            [".m3u8"] = new MediaTypeHeaderValue("application/vnd.apple.mpegurl"),
            [".manifest"] = new MediaTypeHeaderValue("text/cache-manifest"),
            [".map"] = new MediaTypeHeaderValue("application/json"),
            [".markdown"] = new MediaTypeHeaderValue("text/x-markdown"),
            [".md"] = new MediaTypeHeaderValue("text/x-markdown"),
            [".mov"] = new MediaTypeHeaderValue("video/quicktime"),
            [".mp3"] = new MediaTypeHeaderValue("audio/mpeg"),
            [".mp4"] = new MediaTypeHeaderValue("video/mp4"),
            [".mpeg"] = new MediaTypeHeaderValue("video/mpeg"),
            [".mpg"] = new MediaTypeHeaderValue("video/mpeg"),
            [".msi"] = new MediaTypeHeaderValue("application/x-msdownload"),
            [".ogg"] = new MediaTypeHeaderValue("audio/ogg"),
            [".ogv"] = new MediaTypeHeaderValue("video/ogg"),
            [".otf"] = new MediaTypeHeaderValue("font/opentype"),
            [".pdf"] = new MediaTypeHeaderValue("application/pdf"),
            [".png"] = new MediaTypeHeaderValue("image/png"),
            [".ppt"] = new MediaTypeHeaderValue("application/vnd.ms-powerpoint"),
            [".pptx"] = new MediaTypeHeaderValue("application/vnd.openxmlformats-officedocument.presentationml.presentation"),
            [".psd"] = new MediaTypeHeaderValue("image/vnd.adobe.photoshop"),
            [".rar"] = new MediaTypeHeaderValue("application/x-rar-compressed"),
            [".rm"] = new MediaTypeHeaderValue("application/vnd.rn-realmedia"),
            [".rmvb"] = new MediaTypeHeaderValue("application/vnd.rn-realmedia-vbr"),
            [".roff"] = new MediaTypeHeaderValue("text/troff"),
            [".sass"] = new MediaTypeHeaderValue("text/x-sass"),
            [".scss"] = new MediaTypeHeaderValue("text/x-scss"),
            [".sh"] = new MediaTypeHeaderValue("application/x-sh"),
            [".sql"] = new MediaTypeHeaderValue("application/x-sql"),
            [".svg"] = new MediaTypeHeaderValue("image/svg+xml"),
            [".swf"] = new MediaTypeHeaderValue("application/x-shockwave-flash"),
            [".tar"] = new MediaTypeHeaderValue("application/x-tar"),
            [".text"] = new MediaTypeHeaderValue("text/plain"),
            [".torrent"] = new MediaTypeHeaderValue("application/x-bittorrent"),
            [".ttf"] = new MediaTypeHeaderValue("application/x-font-ttf"),
            [".txt"] = new MediaTypeHeaderValue("text/plain"),
            [".wav"] = new MediaTypeHeaderValue("audio/x-wav"),
            [".webm"] = new MediaTypeHeaderValue("video/webm"),
            [".wm"] = new MediaTypeHeaderValue("video/x-ms-wm"),
            [".wma"] = new MediaTypeHeaderValue("audio/x-ms-wma"),
            [".wmx"] = new MediaTypeHeaderValue("video/x-ms-wmx"),
            [".woff"] = new MediaTypeHeaderValue("application/font-woff"),
            [".woff2"] = new MediaTypeHeaderValue("application/font-woff2"),
            [".wps"] = new MediaTypeHeaderValue("application/vnd.ms-works"),
            [".xhtml"] = new MediaTypeHeaderValue("application/xhtml+xml"),
            [".xls"] = new MediaTypeHeaderValue("application/vnd.ms-excel"),
            [".xlsx"] = new MediaTypeHeaderValue("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"),
            [".xml"] = new MediaTypeHeaderValue("application/xml"),
            [".xz"] = new MediaTypeHeaderValue("application/x-xz"),
            [".yaml"] = new MediaTypeHeaderValue("text/yaml"),
            [".yml"] = new MediaTypeHeaderValue("text/yaml"),
            [".zip"] = new MediaTypeHeaderValue("application/zip")
        };

        private readonly IRequestInitialize initialize;

        private static Func<object, string, List<KeyValuePair<string, object>>> MakeTypeResults(Type type)
        {
            var objectType = typeof(object);

            var objectExp = Parameter(objectType, "param");
            var dateFormatStringExp = Parameter(typeof(string), "dateFormatString");
            var variableExp = Variable(type, "variable");
            var dictionaryExp = Variable(listKvType, "dictionary");

            var propertyInfos = Array.FindAll(type.GetProperties(), x => x.CanRead);

            var expressions = new List<Expression>
            {
                Assign(variableExp, Convert(objectExp, type)),
                Assign(dictionaryExp, New(listKvCtor, Constant(propertyInfos.Length)))
            };

            foreach (var propertyInfo in propertyInfos)
            {
                Expression valueExp;

                Expression propertyExp = Property(variableExp, propertyInfo);

                var propertyType = propertyInfo.PropertyType;

                bool isNullable = propertyType.IsNullable();

                if (isNullable)
                {
                    valueExp = Property(propertyExp, "Value");

                    propertyType = Nullable.GetUnderlyingType(propertyType)!;
                }
                else
                {
                    valueExp = propertyExp;
                }

                if (propertyType == dateType)
                {
                    valueExp = Call(valueExp, dateToStringFn, dateFormatStringExp);
                }
                else if (propertyType.IsValueType)
                {
                    if (propertyType.IsEnum)
                    {
                        propertyType = Enum.GetUnderlyingType(propertyType);

                        valueExp = Convert(valueExp, propertyType);
                    }

                    var toStringFn = propertyType.GetMethod("ToString", Type.EmptyTypes)!;

                    valueExp = Call(valueExp, toStringFn);
                }

                var bodyCallExp = Call(dictionaryExp, listKvAddFn, New(kvCtor, Constant(propertyInfo.Name), valueExp));

                if (isNullable)
                {
                    expressions.Add(IfThen(Property(propertyExp, "HasValue"), bodyCallExp));
                }
                else if (propertyType.IsValueType)
                {
                    expressions.Add(bodyCallExp);
                }
                else
                {
                    expressions.Add(IfThen(NotEqual(propertyExp, Constant(null, propertyType)), bodyCallExp));
                }
            }

            expressions.Add(dictionaryExp);

            var bodyExp = Block(new ParameterExpression[] { variableExp, dictionaryExp }, expressions);

            var lambdaExp = Lambda<Func<object, string, List<KeyValuePair<string, object>>>>(bodyExp, objectExp, dateFormatStringExp);

            return lambdaExp.Compile();
        }

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
                var options = GetOptions(HttpMethod.Get, timeout);

                var httpMsg = await SendAsync(options, cancellationToken);

                httpMsg.EnsureSuccessStatusCode();

#if NET6_0_OR_GREATER
                return await httpMsg.Content.ReadAsStreamAsync(cancellationToken);
#else
                return await httpMsg.Content.ReadAsStreamAsync();
#endif
            }

            public override async Task<string> SendAsync(HttpMethod method, double timeout = 1000D, CancellationToken cancellationToken = default)
            {
                var options = GetOptions(method, timeout);

                var httpMsg = await SendAsync(options, cancellationToken);

                httpMsg.EnsureSuccessStatusCode();

#if NET6_0_OR_GREATER
                return await httpMsg.Content.ReadAsStringAsync(cancellationToken);
#else
                return await httpMsg.Content.ReadAsStringAsync();
#endif
            }

            public abstract RequestOptions GetOptions(HttpMethod method, double timeout);

            public abstract Task<HttpResponseMessage> SendAsync(RequestOptions options, CancellationToken cancellationToken = default);
        }

        private interface IToContent
        {
            HttpContent Content { get; }
        }

        private abstract class RequestableEncoding : RequestableString, IRequestableEncoding
        {
            private readonly Encoding encoding;

            private class ToContentByBody : IToContent
            {
                private readonly Encoding encoding;
                private readonly string body;
                private readonly string contentType;

                public ToContentByBody(Encoding encoding, string body, string contentType)
                {
                    this.encoding = encoding;
                    this.body = body;
                    this.contentType = contentType;
                }

                public HttpContent Content => new StringContent(body, encoding, contentType);
            }

            private class ToContentByForm<TBody> : IToContent where TBody : IEnumerable<KeyValuePair<string, object>>
            {
                private readonly Encoding encoding;
                private readonly TBody body;
                private readonly string dateFormatString;

                public ToContentByForm(Encoding encoding, TBody body, string dateFormatString)
                {
                    this.encoding = encoding;
                    this.body = body;
                    this.dateFormatString = dateFormatString ?? "yyyy-MM-dd HH:mm:ss.FFFFFFFK";
                }

                private static void AppendToForm(MultipartFormDataContent content, string name, FileInfo fileInfo)
                {
                    if (fileInfo is null)
                    {
                        throw new ArgumentNullException(nameof(fileInfo));
                    }

                    byte[] byteArray;
                    long contentLength;

                    using (var fileStream = fileInfo.Open(FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        contentLength = fileStream.Length;

                        if (contentLength > int.MaxValue)
                        {
                            using (var ms = new MemoryStream())
                            {
                                fileStream.CopyTo(ms);

                                byteArray = ms.ToArray();
                            }
                        }
                        else
                        {
                            byteArray = new byte[contentLength];

                            fileStream.Read(byteArray, 0, (int)contentLength);
                        }
                    }

                    var byteContent = new ByteArrayContent(byteArray);

                    var extension = Path.GetExtension(fileInfo.Name);

                    if (extension.IsEmpty())
                    {
                        byteContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                    }
                    else if (mediaTypes.TryGetValue(extension.ToLower(), out MediaTypeHeaderValue mediaType))
                    {
                        byteContent.Headers.ContentType = mediaType;
                    }

                    byteContent.Headers.ContentLength = contentLength;

                    content.Add(byteContent, name, fileInfo.Name);
                }

                private static void AppendToForm(MultipartFormDataContent content, Encoding encoding, string name, object value, string dateFormatString, bool throwErrorsIfEnumerable)
                {
                    switch (value)
                    {
                        case string text:

                            content.Add(new StringContent(text, encoding), name);
                            break;
                        case DateTime date:

                            content.Add(new StringContent(date.ToString(dateFormatString), encoding), name);
                            break;
                        case Stream stream:

                            content.Add(new StreamContent(stream), name);
                            break;
                        case FileInfo fileInfo:

                            AppendToForm(content, name, fileInfo);

                            break;
                        case byte[] buffer:

                            content.Add(new StringContent(System.Convert.ToBase64String(buffer), encoding), name);
                            break;
                        case IEnumerable<FileInfo> enumerable:
                            if (throwErrorsIfEnumerable)
                            {
                                throw new InvalidOperationException("不支持多维数组的参数传递!");
                            }

                            foreach (var fileInfo in enumerable)
                            {
                                AppendToForm(content, name, fileInfo);
                            }

                            break;
                        default:
                            if (value is null)
                            {
                                break;
                            }

                            content.Add(new StringContent(value.ToString(), encoding), name);

                            break;
                    }
                }

                public HttpContent Content
                {
                    get
                    {
                        if (body.Any(x => x.Value is FileInfo || x.Value is IEnumerable<FileInfo>))
                        {
                            var content = new MultipartFormDataContent(string.Concat("--", DateTime.Now.Ticks.ToString("x")));

                            foreach (var kv in body)
                            {
                                AppendToForm(content, encoding, kv.Key, kv.Value, dateFormatString, false);
                            }

                            return content;
                        }
                        else
                        {
                            var content = new FormUrlEncodedContent(body.Select(x =>
                            {
                                return x.Value switch
                                {
                                    string text => new KeyValuePair<string, string>(x.Key, text),
                                    DateTime date => new KeyValuePair<string, string>(x.Key, date.ToString(dateFormatString)),
                                    byte[] buffer => new KeyValuePair<string, string>(x.Key, System.Convert.ToBase64String(buffer)),
                                    _ => new KeyValuePair<string, string>(x.Key, x.Value?.ToString())
                                };
                            }));

                            return content;
                        }
                    }
                }
            }

            public RequestableEncoding(Encoding encoding)
            {
                this.encoding = encoding;
            }

            public IRequestableContent Body(string body, string contentType) => new RequestableContent(this, encoding, new ToContentByBody(encoding, body, contentType));

            public IRequestableContent Form<TBody>(TBody body) where TBody : IEnumerable<KeyValuePair<string, object>> => Form(body, "yyyy-MM-dd HH:mm:ss.FFFFFFFK");

            public IRequestableContent Form<TBody>(TBody body, string dateFormatString) where TBody : IEnumerable<KeyValuePair<string, object>> => new RequestableContent(this, encoding, new ToContentByForm<TBody>(encoding, body, dateFormatString));

            public IRequestableContent Form<TBody>(TBody body, NamingType namingType = NamingType.Normal, string dateFormatString = "yyyy-MM-dd HH:mm:ss.FFFFFFFK") where TBody : class
            {
                if (body is null)
                {
                    return this;
                }

                dateFormatString ??= "yyyy-MM-dd HH:mm:ss.FFFFFFFK";

                var results = cachings.GetOrAdd(typeof(TBody), MakeTypeResults)
                    .Invoke(body, dateFormatString);

                return Form(namingType == NamingType.Normal
                        ? results
                        : results.ConvertAll(x => new KeyValuePair<string, object>(x.Key.ToNamingCase(namingType), x.Value))
                    , dateFormatString);
            }

            public IRequestableContent Json(string json) => Body(json, "application/json");

            public IRequestableContent Json<T>(T param, NamingType namingType = NamingType.Normal) where T : class => Json(JsonHelper.ToJson(param, namingType));

            public IJsonDeserializeRequestable<T> JsonCast<T>(NamingType namingType = NamingType.Normal) where T : class => new JsonDeserializeRequestable<T>(this, namingType);

            public IJsonDeserializeRequestable<T> JsonCast<T>(T anonymousTypeObject, NamingType namingType = NamingType.Normal) where T : class => new JsonDeserializeRequestable<T>(this, namingType);

            public IRequestableContent Xml(string xml) => Body(xml, "application/xml");

            public IRequestableContent Xml<T>(T param) where T : class => Xml(XmlHelper.XmlSerialize(param, encoding));

            public IXmlDeserializeRequestable<T> XmlCast<T>() where T : class => new XmlDeserializeRequestable<T>(this, encoding);

            public IXmlDeserializeRequestable<T> XmlCast<T>(T anonymousTypeObject) where T : class => new XmlDeserializeRequestable<T>(this, encoding);

            public IWhenRequestable When(Predicate<HttpStatusCode> whenStatus)
            {
                if (whenStatus is null)
                {
                    throw new ArgumentNullException(nameof(whenStatus));
                }

                return new WhenRequestable(this, encoding, whenStatus);
            }
        }

        private class QueryString<TRequestable> where TRequestable : IRequestableBase
        {
            private bool hasQueryString;
            private readonly StringBuilder sb;
            private readonly TRequestable requestable;

            public QueryString(TRequestable requestable, string requestUri)
            {
                this.requestable = requestable;

                hasQueryString = requestUri.Contains('?');

                sb = new StringBuilder(requestUri);
            }

            public int Length => sb.Length;

            public TRequestable AppendQueryString(string param)
            {
                if (param is null)
                {
                    return requestable;
                }

                int startIndex = 0;
                int length = param.Length;

                for (; startIndex < length; startIndex++)
                {
                    var c = param[startIndex];

                    if (c is ' ' or '?' or '&')
                    {
                        continue;
                    }

                    break;
                }

                if (startIndex >= length)
                {
                    return requestable;
                }

                if (hasQueryString)
                {
                    sb.Append('&');
                }
                else
                {
                    sb.Append('?');

                    hasQueryString = true;
                }

                sb.Append(param, startIndex, length - startIndex);

                return requestable;
            }

            public TRequestable AppendQueryString(string name, string value)
            {
                if (string.IsNullOrEmpty(name))
                {
                    throw new ArgumentException($"“{nameof(name)}”不能为 null 或空。", nameof(name));
                }

                return value.IsEmpty()
                    ? requestable
                    : AppendQueryString(string.Concat(name, "=", HttpUtility.UrlEncode(value)));
            }

            public TRequestable AppendQueryString(string name, DateTime value, string dateFormatString = "yyyy-MM-dd HH:mm:ss.FFFFFFFK") => AppendQueryString(name, value.ToString(dateFormatString ?? "yyyy-MM-dd HH:mm:ss.FFFFFFFK"));

            public TRequestable AppendQueryString(string name, object value, string dateFormatString = "yyyy-MM-dd HH:mm:ss.FFFFFFFK")
            {
                AppendTo(name, value, dateFormatString ?? "yyyy-MM-dd HH:mm:ss.FFFFFFFK", false);

                return requestable;
            }

            private void AppendTo(string name, object value, string dateFormatString, bool throwErrorsIfEnumerable)
            {
                switch (value)
                {
                    case string text:
                        AppendQueryString(name, text);

                        break;
                    case DateTime date:
                        AppendQueryString(name, date, dateFormatString);

                        break;
                    case IEnumerable enumerable:
                        if (throwErrorsIfEnumerable)
                        {
                            throw new InvalidOperationException("不支持多维数组的参数传递!");
                        }

                        foreach (var data in enumerable)
                        {
                            AppendTo(name, data, dateFormatString, true);
                        }

                        break;
                    default:
                        if (value is null)
                        {
                            break;
                        }

                        AppendQueryString(name, value.ToString());

                        break;
                }
            }

            public TRequestable AppendQueryString<TParam>(TParam param) where TParam : IEnumerable<KeyValuePair<string, object>> => AppendQueryString(param, "yyyy-MM-dd HH:mm:ss.FFFFFFFK");

            public TRequestable AppendQueryString<TParam>(TParam param, string dateFormatString) where TParam : IEnumerable<KeyValuePair<string, object>>
            {
                if (param is null)
                {
                    return requestable;
                }

                dateFormatString ??= "yyyy-MM-dd HH:mm:ss.FFFFFFFK";

                foreach (var kv in param)
                {
                    AppendTo(kv.Key, kv.Value, dateFormatString, false);
                }

                return requestable;
            }

            public TRequestable AppendQueryString<TParam>(TParam param, NamingType namingType = NamingType.SnakeCase, string dateFormatString = "yyyy-MM-dd HH:mm:ss.FFFFFFFK") where TParam : class
            {
                if (param is null)
                {
                    return requestable;
                }

                dateFormatString ??= "yyyy-MM-dd HH:mm:ss.FFFFFFFK";

                var results = cachings.GetOrAdd(typeof(TParam), MakeTypeResults)
                    .Invoke(param, dateFormatString);

                return AppendQueryString(namingType == NamingType.Normal
                        ? results
                        : results.ConvertAll(x => new KeyValuePair<string, object>(x.Key.ToNamingCase(namingType), x.Value))
                    , dateFormatString);
            }

            public override string ToString() => sb.ToString();
        }

        private class Requestable : RequestableEncoding, IRequestable, IRequestableBase
        {
            private readonly RequestFactory factory;
            private readonly Dictionary<string, string> headers;
            private readonly QueryString<Requestable> queryString;
            private static readonly Encoding encodingDefault = Encoding.UTF8;

            public Requestable(RequestFactory factory, string requestUri) : base(encodingDefault)
            {
                this.factory = factory;

                headers = new Dictionary<string, string>();
                queryString = new QueryString<Requestable>(this, requestUri);
            }

            private Requestable(RequestFactory factory, Encoding encoding, QueryString<Requestable> queryString, Dictionary<string, string> headers) : base(encoding)
            {
                this.factory = factory;
                this.headers = headers;

                this.queryString = queryString;
            }

            public IRequestable AppendQueryString(string param) => queryString.AppendQueryString(param);

            public IRequestable AppendQueryString(string name, string value) => queryString.AppendQueryString(name, value);

            public IRequestable AppendQueryString(string name, DateTime value, string dateFormatString = "yyyy-MM-dd HH:mm:ss.FFFFFFFK") => queryString.AppendQueryString(name, value, dateFormatString);

            public IRequestable AppendQueryString(string name, object value, string dateFormatString = "yyyy-MM-dd HH:mm:ss.FFFFFFFK") => queryString.AppendQueryString(name, value, dateFormatString);

            public IRequestable AppendQueryString<TParam>(TParam param) where TParam : IEnumerable<KeyValuePair<string, object>> => queryString.AppendQueryString(param);

            public IRequestable AppendQueryString<TParam>(TParam param, string dateFormatString) where TParam : IEnumerable<KeyValuePair<string, object>> => queryString.AppendQueryString(param, dateFormatString);

            public IRequestable AppendQueryString<TParam>(TParam param, NamingType namingType = NamingType.SnakeCase, string dateFormatString = "yyyy-MM-dd HH:mm:ss.FFFFFFFK") where TParam : class => queryString.AppendQueryString(param, namingType, dateFormatString);

            public IRequestable AssignHeader(string header, string value)
            {
                if (header is null)
                {
                    throw new ArgumentNullException(nameof(header));
                }

                headers[header] = value;

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

            public override RequestOptions GetOptions(HttpMethod method, double timeout) => new RequestOptions(queryString.ToString(), headers)
            {
                Method = method,
                Timeout = timeout,
            };

            public IRequestableEncoding UseEncoding(Encoding encoding)
            {
                if (encoding is null || Equals(encodingDefault, encoding))
                {
                    return this;
                }

                return new Requestable(factory, encoding, queryString, headers);
            }

            IRequestableBase IRequestableBase<IRequestableBase>.AssignHeader(string header, string value)
            {
                AssignHeader(header, value);

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

            public override Task<HttpResponseMessage> SendAsync(RequestOptions options, CancellationToken cancellationToken = default) => factory.SendAsync(options, cancellationToken);
        }

        private class WhenRequestable : IWhenRequestable
        {
            private readonly RequestableString requestable;
            private readonly Encoding encoding;
            private readonly Predicate<HttpStatusCode> whenStatus;

            public WhenRequestable(RequestableString requestable, Encoding encoding, Predicate<HttpStatusCode> whenStatus)
            {
                this.requestable = requestable;
                this.encoding = encoding;
                this.whenStatus = whenStatus;
            }

            public IThenRequestable ThenAsync(Func<IRequestableBase, Task> thenAsync)
            {
                if (thenAsync is null)
                {
                    throw new ArgumentNullException(nameof(thenAsync));
                }

                return new ThenRequestable(requestable, encoding, whenStatus, thenAsync);
            }
        }

        private class ThenRequestable : RequestableEncoding, IThenRequestable
        {
            private volatile bool initializedStatusCode;
            private readonly RequestableString requestable;
            private readonly Predicate<HttpStatusCode> whenStatus;
            private readonly Func<IRequestableBase, Task> thenAsync;

            public ThenRequestable(RequestableString requestable, Encoding encoding, Predicate<HttpStatusCode> whenStatus, Func<IRequestableBase, Task> thenAsync) : base(encoding)
            {
                this.requestable = requestable;
                this.whenStatus = whenStatus;
                this.thenAsync = thenAsync;
            }

            public override RequestOptions GetOptions(HttpMethod method, double timeout) => requestable.GetOptions(method, timeout);

            public override async Task<HttpResponseMessage> SendAsync(RequestOptions options, CancellationToken cancellationToken = default)
            {
                var httpMsg = await requestable.SendAsync(options, cancellationToken);

                if (initializedStatusCode)
                {
                    return httpMsg;
                }

                if (whenStatus(httpMsg.StatusCode))
                {
                    initializedStatusCode = true;

                    var requestableRef = new RequestableBase(requestable);

                    await thenAsync(requestableRef);

                    return await requestableRef.SendAsync(options.Method, options.Timeout, cancellationToken);
                }

                return httpMsg;
            }

            private class RequestableBase : IRequestableBase
            {
                private readonly RequestableString requestable;
                private readonly QueryString<RequestableBase> queryString;
                private readonly Dictionary<string, string> headers = new Dictionary<string, string>();

                public RequestableBase(RequestableString requestable)
                {
                    this.requestable = requestable;

                    queryString = new QueryString<RequestableBase>(this, string.Empty);
                }

                public IRequestableBase AppendQueryString(string param) => queryString.AppendQueryString(param);

                public IRequestableBase AppendQueryString(string name, string value) => queryString.AppendQueryString(name, value);

                public IRequestableBase AppendQueryString(string name, DateTime value, string dateFormatString = "yyyy-MM-dd HH:mm:ss.FFFFFFFK") => queryString.AppendQueryString(name, value, dateFormatString);

                public IRequestableBase AppendQueryString(string name, object value, string dateFormatString = "yyyy-MM-dd HH:mm:ss.FFFFFFFK") => queryString.AppendQueryString(name, value, dateFormatString);

                public IRequestableBase AppendQueryString<TParam>(TParam param) where TParam : IEnumerable<KeyValuePair<string, object>> => queryString.AppendQueryString(param);

                public IRequestableBase AppendQueryString<TParam>(TParam param, string dateFormatString) where TParam : IEnumerable<KeyValuePair<string, object>> => queryString.AppendQueryString(param, dateFormatString);

                public IRequestableBase AppendQueryString<TParam>(TParam param, NamingType namingType = NamingType.SnakeCase, string dateFormatString = "yyyy-MM-dd HH:mm:ss.FFFFFFFK") where TParam : class => queryString.AppendQueryString(param, namingType, dateFormatString);

                public IRequestableBase AssignHeader(string header, string value)
                {
                    if (header is null)
                    {
                        throw new ArgumentNullException(nameof(header));
                    }

                    headers[header] = value;

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
                    if (queryString.Length == 0)
                    {
                        return requestUri;
                    }

                    int indexOf = requestUri.IndexOf('?');

                    var queryStrings = queryString.ToString();

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

                public Task<HttpResponseMessage> SendAsync(HttpMethod method, double timeout, CancellationToken cancellationToken = default)
                {
                    var options = requestable.GetOptions(method, timeout);

                    options.RequestUri = RequestUriRef(options.RequestUri);

                    if (headers.Count > 0)
                    {
                        foreach (var header in headers)
                        {
                            options.Headers[header.Key] = header.Value;
                        }
                    }

                    return requestable.SendAsync(options, cancellationToken);
                }
            }
        }

        private class RequestableContent : RequestableString, IRequestableContent
        {
            private readonly RequestableString requestable;
            private readonly Encoding encoding;
            private readonly IToContent content;

            public RequestableContent(RequestableString requestable, Encoding encoding, IToContent content)
            {
                this.requestable = requestable;
                this.encoding = encoding;
                this.content = content;
            }

            public IJsonDeserializeRequestable<T> JsonCast<T>(NamingType namingType = NamingType.Normal) where T : class => new JsonDeserializeRequestable<T>(this, namingType);

            public IXmlDeserializeRequestable<T> XmlCast<T>() where T : class => new XmlDeserializeRequestable<T>(this, encoding);

            public IJsonDeserializeRequestable<T> JsonCast<T>(T anonymousTypeObject, NamingType namingType = NamingType.Normal) where T : class => JsonCast<T>(namingType);

            public IXmlDeserializeRequestable<T> XmlCast<T>(T anonymousTypeObject) where T : class => XmlCast<T>();

            public override RequestOptions GetOptions(HttpMethod method, double timeout)
            {
                var options = requestable.GetOptions(method, timeout);

                options.Content = content.Content;

                return options;
            }

            public override Task<HttpResponseMessage> SendAsync(RequestOptions options, CancellationToken cancellationToken = default) => requestable.SendAsync(options, cancellationToken);

            public IWhenRequestable When(Predicate<HttpStatusCode> whenStatus)
            {
                if (whenStatus is null)
                {
                    throw new ArgumentNullException(nameof(whenStatus));
                }

                return new WhenRequestable(this, encoding, whenStatus);
            }
        }

        private class RequestableDataVerify<T> : IRequestableDataVerify<T>
        {
            private readonly Requestable<T> requestable;
            private readonly Predicate<T> dataVerify;

            public RequestableDataVerify(Requestable<T> requestable, Predicate<T> dataVerify)
            {
                this.requestable = requestable;
                this.dataVerify = dataVerify;
            }

            public IRequestableDataVerifySuccess<T, TResult> Success<TResult>(Func<T, TResult> dataVerifySuccess)
            {
                if (dataVerifySuccess is null)
                {
                    throw new ArgumentNullException(nameof(dataVerifySuccess));
                }

                return new RequestableDataVerifySuccess<T, TResult>(requestable, dataVerify, dataVerifySuccess);
            }

            public IRequestableDataVerifyFail<T> Fail<TError>(Func<T, TError> throwError) where TError : Exception
            {
                if (throwError is null)
                {
                    throw new ArgumentNullException(nameof(throwError));
                }

                return new RequestableDataVerifyError<T, TError>(requestable, dataVerify, throwError);
            }
        }

        private class RequestableDataVerifyError<T, TError> : Requestable<T>, IRequestableDataVerifyFail<T> where TError : Exception
        {
            private readonly Requestable<T> requestable;
            private readonly Predicate<T> dataVerify;
            private readonly Func<T, TError> throwError;

            public RequestableDataVerifyError(Requestable<T> requestable, Predicate<T> dataVerify, Func<T, TError> throwError)
            {
                this.requestable = requestable;
                this.dataVerify = dataVerify;
                this.throwError = throwError;
            }

            public override async Task<T> SendAsync(HttpMethod method, double timeout = 1000D, CancellationToken cancellationToken = default)
            {
                var msgData = await requestable.SendAsync(method, timeout, cancellationToken);

                if (dataVerify(msgData))
                {
                    return msgData;
                }

                throw throwError.Invoke(msgData);
            }
        }

        private class RequestableDataVerifyError<T, TResult, TError> : Requestable<TResult>, IRequestableDataVerifyFail<T, TResult> where TError : Exception
        {
            private readonly Requestable<T> requestable;
            private readonly Predicate<T> dataVerify;
            private readonly Func<T, TResult> dataVerifySuccess;
            private readonly Func<T, TError> throwError;

            public RequestableDataVerifyError(Requestable<T> requestable, Predicate<T> dataVerify, Func<T, TResult> dataVerifySuccess, Func<T, TError> throwError)
            {
                this.requestable = requestable;
                this.dataVerify = dataVerify;
                this.dataVerifySuccess = dataVerifySuccess;
                this.throwError = throwError;
            }

            public override async Task<TResult> SendAsync(HttpMethod method, double timeout = 1000, CancellationToken cancellationToken = default)
            {
                var msgData = await requestable.SendAsync(method, timeout, cancellationToken);

                if (dataVerify(msgData))
                {
                    return dataVerifySuccess.Invoke(msgData);
                }

                throw throwError.Invoke(msgData);
            }
        }

        private class RequestableDataVerifyFail<T, TResult> : Requestable<TResult>, IRequestableDataVerifyFail<T, TResult>
        {
            private readonly Requestable<T> requestable;
            private readonly Predicate<T> dataVerify;
            private readonly Func<T, TResult> dataVerifySuccess;
            private readonly Func<T, TResult> dataVerifyFail;

            public RequestableDataVerifyFail(Requestable<T> requestable, Predicate<T> dataVerify, Func<T, TResult> dataVerifySuccess, Func<T, TResult> dataVerifyFail)
            {
                this.requestable = requestable;
                this.dataVerify = dataVerify;
                this.dataVerifySuccess = dataVerifySuccess;
                this.dataVerifyFail = dataVerifyFail;
            }

            public override async Task<TResult> SendAsync(HttpMethod method, double timeout = 1000, CancellationToken cancellationToken = default)
            {
                var msgData = await requestable.SendAsync(method, timeout, cancellationToken);

                if (dataVerify(msgData))
                {
                    return dataVerifySuccess.Invoke(msgData);
                }

                return dataVerifyFail.Invoke(msgData);
            }
        }

        private class RequestableDataVerifySuccess<T, TResult> : IRequestableDataVerifySuccess<T, TResult>
        {
            private readonly Requestable<T> requestable;
            private readonly Predicate<T> dataVerify;
            private readonly Func<T, TResult> dataVerifySuccess;

            public RequestableDataVerifySuccess(Requestable<T> requestable, Predicate<T> dataVerify, Func<T, TResult> dataVerifySuccess)
            {
                this.requestable = requestable;
                this.dataVerify = dataVerify;
                this.dataVerifySuccess = dataVerifySuccess;
            }

            public IRequestableDataVerifyFail<T, TResult> Fail(Func<T, TResult> dataVerifyFail)
            {
                if (dataVerifyFail is null)
                {
                    throw new ArgumentNullException(nameof(dataVerifyFail));
                }

                return new RequestableDataVerifyFail<T, TResult>(requestable, dataVerify, dataVerifySuccess, dataVerifyFail);
            }

            public IRequestableDataVerifyFail<T, TResult> Fail<TError>(Func<T, TError> throwError) where TError : Exception
            {
                if (throwError is null)
                {
                    throw new ArgumentNullException(nameof(throwError));
                }

                return new RequestableDataVerifyError<T, TResult, TError>(requestable, dataVerify, dataVerifySuccess, throwError);
            }
        }

        private class JsonDeserializeRequestable<T> : Requestable<T>, IJsonDeserializeRequestable<T>, IRequestableExtend<T>
        {
            private readonly RequestableString requestable;
            private readonly NamingType namingType;

            public JsonDeserializeRequestable(RequestableString requestable, NamingType namingType)
            {
                this.requestable = requestable;
                this.namingType = namingType;
            }

            public IRequestableDataVerify<T> DataVerify(Predicate<T> predicate)
            {
                if (predicate is null)
                {
                    throw new ArgumentNullException(nameof(predicate));
                }

                return new RequestableDataVerify<T>(this, predicate);
            }

            public IRequestableExtend<T> JsonCatch(Func<Exception, T> abnormalResultAnalysis)
            {
                if (abnormalResultAnalysis is null)
                {
                    throw new ArgumentNullException(nameof(abnormalResultAnalysis));
                }

                return new JsonDeserializeRequestableCatch<T>(requestable, namingType, abnormalResultAnalysis);
            }

            public override async Task<T> SendAsync(HttpMethod method, double timeout = 1000D, CancellationToken cancellationToken = default)
            {
                var stringMsg = await requestable.SendAsync(method, timeout, cancellationToken);

                return JsonHelper.Json<T>(stringMsg, namingType);
            }
        }

        private class JsonDeserializeRequestableCatch<T> : JsonDeserializeRequestable<T>, IRequestableExtend<T>
        {
            private readonly Func<Exception, T> abnormalResultAnalysis;

            public JsonDeserializeRequestableCatch(RequestableString requestable, NamingType namingType, Func<Exception, T> abnormalResultAnalysis) : base(requestable, namingType)
            {
                this.abnormalResultAnalysis = abnormalResultAnalysis;
            }

            private static bool IsJsonError(Exception e)
            {
                for (Type type = e.GetType(), destinationType = typeof(Exception); type != destinationType; type = type.BaseType ?? destinationType)
                {
                    if (type.Name == "JsonException")
                    {
                        return true;
                    }
                }

                return false;
            }

            public override async Task<T> SendAsync(HttpMethod method, double timeout = 1000, CancellationToken cancellationToken = default)
            {
                try
                {
                    return await base.SendAsync(method, timeout, cancellationToken);
                }
                catch (Exception ex) when (IsJsonError(ex))
                {
                    return abnormalResultAnalysis.Invoke(ex);
                }
            }
        }

        private class XmlDeserializeRequestable<T> : Requestable<T>, IXmlDeserializeRequestable<T>, IRequestableExtend<T>
        {
            private readonly RequestableString requestable;
            private readonly Encoding encoding;

            public XmlDeserializeRequestable(RequestableString requestable, Encoding encoding)
            {
                this.requestable = requestable;
                this.encoding = encoding;
            }

            public IRequestableDataVerify<T> DataVerify(Predicate<T> predicate)
            {
                if (predicate is null)
                {
                    throw new ArgumentNullException(nameof(predicate));
                }

                return new RequestableDataVerify<T>(this, predicate);
            }

            public override async Task<T> SendAsync(HttpMethod method, double timeout = 1000D, CancellationToken cancellationToken = default)
            {
                var stringMsg = await requestable.SendAsync(method, timeout, cancellationToken);

                return XmlHelper.XmlDeserialize<T>(stringMsg, encoding);
            }

            public IRequestableExtend<T> XmlCatch(Func<XmlException, T> abnormalResultAnalysis)
            {
                if (abnormalResultAnalysis is null)
                {
                    throw new ArgumentNullException(nameof(abnormalResultAnalysis));
                }

                return new XmlDeserializeRequestableCatch<T>(requestable, encoding, abnormalResultAnalysis);
            }
        }

        private class XmlDeserializeRequestableCatch<T> : XmlDeserializeRequestable<T>, IRequestableExtend<T>
        {
            private readonly Func<XmlException, T> abnormalResultAnalysis;

            public XmlDeserializeRequestableCatch(RequestableString requestable, Encoding encoding, Func<XmlException, T> abnormalResultAnalysis) : base(requestable, encoding)
            {
                this.abnormalResultAnalysis = abnormalResultAnalysis;
            }

            public override async Task<T> SendAsync(HttpMethod method, double timeout = 1000, CancellationToken cancellationToken = default)
            {
                try
                {
                    return await base.SendAsync(method, timeout, cancellationToken);
                }
                catch (XmlException ex)
                {
                    return abnormalResultAnalysis.Invoke(ex);
                }
            }
        }

#if NET_Traditional
        static RequestFactory()
        {
            ServicePointManager.ServerCertificateValidationCallback += (sender, certificate, chain, sslPolicyErrors) => true;
        }
#endif

        /// <summary>
        /// 请求工厂。
        /// </summary>
        protected RequestFactory() : this(new RequestInitialize())
        {
        }

        /// <summary>
        /// 请求工厂。
        /// </summary>
        /// <param name="initialize">请求初始化。</param>
        /// <exception cref="ArgumentNullException">参数 <paramref name="initialize"/> 为“null”。</exception>
        public RequestFactory(IRequestInitialize initialize)
        {
            this.initialize = initialize ?? throw new ArgumentNullException(nameof(initialize));
        }

        /// <summary>
        /// 创建请求能力。
        /// </summary>
        /// <param name="requestUri">请求地址。</param>
        /// <returns>请求能力。</returns>
        public IRequestable CreateRequestable(string requestUri)
        {
            var requestable = new Requestable(this, requestUri);

            Initialize(requestable);

            return requestable;
        }

        /// <summary>
        /// 注册文件媒体类型。
        /// </summary>
        /// <param name="fileSuffix">文件后缀，含符号“.”。</param>
        /// <param name="mediaType">媒体类型。</param>
        /// <exception cref="ArgumentNullException"></exception>
        public static void RegisterMediaType(string fileSuffix, string mediaType)
        {
            if (fileSuffix is null)
            {
                throw new ArgumentNullException(nameof(fileSuffix));
            }

            if (mediaType is null)
            {
                throw new ArgumentNullException(nameof(mediaType));
            }

            if (fileSuffix[0] != '.')
            {
                throw new ArgumentException("文件后缀必须以“.”开头！");
            }

            mediaTypes[fileSuffix.ToLower()] = new MediaTypeHeaderValue(mediaType);
        }

        /// <summary>
        /// 初始化。
        /// </summary>
        /// <param name="requestable">基础请求能力。</param>
        protected virtual void Initialize(IRequestableBase requestable) => initialize.Initialize(requestable);

        /// <summary>
        /// 发送请求。
        /// </summary>
        /// <param name="options">请求配置。</param>
        /// <param name="cancellationToken">可由其他对象或线程用以接收取消通知的取消标记。</param>
        /// <returns>请求结果。</returns>
        protected virtual async Task<HttpResponseMessage> SendAsync(RequestOptions options, CancellationToken cancellationToken)
        {
            if (options is null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            var client = clients.Get(options.Timeout);

            using (var httpMsg = new HttpRequestMessage(options.Method, options.RequestUri))
            {
                httpMsg.Content = options.Content;

                if (options.Headers.Count > 0)
                {
                    foreach (var kv in options.Headers)
                    {
                        httpMsg.Headers.Add(kv.Key, kv.Value);
                    }
                }

                try
                {
                    return await client.SendAsync(httpMsg, cancellationToken);
                }
                catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
                {
                    throw new TimeoutException();
                }
            }
        }

        /// <summary>
        /// 创建请求能力。
        /// </summary>
        /// <param name="requestUri">请求地址。</param>
        /// <returns>请求能力。</returns>
        public static IRequestable Create(string requestUri) => factory.CreateRequestable(requestUri);
    }
}