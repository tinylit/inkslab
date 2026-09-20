#pragma warning disable CS1591
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Inkslab.Net.Tests
{
    public class ContentReplayTests
    {
        [Fact]
        public async Task OriginalContent_ConfigurationDoesNotReadAsync()
        {
            using var source = new TrackingStream("payload", false);
            using var handler = new TestHttpMessageHandler(async (r, t) => TestHttpMessageHandler.Response(await r.Content.ReadAsStringAsync(t)));
            using var client = handler.CreateClient();
            var request = new TestRequestFactory(client).CreateRequestable("https://unit.test/").Content(new StreamContent(source));
            Assert.Equal(0, source.Reads);
            Assert.False(source.Disposed);
            Assert.Equal("payload", await request.PostAsync());
            Assert.True(source.Disposed);
        }
        [Theory]
        [InlineData(0)] [InlineData(1)] [InlineData(2)] [InlineData(3)]
        public async Task OriginalContent_RejectsSecondSendAsync(int kind)
        {
            int sends = 0;
            using var handler = new TestHttpMessageHandler(async (r, t) => { sends++; await r.Content.ReadAsStringAsync(t); return TestHttpMessageHandler.Response(); });
            using var client = handler.CreateClient();
            var root = new TestRequestFactory(client).CreateRequestable("https://unit.test/");
            var request = kind switch
            {
                0 => root.Content(new StringContent("payload")),
                1 => root.Stream(new MemoryStream(new byte[] { 1 })),
                2 => root.Form(new FormUrlEncodedContent(new Dictionary<string, string> { ["a"] = "b" })),
                _ => root.Form(new MultipartFormDataContent { new StringContent("payload") })
            };
            Assert.Equal("ok", await request.PostAsync());
            await Assert.ThrowsAsync<InvalidOperationException>(() => request.PostAsync());
            Assert.Equal(1, sends);
        }
        [Fact]
        public async Task NonReplayableContent_RejectsRetryBeforeThenAsync()
        {
            int sends = 0, thens = 0;
            using var handler = new TestHttpMessageHandler((r, t) => { sends++; return Task.FromResult(TestHttpMessageHandler.Response("no", HttpStatusCode.Unauthorized)); });
            using var client = handler.CreateClient();
            using var source = new TrackingStream("payload");
            var request = new TestRequestFactory(client).CreateRequestable("https://unit.test/")
                .Stream(source).When(s => s == HttpStatusCode.Unauthorized).ThenAsync(_ => { thens++; return Task.CompletedTask; });
            await Assert.ThrowsAsync<InvalidOperationException>(() => request.PostAsync());
            Assert.Equal(1, sends); Assert.Equal(0, thens); Assert.True(source.Disposed);
        }
        [Fact]
        public async Task ReplayableChain_ResetsStrategiesForEachCallAsync()
        {
            var bodies = new List<string>();
            using var handler = new TestHttpMessageHandler(async (r, t) => { bodies.Add(await r.Content.ReadAsStringAsync(t)); return TestHttpMessageHandler.Response("ok", bodies.Count % 2 == 1 ? HttpStatusCode.Unauthorized : HttpStatusCode.OK); });
            using var client = handler.CreateClient();
            var request = new TestRequestFactory(client).CreateRequestable("https://unit.test/")
                .Body("payload", "text/plain").When(s => s == HttpStatusCode.Unauthorized).ThenAsync(_ => Task.CompletedTask);
            Assert.Equal("ok", await request.PostAsync()); Assert.Equal("ok", await request.PostAsync());
            Assert.Equal(new[] { "payload", "payload", "payload", "payload" }, bodies);
        }
    }
    internal class TrackingStream : MemoryStream
    {
        private readonly bool _seek;
        public int Reads { get; private set; }
        public bool Disposed { get; private set; }
        public TrackingStream(string value, bool seek = true) : base(Encoding.UTF8.GetBytes(value)) => _seek = seek;
        public override bool CanSeek => _seek && base.CanSeek;
        public override long Length => _seek ? base.Length : throw new NotSupportedException();
        public override long Position { get => _seek ? base.Position : throw new NotSupportedException(); set { if (!_seek) { throw new NotSupportedException(); } base.Position = value; } }
        public override int Read(byte[] buffer, int offset, int count) { Reads++; return base.Read(buffer, offset, count); }
        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken token) { Reads++; return base.ReadAsync(buffer, offset, count, token); }
        protected override void Dispose(bool disposing) { Disposed = true; base.Dispose(disposing); }
    }
}
