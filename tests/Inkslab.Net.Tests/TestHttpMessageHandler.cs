using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Inkslab.Net.Options;

namespace Inkslab.Net.Tests
{
    internal sealed class TestHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _send;
        public TestHttpMessageHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send) => _send = send;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token) => _send(request, token);
        public static HttpResponseMessage Response(string body = "ok", HttpStatusCode status = HttpStatusCode.OK)
            => new HttpResponseMessage(status) { Content = new StringContent(body) };
        public HttpClient CreateClient() => new HttpClient(this, false) { Timeout = Timeout.InfiniteTimeSpan };
    }

    // Tests for request composition/retry hooks use the existing protected override only.
    // Transport cancellation and lifetime assertions use TCP loopback tests instead.
    internal sealed class TestRequestFactory : RequestFactory
    {
        private readonly HttpClient _client;
        public TestRequestFactory(HttpClient client) => _client = client;
        protected override async Task<HttpResponseMessage> SendAsync(RequestOptions options, CancellationToken token)
        {
            using var request = new HttpRequestMessage(options.Method, options.RequestUri);
            foreach (var header in options.Headers)
            {
                if (options.SkipValidationHeaders.Contains(header.Key)) { request.Headers.TryAddWithoutValidation(header.Key, header.Value); }
                else { request.Headers.Add(header.Key, header.Value); }
            }
            request.Content = options.Content;
            return await _client.SendAsync(request, options.CompletionOption, token);
        }
    }
}
