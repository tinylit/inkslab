#pragma warning disable CS1591
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Inkslab.Net.Options;
using Xunit;
namespace Inkslab.Net.Tests
{
    public class FactoryOverrideCompatibilityTests
    {
        [Theory]
        [InlineData("body", true)] [InlineData("json", true)] [InlineData("form", true)] [InlineData("content", true)]
        [InlineData("body", false)] [InlineData("json", false)] [InlineData("form", false)] [InlineData("content", false)]
        public async Task OverrideSeesContentAndFreshRetryInstancesAsync(string kind, bool forward)
        {
            int sends = 0;
            string expected = kind == "form" ? "key=value" : "payload";
            using var server = new LoopbackHttpServer(async (headers, network, token) =>
            {
                sends++; Assert.Equal(expected, await LoopbackHttpServer.ReadBodyAsync(headers, network, token));
                await LoopbackHttpServer.RespondAsync(network, token, "ok", kind != "content" && sends % 2 == 1 ? 401 : 200);
            }, kind == "content" ? 1 : 4);
            using var client = new HttpClient();
            var factory = new ObservingFactory(client, forward);
            var root = factory.CreateRequestable(server.Url);
            var content = kind switch
            {
                "body" => root.Body("payload", "text/plain"),
                "json" => root.Json("payload"),
                "form" => root.Form(new Dictionary<string, string> { ["key"] = "value" }),
                _ => root.Content(new StringContent("payload"))
            };
            var request = content.When(s => s == HttpStatusCode.Unauthorized).ThenAsync(_ => Task.CompletedTask);
            Assert.Equal("ok", await request.PostAsync());
            if (kind == "content")
            {
                await Assert.ThrowsAsync<InvalidOperationException>(() => request.PostAsync());
                Assert.Equal(1, sends);
            }
            else
            {
                Assert.Equal("ok", await request.PostAsync());
                Assert.Equal(4, sends);
                Assert.Equal(4, new HashSet<HttpContent>(factory.Contents).Count);
                Assert.Equal(4, new HashSet<RequestOptions>(factory.Options).Count);
            }
            Assert.All(factory.Bodies, body => Assert.Equal(expected, body));
            await server.Completion;
        }
        [Fact]
        public async Task PreCanceled_DoesNotEnterOverrideOrClaimStreamAsync()
        {
            using var handler = new TestHttpMessageHandler((r, t) => throw new InvalidOperationException());
            using var client = handler.CreateClient(); using var source = new TrackingStream("data");
            var factory = new ObservingFactory(client, true);
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => factory.CreateRequestable("https://unit.test/").Stream(source).PostAsync(cancellationToken: new CancellationToken(true)));
            Assert.Empty(factory.Options); Assert.False(source.Disposed);
        }
        [Fact]
        public async Task OverrideClaimsThenThrows_DisposesContentAsync()
        {
            using var source = new TrackingStream("data");
            var request = new ThrowingFactory().CreateRequestable("https://unit.test/").Stream(source);
            await Assert.ThrowsAsync<IOException>(() => request.PostAsync());
            Assert.True(source.Disposed);
            await Assert.ThrowsAsync<InvalidOperationException>(() => request.PostAsync());
        }
        private sealed class ThrowingFactory : RequestFactory
        {
            protected override Task<HttpResponseMessage> SendAsync(RequestOptions options, CancellationToken token)
            {
                Assert.NotNull(options.Content);
                throw new IOException("override failed after claiming content");
            }
        }
        private sealed class ObservingFactory : RequestFactory
        {
            private readonly HttpClient _client; private readonly bool _forward;
            public List<HttpContent> Contents { get; } = new();
            public List<RequestOptions> Options { get; } = new();
            public List<string> Bodies { get; } = new();
            public ObservingFactory(HttpClient client, bool forward) { _client = client; _forward = forward; }
            protected override async Task<HttpResponseMessage> SendAsync(RequestOptions options, CancellationToken token)
            {
                Options.Add(options);
                Assert.NotNull(options.Content);
                Assert.Same(options.Content, options.Content);
                Contents.Add(options.Content);
                Bodies.Add(await options.Content.ReadAsStringAsync(token));
                if (_forward) { return await base.SendAsync(options, token); }
                using var request = new HttpRequestMessage(options.Method, options.RequestUri) { Content = options.Content };
                return await _client.SendAsync(request, options.CompletionOption, token);
            }
        }
    }
}
