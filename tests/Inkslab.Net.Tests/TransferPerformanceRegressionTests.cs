#pragma warning disable CS1591
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Inkslab.Net.Options;
using Xunit;

namespace Inkslab.Net.Tests
{
    public class TransferPerformanceRegressionTests
    {
#if INKSLAB_NET_STANDARD_ASSET
        [Fact]
        public void CompatibilityRun_LoadsActualNetStandardAsset()
        {
            var framework = (System.Runtime.Versioning.TargetFrameworkAttribute)Attribute.GetCustomAttribute(
                typeof(RequestFactory).Assembly, typeof(System.Runtime.Versioning.TargetFrameworkAttribute));
            Assert.Equal(".NETStandard,Version=v2.1", framework.FrameworkName);
        }
#endif
        [Fact]
        public async Task HeadResponse_DoesNotBufferAdvertisedBodyLengthAsync()
        {
            using var server = new LoopbackHttpServer(async (headers, stream, token) =>
            {
                var response = System.Text.Encoding.ASCII.GetBytes("HTTP/1.1 200 OK\r\nContent-Length: 3000000000\r\nConnection: close\r\n\r\n");
                await stream.WriteAsync(response, token);
            });
            Assert.Equal("headers", await RequestFactory.Create(server.Url).CustomCast((response, token) =>
            {
                Assert.Equal(3000000000L, response.Content.Headers.ContentLength);
                return Task.FromResult("headers");
            }).HeadAsync(10000));
            await server.Completion;
        }

        [Fact]
        public async Task ForwardingOverride_SeesContentCompletionAndReturnsAfterBodyAsync()
        {
            using var server = new LoopbackHttpServer();
            var factory = new CompletionObservingFactory();
            var send = factory.CreateRequestable(server.Url).GetAsync(10000);
            await server.HeadersSent.Task.WaitAsync(TimeSpan.FromSeconds(5));
            Assert.Equal(HttpCompletionOption.ResponseContentRead, factory.CompletionOption);
            Assert.False(factory.Returned.Task.IsCompleted);
            server.ReleaseBody.TrySetResult(true);
            Assert.Equal("abcdef", await send);
            Assert.True(factory.Returned.Task.IsCompleted);
            await server.Completion;
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task BufferedResponse_BodyCancellationPreservesExceptionClassificationAsync(bool timeout)
        {
            using var server = new LoopbackHttpServer();
            using var cancellation = new CancellationTokenSource();
            var send = RequestFactory.Create(server.Url).GetAsync(timeout ? 1000 : 10000, cancellation.Token);
            await server.HeadersSent.Task.WaitAsync(TimeSpan.FromSeconds(5));
            if (timeout) { await Assert.ThrowsAsync<TimeoutException>(() => send.WaitAsync(TimeSpan.FromSeconds(5))); }
            else
            {
                cancellation.Cancel();
                var error = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => send.WaitAsync(TimeSpan.FromSeconds(5)));
                Assert.Equal(cancellation.Token, error.CancellationToken);
            }
        }

        private sealed class CompletionObservingFactory : RequestFactory
        {
            public HttpCompletionOption CompletionOption { get; private set; }
            public TaskCompletionSource<bool> Returned { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
            protected override async Task<HttpResponseMessage> SendAsync(RequestOptions options, CancellationToken token)
            {
                CompletionOption = options.CompletionOption;
                var response = await base.SendAsync(options, token);
                Assert.Equal(CompletionOption, options.CompletionOption);
                Returned.TrySetResult(true);
                return response;
            }
        }

        [Fact]
        public async Task BufferedResponse_StringReadDoesNotAllocateAnotherBodyBufferAsync()
        {
            const int length = 1024 * 1024;
            var body = new string('x', length);
            using var server = new LoopbackHttpServer((headers, stream, token) => LoopbackHttpServer.RespondAsync(stream, token, body));
            var result = await RequestFactory.Create(server.Url).CustomCast((response, token) =>
            {
                long before = GC.GetAllocatedBytesForCurrentThread();
                var text = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
                Assert.InRange(allocated, 2L * length, 5L * length / 2);
                return Task.FromResult(text);
            }).GetAsync(10000);
            Assert.Equal(body, result);
            await server.Completion;
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task MultipartCancellation_StopsSourceBeforeReturningAsync(bool timeout)
        {
            using var source = new CancellationOnlySource();
            using var cancellation = new CancellationTokenSource();
            using var server = new LoopbackHttpServer((headers, stream, token) => Task.Delay(Timeout.Infinite, token));
            var upload = RequestFactory.Create(server.Url)
                .Form(new Dictionary<string, object> { ["upload"] = source }, "O")
                .PostAsync(timeout ? 1000 : 10000, cancellation.Token);
            try
            {
                await source.Started.Task.WaitAsync(TimeSpan.FromSeconds(5));
                Assert.True(source.ReadToken.CanBeCanceled);
                if (!timeout) { cancellation.Cancel(); }
                if (timeout) { await Assert.ThrowsAsync<TimeoutException>(() => upload.WaitAsync(TimeSpan.FromSeconds(5))); }
                else { await Assert.ThrowsAnyAsync<OperationCanceledException>(() => upload.WaitAsync(TimeSpan.FromSeconds(5))); }
                Assert.True(source.Stopped.Task.IsCompleted);
                Assert.Equal(1, source.Disposals);
            }
            finally
            {
                // A failing legacy asset must not leave an uninterruptible upload behind.
                source.Release.TrySetResult(true);
                cancellation.Cancel();
                try { await upload.WaitAsync(TimeSpan.FromSeconds(5)); } catch { }
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task DownloadRead_UsesSoleCallerTokenDirectlyAsync(bool memory)
        {
            using var source = new CancellationOnlySource();
            using var cancellation = new CancellationTokenSource();
            using var handler = new TestHttpMessageHandler((request, token) => Task.FromResult(new HttpResponseMessage { Content = new StreamContent(source) }));
            using var client = handler.CreateClient();
            using var download = await new TestRequestFactory(client).CreateRequestable("https://unit.test/").DownloadAsync();
            var read = memory ? download.ReadAsync(new byte[1].AsMemory(), cancellation.Token).AsTask()
                : download.ReadAsync(new byte[1], 0, 1, cancellation.Token);
            try
            {
                await source.Started.Task;
                Assert.Equal(cancellation.Token, source.ReadToken);
            }
            finally
            {
                cancellation.Cancel();
                await Assert.ThrowsAnyAsync<OperationCanceledException>(() => read);
            }
        }

        private sealed class CancellationOnlySource : Stream
        {
            public CancellationToken ReadToken { get; private set; }
            public int Disposals { get; private set; }
            public TaskCompletionSource<bool> Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
            public TaskCompletionSource<bool> Stopped { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
            public TaskCompletionSource<bool> Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
            public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken token)
            {
                ReadToken = token;
                Started.TrySetResult(true);
                try { await Release.Task.WaitAsync(token); return 0; }
                finally { Stopped.TrySetResult(true); }
            }
            public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken token = default)
                => new(ReadAsync(Array.Empty<byte>(), 0, 0, token));
            public override bool CanRead => true;
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
