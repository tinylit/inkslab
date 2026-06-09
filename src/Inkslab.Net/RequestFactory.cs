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
    /// 请求工厂。
    /// </summary>
    public partial class RequestFactory : IRequestFactory
    {
        private static readonly RequestFactory _factory = new RequestFactory();

        private static readonly Type _kvType = typeof(KeyValuePair<string, object>);
        private static readonly ConstructorInfo _kvCtor = _kvType.GetConstructor(new Type[] { typeof(string), typeof(object) });

        private static readonly Type _listKvType = typeof(List<KeyValuePair<string, object>>);
        private static readonly ConstructorInfo _listKvCtor = _listKvType.GetConstructor(new Type[] { typeof(int) });
        private static readonly MethodInfo _listKvAddFn = _listKvType.GetMethod("Add", new Type[] { _kvType });

        private static readonly Type _dateType = typeof(DateTime);

#if NET_Traditional
        private static readonly Lfu<double, HttpClient> _clients = new Lfu<double, HttpClient>(100, timeout => new HttpClient
        {
            Timeout = TimeSpan.FromMilliseconds(timeout)
        });
#else
        private static readonly Lfu<double, HttpClient> _clients = new Lfu<double, HttpClient>(100, timeout => new HttpClient(new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        })
        {
            Timeout = TimeSpan.FromMilliseconds(timeout)
        });
