using Inkslab.Serialize.Json;
using Inkslab.Serialize.Xml;
using System;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace Inkslab.Net
{
    public partial class RequestFactory
    {
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
    }
}
