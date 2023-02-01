using Inkslab.Collections;
using Inkslab.Net.Options;
using Inkslab.Serialize.Json;
using Inkslab.Serialize.Xml;
using System;
using System.Collections;
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
using System.Xml;

namespace Inkslab.Net
{
    using static Expression;

    /// <summary>
    /// 请求工厂。
    /// </summary>
    public class RequestFactory : IRequestFactory
    {
        private static readonly Type kvType = typeof(KeyValuePair<string, object>);
        private static readonly ConstructorInfo kvCtor = kvType.GetConstructor(new Type[] { typeof(string), typeof(object) });

        private static readonly Type listKVType = typeof(List<KeyValuePair<string, object>>);
        private static readonly ConstructorInfo listKVCtor = listKVType.GetConstructor(new Type[] { typeof(int) });
        private static readonly MethodInfo listKVAddFn = listKVType.GetMethod("Add", new Type[] { kvType });

        private static readonly Type dateType = typeof(DateTime);
        private static readonly MethodInfo dateToStringFn = dateType.GetMethod("ToString", new Type[] { typeof(string) });

        private static readonly LRU<double, HttpClient> clients = new LRU<double, HttpClient>(timeout => new HttpClient { Timeout = TimeSpan.FromMilliseconds(timeout) });

        private static readonly LRU<Type, Func<object, string, List<KeyValuePair<string, object>>>> lru = new LRU<Type, Func<object, string, List<KeyValuePair<string, object>>>>(type =>
        {
            var objectType = typeof(object);

            var objectExp = Parameter(objectType, "param");
            var dateFormatStringExp = Parameter(typeof(string), "dateFormatString");
            var variableExp = Variable(type, "variable");
            var dictionaryExp = Variable(listKVType, "dictionary");

            var propertyInfos = Array.FindAll(type.GetProperties(), x => x.CanRead);

            var expressions = new List<Expression>
            {
                Assign(variableExp, Convert(objectExp, type)),
                Assign(dictionaryExp, New(listKVCtor, Constant(propertyInfos.Length)))
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

                    propertyType = Nullable.GetUnderlyingType(propertyType);
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

                    var toStringFn = propertyType.GetMethod("ToString", Type.EmptyTypes);

                    valueExp = Call(valueExp, toStringFn);
                }

                var bodyCallExp = Call(dictionaryExp, listKVAddFn, New(kvCtor, Constant(propertyInfo.Name), valueExp));

                if (isNullable)
                {
                    expressions.Add(IfThen(Property(propertyExp, "HasValue"), bodyCallExp));
                }
                else
                {
                    expressions.Add(bodyCallExp);
                }
            }

            expressions.Add(dictionaryExp);

            var bodyExp = Block(new ParameterExpression[] { variableExp, dictionaryExp }, expressions);

            var lambdaExp = Lambda<Func<object, string, List<KeyValuePair<string, object>>>>(bodyExp, objectExp, dateFormatStringExp);

            return lambdaExp.Compile();
        });

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
            private readonly RequestFactory factory;

            public RequestableString(RequestFactory factory)
            {
                this.factory = factory;
            }

            public async Task<Stream> DownloadAsync(double timeout = 10000D, CancellationToken cancellationToken = default)
            {
                var options = GetOptions(HttpMethod.Get, timeout);

                var httpMsg = await SendAsync(options, cancellationToken);

                httpMsg.EnsureSuccessStatusCode();

                return await httpMsg.Content.ReadAsStreamAsync();
            }

            public override async Task<string> SendAsync(HttpMethod method, double timeout = 1000D, CancellationToken cancellationToken = default)
            {
                var options = GetOptions(method, timeout);

                var httpMsg = await SendAsync(options, cancellationToken);

                httpMsg.EnsureSuccessStatusCode();

                return await httpMsg.Content.ReadAsStringAsync();
            }

            protected abstract RequestOptions GetOptions(HttpMethod method, double timeout);