#endif
        private static readonly ConcurrentDictionary<Type, Func<object, List<KeyValuePair<string, object>>>> _cachings = new ConcurrentDictionary<Type, Func<object, List<KeyValuePair<string, object>>>>();

        private static readonly Dictionary<string, MediaTypeHeaderValue> _mediaTypes = new Dictionary<string, MediaTypeHeaderValue>
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

        private readonly IRequestInitialize _initialize;

        private static Func<object, List<KeyValuePair<string, object>>> MakeTypeResults(Type type)
        {
            var objectType = typeof(object);

            var objectExp = Parameter(objectType, "param");
            var variableExp = Variable(type, "variable");
            var dictionaryExp = Variable(_listKvType, "dictionary");

            var propertyInfos = Array.FindAll(type.GetProperties(), x => x.CanRead);

            var expressions = new List<Expression>
            {
                Assign(variableExp, Convert(objectExp, type)),
                Assign(dictionaryExp, New(_listKvCtor, Constant(propertyInfos.Length)))
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

                if (propertyType == _dateType)
                {
                    valueExp = Convert(valueExp, objectType);
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

                var bodyCallExp = Call(dictionaryExp, _listKvAddFn, New(_kvCtor, Constant(propertyInfo.Name), valueExp));

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

            var lambdaExp = Lambda<Func<object, List<KeyValuePair<string, object>>>>(bodyExp, objectExp);

            return lambdaExp.Compile();
        }

        private class JsonDeserializeRequestable<T> : Requestable<T>, IJsonDeserializeRequestable<T>, IRequestableExtend<T>
        {
            private readonly RequestableString _requestable;
            private readonly NamingType _namingType;

            public JsonDeserializeRequestable(RequestableString requestable, NamingType namingType)
            {
                _requestable = requestable;
                _namingType = namingType;
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

                return new JsonDeserializeRequestableCatch<T>(_requestable, _namingType, abnormalResultAnalysis);
            }

            public override async Task<T> SendAsync(HttpMethod method, double timeout = 1000D, CancellationToken cancellationToken = default)
            {
                var stringMsg = await _requestable.SendAsync(method, timeout, cancellationToken);

                return JsonHelper.Json<T>(stringMsg, _namingType);
            }
        }

        private class JsonDeserializeRequestableCatch<T> : JsonDeserializeRequestable<T>, IRequestableExtend<T>
        {
            private readonly Func<Exception, T> _abnormalResultAnalysis;

            public JsonDeserializeRequestableCatch(RequestableString requestable, NamingType namingType, Func<Exception, T> abnormalResultAnalysis) : base(requestable, namingType)
            {
                _abnormalResultAnalysis = abnormalResultAnalysis;
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
                    return _abnormalResultAnalysis.Invoke(ex);
                }
            }
        }

        private class XmlDeserializeRequestable<T> : Requestable<T>, IXmlDeserializeRequestable<T>, IRequestableExtend<T>
        {
            private readonly RequestableString _requestable;
            private readonly Encoding _encoding;

            public XmlDeserializeRequestable(RequestableString requestable, Encoding encoding)
            {
                _requestable = requestable;
                _encoding = encoding;
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
                var stringMsg = await _requestable.SendAsync(method, timeout, cancellationToken);

                return XmlHelper.XmlDeserialize<T>(stringMsg, _encoding);
            }

            public IRequestableExtend<T> XmlCatch(Func<XmlException, T> abnormalResultAnalysis)
            {
                if (abnormalResultAnalysis is null)
                {
                    throw new ArgumentNullException(nameof(abnormalResultAnalysis));
                }

                return new XmlDeserializeRequestableCatch<T>(_requestable, _encoding, abnormalResultAnalysis);
            }
        }

        private class XmlDeserializeRequestableCatch<T> : XmlDeserializeRequestable<T>, IRequestableExtend<T>
        {
            private readonly Func<XmlException, T> _abnormalResultAnalysis;

            public XmlDeserializeRequestableCatch(RequestableString requestable, Encoding encoding, Func<XmlException, T> abnormalResultAnalysis) : base(requestable, encoding)
            {
                _abnormalResultAnalysis = abnormalResultAnalysis;
            }

            public override async Task<T> SendAsync(HttpMethod method, double timeout = 1000, CancellationToken cancellationToken = default)
            {
                try
                {
                    return await base.SendAsync(method, timeout, cancellationToken);
                }
                catch (XmlException ex)
                {
                    return _abnormalResultAnalysis.Invoke(ex);
                }
            }
        }

        private class CustomDeserializeRequestable<T> : Requestable<T>, ICustomDeserializeRequestable<T>, IRequestableExtend<T>
        {
            private readonly RequestableString _requestable;
            private readonly Func<HttpResponseMessage, CancellationToken, Task<T>> _customFactory;

            public CustomDeserializeRequestable(RequestableString requestable, Func<HttpResponseMessage, CancellationToken, Task<T>> customFactory)
            {
                _requestable = requestable;
                _customFactory = customFactory;
            }

            public IRequestableExtend<T> Catch(Func<Exception, T> abnormalResultAnalysis)
            {
                if (abnormalResultAnalysis is null)
                {
                    throw new ArgumentNullException(nameof(abnormalResultAnalysis));
                }

                return new CustomDeserializeRequestableCatch<T>(_requestable, _customFactory, abnormalResultAnalysis);
            }

            public IRequestableDataVerify<T> DataVerify(Predicate<T> dataVerify)
            {
                if (dataVerify is null)
                {
                    throw new ArgumentNullException(nameof(dataVerify));
                }

                return new RequestableDataVerify<T>(this, dataVerify);
            }

            public override async Task<T> SendAsync(HttpMethod method, double timeout = 1000, CancellationToken cancellationToken = default)
            {
                using var httpMsg = await _requestable.PrimitiveSendAsync(method, timeout, cancellationToken);

                return await _customFactory.Invoke(httpMsg, cancellationToken);
            }
        }

        private class CustomDeserializeRequestableCatch<T> : CustomDeserializeRequestable<T>, IRequestableExtend<T>
        {
            private readonly Func<Exception, T> _abnormalResultAnalysis;

            public CustomDeserializeRequestableCatch(RequestableString requestable, Func<HttpResponseMessage, CancellationToken, Task<T>> customFactory, Func<Exception, T> abnormalResultAnalysis) : base(requestable, customFactory)
            {
                _abnormalResultAnalysis = abnormalResultAnalysis;
            }

            public override async Task<T> SendAsync(HttpMethod method, double timeout = 1000, CancellationToken cancellationToken = default)
            {
                try
                {
                    return await base.SendAsync(method, timeout, cancellationToken);
                }
                catch (Exception ex)
                {
                    return _abnormalResultAnalysis.Invoke(ex);
                }
            }
        }

        private class CustomByStringDeserializeRequestable<T> : Requestable<T>, ICustomDeserializeRequestable<T>, IRequestableExtend<T>
        {
            private readonly RequestableString _requestable;
            private readonly Func<string, T> _customFactory;

            public CustomByStringDeserializeRequestable(RequestableString requestable, Func<string, T> customFactory)
            {
                _requestable = requestable;
                _customFactory = customFactory;
            }

            public IRequestableExtend<T> Catch(Func<Exception, T> abnormalResultAnalysis)
            {
                if (abnormalResultAnalysis is null)
                {
                    throw new ArgumentNullException(nameof(abnormalResultAnalysis));
                }

                return new CustomByStringDeserializeRequestableCatch<T>(_requestable, _customFactory, abnormalResultAnalysis);
            }

            public IRequestableDataVerify<T> DataVerify(Predicate<T> dataVerify)
            {
                if (dataVerify is null)
                {
                    throw new ArgumentNullException(nameof(dataVerify));
                }

                return new RequestableDataVerify<T>(this, dataVerify);
            }

            public override async Task<T> SendAsync(HttpMethod method, double timeout = 1000, CancellationToken cancellationToken = default)
            {
                var httpMsg = await _requestable.SendAsync(method, timeout, cancellationToken);

                return _customFactory.Invoke(httpMsg);
            }
        }

        private class CustomByStringDeserializeRequestableCatch<T> : CustomByStringDeserializeRequestable<T>, IRequestableExtend<T>
        {
            private readonly Func<Exception, T> _abnormalResultAnalysis;

            public CustomByStringDeserializeRequestableCatch(RequestableString requestable, Func<string, T> customFactory, Func<Exception, T> abnormalResultAnalysis) : base(requestable, customFactory)
            {
                _abnormalResultAnalysis = abnormalResultAnalysis;
            }

            public override async Task<T> SendAsync(HttpMethod method, double timeout = 1000, CancellationToken cancellationToken = default)
            {
                try
                {
                    return await base.SendAsync(method, timeout, cancellationToken);
                }
                catch (Exception ex)
                {
                    return _abnormalResultAnalysis.Invoke(ex);
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
            _initialize = initialize ?? throw new ArgumentNullException(nameof(initialize));
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

            _mediaTypes[fileSuffix.ToLower()] = new MediaTypeHeaderValue(mediaType);
        }

        /// <summary>
        /// 初始化。
        /// </summary>
        /// <param name="requestable">基础请求能力。</param>
        protected virtual void Initialize(IRequestableBase requestable) => _initialize.Initialize(requestable);

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

            var client = _clients.Get(options.Timeout);

            using (var httpMsg = new HttpRequestMessage(options.Method, options.RequestUri))
            {
                httpMsg.Content = options.Content;

                if (options.Headers.Count > 0)
                {
                    foreach (var kv in options.Headers)
                    {
                        if (options.SkipValidationHeaders.Contains(kv.Key))
                        {
                            httpMsg.Headers.TryAddWithoutValidation(kv.Key, kv.Value);
                        }
                        else
                        {
                            httpMsg.Headers.Add(kv.Key, kv.Value);
                        }
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
        public static IRequestable Create(string requestUri) => _factory.CreateRequestable(requestUri);
    }
}