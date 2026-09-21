using Inkslab.Serialize.Json;
using Inkslab.Serialize.Xml;
using Inkslab.Net.Validation;
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

namespace Inkslab.Net
{
    public partial class RequestFactory
    {
        private interface IToContent
        {
            bool CanReplay { get; }
            HttpContent CreateContent();
        }

        private abstract class RequestableEncoding : RequestableString, IRequestableEncoding
        {
            private readonly Encoding _encoding;

            private sealed class ToContentByBody : IToContent
            {
                private readonly Encoding _encoding;
                private readonly string _body;
                private readonly string _contentType;
                public ToContentByBody(Encoding encoding, string body, string contentType)
                { _encoding = encoding; _body = body; _contentType = contentType; }
                public bool CanReplay => true;
                public HttpContent CreateContent() => new StringContent(_body, _encoding, _contentType);
            }

            private sealed class ToContentByOriginal : IToContent
            {
                private HttpContent _body;
                public ToContentByOriginal(HttpContent body) => _body = body ?? throw new ArgumentNullException(nameof(body));
                public bool CanReplay => false;
                public HttpContent CreateContent() => Interlocked.Exchange(ref _body, null)
                    ?? throw new InvalidOperationException("The request content has already been consumed.");
            }

            private sealed class ToContentByStream : IToContent
            {
                private Stream _stream;
                private readonly MediaTypeHeaderValue _mediaType;
                private readonly bool _leaveOpen;
                public ToContentByStream(Stream stream, string contentType, bool leaveOpen)
                {
                    if (stream is null) { throw new ArgumentNullException(nameof(stream)); }
                    if (!stream.CanRead) { throw new ArgumentException("The stream must be readable.", nameof(stream)); }
                    if (string.IsNullOrWhiteSpace(contentType)) { throw new ArgumentException("A media type is required.", nameof(contentType)); }
                    _mediaType = MediaTypeHeaderValue.Parse(contentType);
                    _stream = stream;
                    _leaveOpen = leaveOpen;
                }
                public bool CanReplay => false;
                public HttpContent CreateContent()
                {
                    var stream = Interlocked.Exchange(ref _stream, null)
                        ?? throw new InvalidOperationException("The request stream has already been consumed.");
                    HttpContent content = null;
                    try
                    {
                        content = new UploadStreamContent(stream, ownsStream: !_leaveOpen);
                        content.Headers.ContentType = _mediaType;
                        return content;
                    }
                    catch
                    {
                        if (content != null) { content.Dispose(); }
                        else if (!_leaveOpen) { stream.Dispose(); }
                        throw;
                    }
                }
            }

            private sealed class ToContentByStringValue : IToContent
            {
                private readonly List<KeyValuePair<string, string>> _body;
                public ToContentByStringValue(IEnumerable<KeyValuePair<string, string>> body)
                    => _body = new List<KeyValuePair<string, string>>(body ?? throw new ArgumentNullException(nameof(body)));
                public bool CanReplay => true;
                public HttpContent CreateContent() => new FormUrlEncodedContent(_body);
            }

            private sealed class ToContentByForm<TBody> : IToContent where TBody : IEnumerable<KeyValuePair<string, object>>
            {
                private readonly Encoding _encoding;
                private readonly List<KeyValuePair<string, object>> _body = new List<KeyValuePair<string, object>>();
                private readonly string _dateFormatString;
                private readonly bool _multipart;
                private int _claimed;
                public bool CanReplay { get; } = true;

                public ToContentByForm(Encoding encoding, TBody body, string dateFormatString)
                {
                    if (body is null) { throw new ArgumentNullException(nameof(body)); }
                    _encoding = encoding;
                    _dateFormatString = dateFormatString ?? "yyyy-MM-dd HH:mm:ss.FFFFFFFK";
                    // Snapshot each field/enumeration once, without opening or reading streams.
                    foreach (var kv in body)
                    {
                        if (kv.Value is IEnumerable items && !(kv.Value is string) && !(kv.Value is byte[]))
                        {
                            foreach (var item in items)
                            {
                                if (item is IEnumerable && !(item is string) && !(item is byte[]))
                                { throw new InvalidOperationException("Nested form collections are not supported."); }
                                _body.Add(new KeyValuePair<string, object>(kv.Key, item));
                            }
                        }
                        else { _body.Add(kv); }
                    }
                    _multipart = _body.Any(kv => kv.Value is FileInfo || kv.Value is Stream);
                    CanReplay = !_body.Any(kv => kv.Value is Stream);
                }