            protected virtual Task<HttpResponseMessage> SendAsync(RequestOptions options, CancellationToken cancellationToken = default) => factory.SendAsync(options, cancellationToken);
        }

        private class Requestable : RequestableString, IRequestable, IRequestableBase
        {
            private bool hasQueryString = false;
            private Encoding encoding = Encoding.UTF8;
            private readonly StringBuilder sb = new StringBuilder();
            private readonly Dictionary<string, string> headers = new Dictionary<string, string>();
            private readonly RequestFactory factory;

            public Requestable(RequestFactory factory, string requestUri) : base(factory)
            {
                if (requestUri is null)
                {
                    throw new ArgumentNullException(nameof(requestUri));
                }

                this.factory = factory;

                hasQueryString = requestUri.IndexOf('?') >= 0;

                sb = new StringBuilder(requestUri);
            }

            public IRequestable AppendQueryString(string param)
            {
                if (param is null)
                {
                    return this;
                }

                int startIndex = 0;
                int length = param.Length;

                for (; startIndex < length; startIndex++)
                {
                    var c = param[startIndex];

                    if (c == ' ' || c == '?' || c == '&')
                    {
                        continue;
                    }

                    break;
                }

                if (startIndex >= length)
                {
                    return this;
                }

                sb.Append(hasQueryString ? '&' : '?')
                    .Append(param, startIndex, length - startIndex);

                hasQueryString = true;

                return this;
            }

            public IRequestable AppendQueryString(string name, string value)
            {
                if (string.IsNullOrEmpty(name))
                {
                    throw new ArgumentException($"“{nameof(name)}”不能为 null 或空。", nameof(name));
                }

                return AppendQueryString(string.Concat(name, "=", value ?? string.Empty));
            }

            public IRequestable AppendQueryString(string name, DateTime value, string dateFormatString = "yyyy-MM-dd HH:mm:ss.FFFFFFFK") => AppendQueryString(name, value.ToString(dateFormatString));

            public IRequestable AppendQueryString(string name, object value, string dateFormatString = "yyyy-MM-dd HH:mm:ss.FFFFFFFK")
            {
                AppendTo(name, value, dateFormatString, false);

                return this;
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

            public IRequestable AppendQueryString<TParam>(TParam param) where TParam : IEnumerable<KeyValuePair<string, object>> => AppendQueryString(param, "yyyy-MM-dd HH:mm:ss.FFFFFFFK");

            public IRequestable AppendQueryString<TParam>(TParam param, string dateFormatString) where TParam : IEnumerable<KeyValuePair<string, object>>
            {
                if (param is null)
                {
                    return this;
                }

                dateFormatString ??= "yyyy-MM-dd HH:mm:ss.FFFFFFFK";

                foreach (var kv in param)
                {
                    AppendTo(kv.Key, kv.Value, dateFormatString, false);
                }

                return this;
            }

            public IRequestable AppendQueryString<TParam>(TParam param, NamingType namingType = NamingType.UrlCase, string dateFormatString = "yyyy-MM-dd HH:mm:ss.FFFFFFFK") where TParam : class
            {
                if (param is null)
                {
                    return this;
                }

                dateFormatString ??= "yyyy-MM-dd HH:mm:ss.FFFFFFFK";

                var results = lru.Get(typeof(TParam))
                     .Invoke(param, dateFormatString);

                if (namingType == NamingType.Normal)
                {
                    return AppendQueryString(results, dateFormatString);
                }

                return AppendQueryString(results.ConvertAll(x => new KeyValuePair<string, object>(x.Key.ToNamingCase(namingType), x.Value)), dateFormatString);
            }

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

            protected override RequestOptions GetOptions(HttpMethod method, double timeout) => new RequestOptions(sb.ToString(), headers)
            {
                Method = method,
                Timeout = timeout,
            };

            public IThenRequestable TryThenAsync(Func<IRequestableBase, Task> thenAsync)
            {
                if (thenAsync is null)
                {
                    throw new ArgumentNullException(nameof(thenAsync));
                }

                var options = new RequestOptions(sb.ToString(), headers);

                return new ThenRequestable(factory, encoding, options, thenAsync);
            }

            public IRequestableEncoding UseEncoding(Encoding encoding)
            {
                if (encoding is null)
                {
                    return this;
                }

                this.encoding = encoding;

                return this;
            }

            public IRequestableContent Body(string body, string contentType)
            {
                if (body is null)
                {
                    throw new ArgumentNullException(nameof(body));
                }

                AssignHeader("Content-Type", contentType);

                var content = new StringContent(body, encoding, contentType);

                var options = new RequestOptions(sb.ToString(), headers)
                {
                    Content = content
                };

                return new RequestableContent(factory, encoding, options);
            }

            public IRequestableContent Xml(string xml) => Body(xml, "application/xml");

            public IRequestableContent Xml<T>(T param) where T : class => Xml(XmlHelper.XmlSerialize(param, encoding));

            public IRequestableContent Json(string json) => Body(json, "application/json");

            public IRequestableContent Json<T>(T param, NamingType namingType = NamingType.Normal) where T : class => Json(JsonHelper.ToJson(param, namingType));

            public IJsonDeserializeRequestable<T> JsonCast<T>(NamingType namingType = NamingType.Normal) where T : class => new JsonDeserializeRequestable<T>(this, namingType);

            public IXmlDeserializeRequestable<T> XmlCast<T>() where T : class => new XmlDeserializeRequestable<T>(this, encoding);

            public IJsonDeserializeRequestable<T> JsonCast<T>(T anonymousTypeObject, NamingType namingType = NamingType.Normal) where T : class => JsonCast<T>(namingType);

            public IXmlDeserializeRequestable<T> XmlCast<T>(T anonymousTypeObject) where T : class => XmlCast<T>();

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

            public IRequestableContent Form<TBody>(TBody body) where TBody : IEnumerable<KeyValuePair<string, object>> => Form(body, "yyyy-MM-dd HH:mm:ss.FFFFFFFK");

            public IRequestableContent Form<TBody>(TBody body, string dateFormatString) where TBody : IEnumerable<KeyValuePair<string, object>>
            {
                if (body is null)
                {
                    return this;
                }

                if (body.Any(x => x.Value is FileInfo || x.Value is IEnumerable<FileInfo>))
                {
                    var content = new MultipartFormDataContent(string.Concat("--", DateTime.Now.Ticks.ToString("x")));

                    foreach (var kv in body)
                    {
                        AppendToForm(content, kv.Key, kv.Value, dateFormatString, false);
                    }

                    content.Headers.ContentType = new MediaTypeHeaderValue("multipart/form-data");

                    AssignHeader("Content-Type", "multipart/form-data");

                    return new RequestableContent(factory, encoding, new RequestOptions(sb.ToString(), headers)
                    {
                        Content = content
                    });
                }
                else
                {
                    var content = new FormUrlEncodedContent(body.Select(x =>
                    {
                        switch (x.Value)
                        {
                            case string text:
                                return new KeyValuePair<string, string>(x.Key, text);
                            case DateTime date:
                                return new KeyValuePair<string, string>(x.Key, date.ToString(dateFormatString));
                            case byte[] buffer:
                                return new KeyValuePair<string, string>(x.Key, System.Convert.ToBase64String(buffer));
                            default:
                                return new KeyValuePair<string, string>(x.Key, x.Value?.ToString());
                        }
                    }));

                    content.Headers.ContentType = new MediaTypeHeaderValue("x-www-form-urlencoded");

                    AssignHeader("Content-Type", "x-www-form-urlencoded");

                    return new RequestableContent(factory, encoding, new RequestOptions(sb.ToString(), headers)
                    {
                        Content = content
                    });
                }
            }

            private void AppendToForm(MultipartFormDataContent content, string name, FileInfo fileInfo)
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

            private void AppendToForm(MultipartFormDataContent content, string name, object value, string dateFormatString, bool throwErrorsIfEnumerable)
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

            public IRequestableContent Form<TBody>(TBody body, NamingType namingType = NamingType.Normal, string dateFormatString = "yyyy-MM-dd HH:mm:ss.FFFFFFFK") where TBody : class
            {
                if (body is null)
                {
                    return this;
                }

                dateFormatString ??= "yyyy-MM-dd HH:mm:ss.FFFFFFFK";

                var results = lru.Get(typeof(TBody))
                     .Invoke(body, dateFormatString);

                if (namingType == NamingType.Normal)
                {
                    return AppendQueryString(results, dateFormatString);
                }

                return Form(results.ConvertAll(x => new KeyValuePair<string, object>(x.Key.ToNamingCase(namingType), x.Value)), dateFormatString);
            }
        }

        private class ThenCondition
        {
            private readonly Func<IRequestableBase, Task> initialize;

            public ThenCondition(Func<IRequestableBase, Task> initialize)
            {
                this.initialize = initialize;

                Conditions = new List<Predicate<HttpStatusCode>>();
            }

            public List<Predicate<HttpStatusCode>> Conditions { get; }

            public Task InitializeAsync(IRequestableBase requestable) => initialize.Invoke(requestable);
        }

        private class ThenRequestable : IThenRequestable
        {
            private readonly RequestFactory factory;
            private readonly RequestOptions options;
            private readonly ThenCondition thenCondition;
            private readonly List<ThenCondition> thenConditions;
            private readonly Encoding encoding;

            public ThenRequestable(RequestFactory factory, Encoding encoding, RequestOptions options, Func<IRequestableBase, Task> thenAsync) : this(factory, encoding, options, thenAsync, new List<ThenCondition>())
            {
            }

            public ThenRequestable(RequestFactory factory, Encoding encoding, RequestOptions options, Func<IRequestableBase, Task> thenAsync, List<ThenCondition> thenConditions)
            {
                this.factory = factory;
                this.encoding = encoding;
                this.options = options;
                this.thenConditions = thenConditions;

                thenConditions.Add(thenCondition = new ThenCondition(thenAsync));
            }

            public IThenConditionRequestable If(Predicate<HttpStatusCode> predicate)
            {
                if (predicate is null)
                {
                    throw new ArgumentNullException(nameof(predicate));
                }

                thenCondition.Conditions.Add(predicate);

                return new ThenConditionRequestable(factory, encoding, options, thenCondition, thenConditions);
            }
        }

        private class ThenConditionRequestable : RequestableString, IThenConditionRequestable
        {
            private readonly RequestFactory factory;
            private readonly Encoding encoding;
            private readonly RequestOptions options;
            private readonly ThenCondition thenCondition;
            private readonly List<ThenCondition> thenConditions;

            public ThenConditionRequestable(RequestFactory factory, Encoding encoding, RequestOptions options, ThenCondition thenCondition, List<ThenCondition> thenConditions) : base(factory)
            {
                this.factory = factory;
                this.encoding = encoding;
                this.options = options;
                this.thenCondition = thenCondition;
                this.thenConditions = thenConditions;
            }

            public IJsonDeserializeRequestable<T> JsonCast<T>(NamingType namingType = NamingType.Normal) where T : class => new JsonDeserializeRequestable<T>(this, namingType);

            public IJsonDeserializeRequestable<T> JsonCast<T>(T anonymousTypeObject, NamingType namingType = NamingType.Normal) where T : class => JsonCast<T>(namingType);

            public IThenConditionRequestable Or(Predicate<HttpStatusCode> predicate)
            {
                if (predicate is null)
                {
                    throw new ArgumentNullException(nameof(predicate));
                }

                thenCondition.Conditions.Add(predicate);

                return this;
            }

            public IThenRequestable ThenAsync(Func<IRequestableBase, Task> thenAsync)
            {
                if (thenAsync is null)
                {
                    throw new ArgumentNullException(nameof(thenAsync));
                }

                return new ThenRequestable(factory, encoding, options, thenAsync, thenConditions);
            }

            public IXmlDeserializeRequestable<T> XmlCast<T>() where T : class => new XmlDeserializeRequestable<T>(this, encoding);

            public IXmlDeserializeRequestable<T> XmlCast<T>(T anonymousTypeObject) where T : class => XmlCast<T>();

            protected override RequestOptions GetOptions(HttpMethod method, double timeout)
            {
                options.Method = method;
                options.Timeout = timeout;

                return options;
            }

            protected override async Task<HttpResponseMessage> SendAsync(RequestOptions options, CancellationToken cancellationToken = default)
            {
                var httpMsg = await base.SendAsync(options, cancellationToken);

                var statusCode = httpMsg.StatusCode;

                foreach (var thenCondition in thenConditions)
                {
                    if (thenCondition.Conditions.Exists(x => x.Invoke(statusCode)))
                    {
                        var requestable = new RequestableBase(options.RequestUri, options.Headers);

                        await thenCondition.InitializeAsync(requestable);

                        httpMsg = await base.SendAsync(requestable.GetOptions(options), cancellationToken);

                        if (httpMsg.IsSuccessStatusCode)
                        {
                            break;
                        }
                    }
                }

                return httpMsg;
            }

            private class RequestableBase : IRequestableBase
            {
                private bool hasQueryString;
                private readonly StringBuilder sb;
                private readonly Dictionary<string, string> headers;

                public RequestableBase(string requestUri, Dictionary<string, string> headers)
                {
                    hasQueryString = requestUri.IndexOf('?') >= 0;

                    this.headers = headers;

                    sb = new StringBuilder(requestUri);
                }

                public IRequestableBase AppendQueryString(string param)
                {
                    if (param is null)
                    {
                        return this;
                    }

                    int startIndex = 0;
                    int length = param.Length;

                    for (; startIndex < length; startIndex++)
                    {
                        var c = param[startIndex];

                        if (c == ' ' || c == '?' || c == '&')
                        {
                            continue;
                        }

                        break;
                    }

                    if (startIndex >= length)
                    {
                        return this;
                    }

                    sb.Append(hasQueryString ? '&' : '?')
                        .Append(param, startIndex, length - startIndex);

                    hasQueryString = true;

                    return this;
                }

                public IRequestableBase AppendQueryString(string name, string value)
                {
                    if (string.IsNullOrEmpty(name))
                    {
                        throw new ArgumentException($"“{nameof(name)}”不能为 null 或空。", nameof(name));
                    }

                    return AppendQueryString(string.Concat(name, "=", value ?? string.Empty));
                }

                public IRequestableBase AppendQueryString(string name, DateTime value, string dateFormatString = "yyyy-MM-dd HH:mm:ss.FFFFFFFK") => AppendQueryString(name, value.ToString(dateFormatString));

                public IRequestableBase AppendQueryString(string name, object value, string dateFormatString = "yyyy-MM-dd HH:mm:ss.FFFFFFFK")
                {
                    AppendTo(name, value, dateFormatString, false);

                    return this;
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

                public IRequestableBase AppendQueryString<TParam>(TParam param) where TParam : IEnumerable<KeyValuePair<string, object>> => AppendQueryString(param, "yyyy-MM-dd HH:mm:ss.FFFFFFFK");

                public IRequestableBase AppendQueryString<TParam>(TParam param, string dateFormatString) where TParam : IEnumerable<KeyValuePair<string, object>>
                {
                    if (param is null)
                    {
                        return this;
                    }

                    dateFormatString ??= "yyyy-MM-dd HH:mm:ss.FFFFFFFK";

                    foreach (var kv in param)
                    {
                        AppendTo(kv.Key, kv.Value, dateFormatString, false);
                    }

                    return this;
                }

                public IRequestableBase AppendQueryString<TParam>(TParam param, NamingType namingType = NamingType.UrlCase, string dateFormatString = "yyyy-MM-dd HH:mm:ss.FFFFFFFK") where TParam : class
                {
                    if (param is null)
                    {
                        return this;
                    }

                    dateFormatString ??= "yyyy-MM-dd HH:mm:ss.FFFFFFFK";

                    var results = lru.Get(typeof(TParam))
                         .Invoke(param, dateFormatString);

                    if (namingType == NamingType.Normal)
                    {
                        return AppendQueryString(results, dateFormatString);
                    }

                    return AppendQueryString(results.ConvertAll(x => new KeyValuePair<string, object>(x.Key.ToNamingCase(namingType), x.Value)), dateFormatString);
                }

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

                public RequestOptions GetOptions(RequestOptions options)
                {
                    return new RequestOptions(sb.ToString(), headers)
                    {
                        Method = options.Method,
                        Content = options.Content,
                        Timeout = options.Timeout
                    };
                }
            }
        }

        private class RequestableContent : RequestableString, IRequestableContent
        {
            private readonly RequestFactory factory;
            private readonly Encoding encoding;
            private readonly RequestOptions options;

            public RequestableContent(RequestFactory factory, Encoding encoding, RequestOptions options) : base(factory)
            {
                this.factory = factory;
                this.encoding = encoding;
                this.options = options;
            }

            public IThenRequestable TryThenAsync(Func<IRequestableBase, Task> thenAsync)
            {
                if (thenAsync is null)
                {
                    throw new ArgumentNullException(nameof(thenAsync));
                }

                return new ThenRequestable(factory, encoding, options, thenAsync);
            }

            public IJsonDeserializeRequestable<T> JsonCast<T>(NamingType namingType = NamingType.Normal) where T : class => new JsonDeserializeRequestable<T>(this, namingType);

            public IXmlDeserializeRequestable<T> XmlCast<T>() where T : class => new XmlDeserializeRequestable<T>(this, encoding);

            public IJsonDeserializeRequestable<T> JsonCast<T>(T anonymousTypeObject, NamingType namingType = NamingType.Normal) where T : class => JsonCast<T>(namingType);

            public IXmlDeserializeRequestable<T> XmlCast<T>(T anonymousTypeObject) where T : class => XmlCast<T>();

            protected override RequestOptions GetOptions(HttpMethod method, double timeout)
            {
                options.Method = method;
                options.Timeout = timeout;

                return options;
            }
        }

        private class RequestableDataVerify<T> : IRequestableDataVerify<T>
        {
            private readonly Requestable<T> requestable;
            private readonly List<Predicate<T>> predicates;

            public RequestableDataVerify(Requestable<T> requestable, Predicate<T> predicate)
            {
                this.requestable = requestable;
                predicates = new List<Predicate<T>> { predicate };
            }

            public IRequestableDataVerify<T> And(Predicate<T> predicate)
            {
                if (predicate is null)
                {
                    throw new ArgumentNullException(nameof(predicate));
                }

                predicates.Add(predicate);

                return this;
            }

            public IRequestableDataVerifyFail<T> Fail(Func<T, Exception> throwError)
            {
                if (throwError is null)
                {
                    throw new ArgumentNullException(nameof(throwError));
                }

                return new RequestableDataVerifyFail<T>(requestable, predicates, throwError);
            }
        }

        private class RequestableDataVerifyFail<T> : Requestable<T>, IRequestableDataVerifyFail<T>
        {
            private readonly Requestable<T> requestable;
            private readonly List<Predicate<T>> predicates;
            private readonly Func<T, Exception> throwError;

            public RequestableDataVerifyFail(Requestable<T> requestable, List<Predicate<T>> predicates, Func<T, Exception> throwError)
            {
                this.requestable = requestable;
                this.predicates = predicates;
                this.throwError = throwError;
            }

            public IRequestableDataVerifySuccess<T, TResult> Success<TResult>(Func<T, TResult> dataSuccess)
            {
                if (dataSuccess is null)
                {
                    throw new ArgumentNullException(nameof(dataSuccess));
                }

                return new RequestableDataVerifySuccess<T, TResult>(requestable, predicates, dataSuccess, throwError);
            }

            public override async Task<T> SendAsync(HttpMethod method, double timeout = 1000D, CancellationToken cancellationToken = default)
            {
                var data = await requestable.SendAsync(method, timeout, cancellationToken);

                if (predicates.TrueForAll(x => x.Invoke(data)))
                {
                    return data;
                }

                throw throwError.Invoke(data);
            }
        }

        private class RequestableDataVerifySuccess<T, TResult> : Requestable<TResult>, IRequestableDataVerifySuccess<T, TResult>
        {
            private readonly Requestable<T> requestable;
            private readonly List<Predicate<T>> predicates;
            private readonly Func<T, TResult> dataSuccess;
            private readonly Func<T, Exception> throwError;

            public RequestableDataVerifySuccess(Requestable<T> requestable, List<Predicate<T>> predicates, Func<T, TResult> dataSuccess, Func<T, Exception> throwError)
            {
                this.requestable = requestable;
                this.predicates = predicates;
                this.dataSuccess = dataSuccess;
                this.throwError = throwError;
            }

            public override async Task<TResult> SendAsync(HttpMethod method, double timeout = 1000, CancellationToken cancellationToken = default)
            {
                var data = await requestable.SendAsync(method, timeout, cancellationToken);

                if (predicates.TrueForAll(x => x.Invoke(data)))
                {
                    return dataSuccess.Invoke(data);
                }

                throw throwError.Invoke(data);
            }
        }

        private class JsonDeserializeRequestable<T> : Requestable<T>, IRequestableExtend<T>, IJsonDeserializeRequestable<T>
        {
            private Func<string, Exception, T> returnValue;
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

            public IRequestableExtend<T> JsonCatch(Func<string, Exception, T> returnValue)
            {
                if (returnValue is null)
                {
                    throw new ArgumentNullException(nameof(returnValue));
                }

                this.returnValue = returnValue;

                return this;
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

            public override async Task<T> SendAsync(HttpMethod method, double timeout = 1000D, CancellationToken cancellationToken = default)
            {
                var stringMsg = await requestable.SendAsync(method, timeout, cancellationToken);

                if (returnValue is null)
                {
                    return JsonHelper.Json<T>(stringMsg, namingType);
                }

                try
                {
                    return JsonHelper.Json<T>(stringMsg, namingType);
                }
                catch (Exception ex) when (IsJsonError(ex))
                {
                    return returnValue.Invoke(stringMsg, ex);
                }
            }
        }

        private class XmlDeserializeRequestable<T> : Requestable<T>, IRequestableExtend<T>, IXmlDeserializeRequestable<T>
        {
            private Func<string, XmlException, T> returnValue;
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

                if (returnValue is null)
                {
                    return XmlHelper.XmlDeserialize<T>(stringMsg, encoding);
                }

                try
                {
                    return XmlHelper.XmlDeserialize<T>(stringMsg, encoding);
                }
                catch (XmlException ex)
                {
                    return returnValue.Invoke(stringMsg, ex);
                }
            }

            public IRequestableExtend<T> XmlCatch(Func<string, XmlException, T> returnValue)
            {
                if (returnValue is null)
                {
                    throw new ArgumentNullException(nameof(returnValue));
                }

                this.returnValue = returnValue;

                return this;
            }
        }

        /// <summary>
        /// 创建请求能力。
        /// </summary>
        /// <param name="requestUri">请求地址。</param>
        /// <returns>请求能力。</returns>
        public IRequestable Create(string requestUri)
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

            mediaTypes[mediaType.ToLower()] = new MediaTypeHeaderValue(mediaType);
        }

        /// <summary>
        /// 初始化。
        /// </summary>
        /// <param name="requestable">基础请求能力。</param>
        protected virtual void Initialize(IRequestableBase requestable)
        {
        }

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

            using (var httpMsg = new HttpRequestMessage(options.Method, options.RequestUri)
            {
                Content = options.Content
            })
            {
                if (options.Headers.Count > 0)
                {
                    foreach (var kv in options.Headers)
                    {
                        httpMsg.Headers.Add(kv.Key, kv.Value);
                    }
                }

                return await client.SendAsync(httpMsg, cancellationToken);
            }
        }
    }
}
