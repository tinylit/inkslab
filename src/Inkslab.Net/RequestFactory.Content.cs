using Inkslab.Net.Options;
using Inkslab.Serialize.Json;
using Inkslab.Serialize.Xml;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace Inkslab.Net
{
    public partial class RequestFactory
    {
        private interface IToContent
        {
            HttpContent Content { get; }
        }

        private abstract class RequestableEncoding : RequestableString, IRequestableEncoding
        {
            private readonly Encoding _encoding;

            private class ToContentByBody : IToContent
            {
                private readonly Encoding _encoding;
                private readonly string _body;
                private readonly string _contentType;

                public ToContentByBody(Encoding encoding, string body, string contentType)
                {
                    _encoding = encoding;
                    _body = body;
                    _contentType = contentType;
                }

                public HttpContent Content => new StringContent(_body, _encoding, _contentType);
            }

            private class ToContentByOriginal : IToContent
            {
                private readonly HttpContent _body;

                public ToContentByOriginal(HttpContent body)
                {
                    _body = body;
                }

                public HttpContent Content => _body;
            }

            private class ToContentByStringValue : IToContent
            {
                private readonly IEnumerable<KeyValuePair<string, string>> _body;

                public ToContentByStringValue(IEnumerable<KeyValuePair<string, string>> body)
                {
                    _body = body;
                }

                public HttpContent Content => new FormUrlEncodedContent(_body);
            }

            private class ToContentByForm<TBody> : IToContent where TBody : IEnumerable<KeyValuePair<string, object>>
            {
                private readonly Encoding _encoding;
                private readonly TBody _body;
                private readonly string _dateFormatString;

                public ToContentByForm(Encoding encoding, TBody body, string dateFormatString)
                {
                    _encoding = encoding;
                    _body = body;
                    _dateFormatString = dateFormatString ?? "yyyy-MM-dd HH:mm:ss.FFFFFFFK";
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
                    else if (_mediaTypes.TryGetValue(extension.ToLower(), out MediaTypeHeaderValue mediaType))
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
                        case IEnumerable enumerableValue:
                            if (throwErrorsIfEnumerable)
                            {
                                throw new InvalidOperationException("不支持多维数组的参数传递!");
                            }

                            foreach (var itemValue in enumerableValue)
                            {
                                AppendToForm(content, encoding, name, itemValue, dateFormatString, true);
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
                        if (_body.Any(x => x.Value is FileInfo or IEnumerable<FileInfo>))
                        {
                            var content = new MultipartFormDataContent(string.Concat("--", DateTime.Now.Ticks.ToString("x")));

                            foreach (var kv in _body)
                            {
                                AppendToForm(content, _encoding, kv.Key, kv.Value, _dateFormatString, false);
                            }

                            return content;
                        }
                        else
                        {
                            var content = new FormUrlEncodedContent(_body.Select(x =>
                            {
                                return x.Value switch
                                {
                                    string text => new KeyValuePair<string, string>(x.Key, text),
                                    DateTime date => new KeyValuePair<string, string>(x.Key, date.ToString(_dateFormatString)),
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
                _encoding = encoding;
            }

            public IRequestableContent Body(string body, string contentType) => new RequestableContent(this, _encoding, new ToContentByBody(_encoding, body, contentType));
            public IRequestableContent Form(MultipartFormDataContent body) => new RequestableContent(this, _encoding, new ToContentByOriginal(body));

            public IRequestableContent Form(FormUrlEncodedContent body) => new RequestableContent(this, _encoding, new ToContentByOriginal(body));

            public IRequestableContent Form<TBody>(TBody body) where TBody : IEnumerable<KeyValuePair<string, string>> => new RequestableContent(this, _encoding, new ToContentByStringValue(body));

            public IRequestableContent Form<TBody>(TBody body, string dateFormatString) where TBody : IEnumerable<KeyValuePair<string, object>> => new RequestableContent(this, _encoding, new ToContentByForm<TBody>(_encoding, body, dateFormatString));

            public IRequestableContent Form(object body, NamingType namingType, string dateFormatString = "yyyy-MM-dd HH:mm:ss.FFFFFFFK")
            {
                if (body is null)
                {
                    return this;
                }

                dateFormatString ??= "yyyy-MM-dd HH:mm:ss.FFFFFFFK";

                var results = _cachings.GetOrAdd(body.GetType(), MakeTypeResults)
                    .Invoke(body);

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

            public IRequestableContent Xml<T>(T param) where T : class => Xml(XmlHelper.XmlSerialize(param, _encoding));

            public IXmlDeserializeRequestable<T> XmlCast<T>() where T : class => new XmlDeserializeRequestable<T>(this, _encoding);

            public IXmlDeserializeRequestable<T> XmlCast<T>(T anonymousTypeObject) where T : class => new XmlDeserializeRequestable<T>(this, _encoding);

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