                private string Format(object value) => value switch
                {
                    DateTime date => date.ToString(_dateFormatString),
                    byte[] buffer => System.Convert.ToBase64String(buffer),
                    _ => value?.ToString()
                };

                public HttpContent CreateContent()
                {
                    if (!CanReplay && Interlocked.Exchange(ref _claimed, 1) != 0)
                    { throw new InvalidOperationException("The form contains a consumed stream."); }
                    if (!_multipart)
                    { return new FormUrlEncodedContent(_body.Select(kv => new KeyValuePair<string, string>(kv.Key, Format(kv.Value)))); }
                    var multipart = new MultipartFormDataContent();
                    var pendingStreams = new HashSet<Stream>(_body.Select(kv => kv.Value).OfType<Stream>());
                    try
                    {
                        foreach (var kv in _body)
                        {
                            HttpContent part = null;
                            try
                            {
                                switch (kv.Value)
                                {
                                    case FileInfo file:
                                        part = new UploadStreamContent(file.Open(FileMode.Open, FileAccess.Read, FileShare.Read), ownsStream: true);
                                        part.Headers.ContentType = _mediaTypes.TryGetValue(file.Extension, out var mediaType)
                                            ? MediaTypeHeaderValue.Parse(mediaType.ToString()) : new MediaTypeHeaderValue("application/octet-stream");
                                        multipart.Add(part, kv.Key, file.Name);
                                        break;
                                    case Stream stream:
                                        part = new UploadStreamContent(stream, ownsStream: true);
                                        pendingStreams.Remove(stream);
                                        multipart.Add(part, kv.Key);
                                        break;
                                    case null:
                                        break;
                                    default:
                                        part = new StringContent(Format(kv.Value), _encoding);
                                        multipart.Add(part, kv.Key);
                                        break;
                                }
                            }
                            catch { part?.Dispose(); throw; }
                        }
                        return multipart;
                    }
                    catch
                    {
                        multipart.Dispose();
                        foreach (var stream in pendingStreams) { stream.Dispose(); }
                        throw;
                    }
                }
            }
            public RequestableEncoding(Encoding encoding)
            {
                _encoding = encoding;
            }

            public IRequestableContent Body(string body, string contentType) => new RequestableContent(this, _encoding, new ToContentByBody(_encoding, body, contentType));
            public IRequestableContent Content(HttpContent content) => new RequestableContent(this, _encoding, new ToContentByOriginal(content));
            public IRequestableContent Stream(Stream stream, string contentType = "application/octet-stream", bool leaveOpen = false) => new RequestableContent(this, _encoding, new ToContentByStream(stream, contentType, leaveOpen));
            public IRequestableContent Form(MultipartFormDataContent body) => Content(body);

            public IRequestableContent Form(FormUrlEncodedContent body) => Content(body);

            public IRequestableContent Form<TBody>(TBody body) where TBody : IEnumerable<KeyValuePair<string, string>> => new RequestableContent(this, _encoding, new ToContentByStringValue(body));

            public IRequestableContent Form<TBody>(TBody body, string dateFormatString) where TBody : IEnumerable<KeyValuePair<string, object>> => new RequestableContent(this, _encoding, new ToContentByForm<TBody>(_encoding, body, dateFormatString));

            public IRequestableContent Form(object body, NamingType namingType, string dateFormatString = "yyyy-MM-dd HH:mm:ss.FFFFFFFK")
            {
                EntityValidator.ValidateInput(body);
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

            public IRequestableContent Json<T>(T param, NamingType namingType = NamingType.Normal) where T : class
            {
                EntityValidator.ValidateInput(param, typeof(T));
                return Json(JsonHelper.ToJson(param, namingType));
            }

            public IJsonDeserializeRequestable<T> JsonCast<T>(NamingType namingType = NamingType.Normal) where T : class => new JsonDeserializeRequestable<T>(this, namingType);

            public IJsonDeserializeRequestable<T> JsonCast<T>(T anonymousTypeObject, NamingType namingType = NamingType.Normal) where T : class => new JsonDeserializeRequestable<T>(this, namingType);

            public IRequestableContent Xml(string xml) => Body(xml, "application/xml");

            public IRequestableContent Xml<T>(T param) where T : class
            {
                EntityValidator.ValidateInput(param, typeof(T));
                return Xml(XmlHelper.XmlSerialize(param, _encoding));
            }

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
