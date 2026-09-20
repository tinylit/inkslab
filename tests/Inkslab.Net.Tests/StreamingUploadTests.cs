#pragma warning disable CS1591
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
namespace Inkslab.Net.Tests
{
    public class StreamingUploadTests
    {
        [Theory]
        [InlineData(true)] [InlineData(false)]
        public async Task LeaveOpen_KeepsCallerStreamOpenAsync(bool leaveOpen)
        {
            using var source = new TrackingStream("prefix-payload"); source.Position = 7;
            using var server = new LoopbackHttpServer(async (headers, network, token) =>
            {
                Assert.Contains("Content-Type: application/test", headers);
                Assert.Contains("Content-Length: 7", headers);
                await LoopbackHttpServer.RespondAsync(network, token, await LoopbackHttpServer.ReadBodyAsync(headers, network, token));
            });
            Assert.Equal("payload", await RequestFactory.Create(server.Url)
                .UseEncoding(Encoding.Unicode).Stream(source, "application/test", leaveOpen).PostAsync());
            Assert.Equal(!leaveOpen, source.Disposed);
            await server.Completion;
        }
        [Fact]
        public async Task PureStreamForm_IsMultipartAndOneShotAsync()
        {
            using var source = new TrackingStream("raw-payload", false);
            using var handler = new TestHttpMessageHandler(async (r, t) =>
            {
                Assert.Equal("multipart/form-data", r.Content.Headers.ContentType.MediaType);
                Assert.Null(r.Content.Headers.ContentLength);
                Assert.Contains("raw-payload", await r.Content.ReadAsStringAsync(t));
                return TestHttpMessageHandler.Response();
            });
            using var client = handler.CreateClient();
            var request = new TestRequestFactory(client).CreateRequestable("https://unit.test/").Form(new Dictionary<string, object> { ["upload"] = source }, "O");
            await request.PostAsync(); Assert.True(source.Disposed);
            await Assert.ThrowsAsync<InvalidOperationException>(() => request.PostAsync());
        }
        [Fact]
        public void InvalidInput_DoesNotConsumeSource()
        {
            using var source = new TrackingStream("data");
            var request = RequestFactory.Create("https://unit.test/");
            Assert.Throws<ArgumentNullException>(() => request.Content(null));
            Assert.Throws<ArgumentNullException>(() => request.Stream(null));
            Assert.Throws<FormatException>(() => request.Stream(source, "bad type"));
            Assert.False(source.Disposed); Assert.Equal(0, source.Reads);
        }
    }
    public class ContentOwnershipTests
    {
        [Fact]
        public async Task PreCanceled_DoesNotClaimContentAsync()
        {
            using var source = new TrackingStream("data");
            var request = RequestFactory.Create("https://unit.test/").Stream(source);
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => request.PostAsync(cancellationToken: new CancellationToken(true)));
            Assert.False(source.Disposed);
        }
        [Fact]
        public async Task SendFailure_DisposesClaimedContentAsync()
        {
            using var source = new FailingSource(new IOException("read failure"));
            using var server = new LoopbackHttpServer();
            var request = RequestFactory.Create(server.Url).Stream(source);
            await Assert.ThrowsAsync<HttpRequestException>(() => request.PostAsync());
            Assert.True(source.Disposed);
            await Assert.ThrowsAsync<InvalidOperationException>(() => request.PostAsync());
        }
        [Fact]
        public async Task UnrelatedCancellation_IsNotTimeoutAsync()
        {
            using var source = new FailingSource(new OperationCanceledException("content canceled"));
            using var server = new LoopbackHttpServer();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => RequestFactory.Create(server.Url).Stream(source).PostAsync());
        }
        [Fact]
        public void Factory_RejectsNullInitialization()
        {
            Assert.Throws<ArgumentNullException>(() => new RequestFactory(null));
        }
        [Theory]
        [InlineData(true, false)] [InlineData(false, false)]
        [InlineData(true, true)] [InlineData(false, true)]
        public async Task CanceledStreamUpload_PreservesOwnershipAndStopsReadingAsync(bool leaveOpen, bool timeout)
        {
            using var source = new WaitingSource();
            using var cancellation = new CancellationTokenSource();
            using var server = new LoopbackHttpServer((headers, network, token) => source.Stopped.Task.WaitAsync(token));
            var upload = RequestFactory.Create(server.Url).Stream(source, leaveOpen: leaveOpen).PostAsync(timeout ? 300 : 10000, cancellation.Token);
            await source.Started.Task.WaitAsync(TimeSpan.FromSeconds(5));
            if (!timeout) { cancellation.Cancel(); }
            if (timeout) { await Assert.ThrowsAsync<TimeoutException>(() => upload); }
            else { await Assert.ThrowsAnyAsync<OperationCanceledException>(() => upload); }
            Assert.True(source.Stopped.Task.IsCompleted);
            Assert.Equal(leaveOpen ? 0 : 1, source.Disposals);
            Assert.Equal(leaveOpen, source.CanRead);
            await server.Completion;
        }

        private sealed class FailingSource : MemoryStream
        {
            private readonly Exception _error;
            public bool Disposed { get; private set; }
            public FailingSource(Exception error) : base(new byte[1]) => _error = error;
            public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken token) => Task.FromException(_error);
            protected override void Dispose(bool disposing) { Disposed = true; base.Dispose(disposing); }
        }

        private sealed class WaitingSource : Stream
        {
            private bool _started;
            public int Disposals { get; private set; }
            public TaskCompletionSource<bool> Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
            public TaskCompletionSource<bool> Stopped { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
            public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken token)
            {
                if (!_started) { _started = true; buffer[offset] = 0; return 1; }
                Started.TrySetResult(true);
                try { await Task.Delay(Timeout.Infinite, token); return 0; }
                finally { Stopped.TrySetResult(true); }
            }
            public override bool CanRead => Disposals == 0;
            public override bool CanSeek => false;
            public override bool CanWrite => false;
            public override long Length => throw new NotSupportedException();
            public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
            public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
            public override void Flush() => throw new NotSupportedException();
            public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
            public override void SetLength(long value) => throw new NotSupportedException();
            public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
            protected override void Dispose(bool disposing) { if (disposing) { Disposals++; } base.Dispose(disposing); }
        }
    }
}
