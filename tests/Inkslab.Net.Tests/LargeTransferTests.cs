#pragma warning disable CS1591
using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
namespace Inkslab.Net.Tests
{
    public class LargeTransferTests
    {
        [Fact]
        public async Task EarlyLoopbackResponse_ClosesFileBeforeRetryAsync()
        {
            var path = Path.GetTempFileName();
            var listener = new TcpListener(IPAddress.Loopback, 0);
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            try
            {
                using (var file = File.OpenWrite(path)) { file.SetLength(4 * 1024 * 1024); }
                listener.Start();
                var url = $"http://127.0.0.1:{((IPEndPoint)listener.LocalEndpoint).Port}/";
                var server = Task.Run(async () =>
                {
                    for (int attempt = 0; attempt < 2; attempt++)
                    {
                        using var connection = await listener.AcceptTcpClientAsync(timeout.Token);
                        using var network = connection.GetStream();
                        var header = new StringBuilder(); var single = new byte[1];
                        while (!header.ToString().EndsWith("\r\n\r\n", StringComparison.Ordinal))
                        { Assert.Equal(1, await network.ReadAsync(single, timeout.Token)); header.Append((char)single[0]); }
                        if (attempt == 0)
                        {
                            await network.WriteAsync(Encoding.ASCII.GetBytes("HTTP/1.1 401 Unauthorized\r\nContent-Length: 0\r\nConnection: close\r\n\r\n"), timeout.Token);
                        }
                        {
                            long length = 0;
                            foreach (var line in header.ToString().Split("\r\n"))
                            { if (line.StartsWith("Content-Length:", StringComparison.OrdinalIgnoreCase)) { length = long.Parse(line.Substring(15).Trim()); } }
                            Assert.True(length > 4 * 1024 * 1024);
                            var buffer = new byte[65536];
                            while (length > 0)
                            { int count = await network.ReadAsync(buffer.AsMemory(0, (int)Math.Min(length, buffer.Length)), timeout.Token); if (count == 0 && attempt == 0) { break; } Assert.True(count > 0); length -= count; }
                        }
                        if (attempt == 1)
                        { await network.WriteAsync(Encoding.ASCII.GetBytes("HTTP/1.1 200 OK\r\nContent-Length: 2\r\nConnection: close\r\n\r\nok"), timeout.Token); }
                    }
                });
                var response = await RequestFactory.Create(url)
                    .Form(new System.Collections.Generic.Dictionary<string, object> { ["file"] = new FileInfo(path) }, "O")
                    .When(s => s == HttpStatusCode.Unauthorized)
                    .ThenAsync(_ => { using var exclusive = File.Open(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None); return Task.CompletedTask; })
                    .PostAsync(15000, timeout.Token);
                Assert.Equal("ok", response); await server;
            }
            finally { listener.Stop(); File.Delete(path); }
        }
        [Fact]
        public async Task CanceledUpload_WaitsForSerializationExitAsync()
        {
            using var content = new DelayedUploadContent();
            using var cancellation = new CancellationTokenSource();
            using var server = new LoopbackHttpServer(async (headers, network, token) =>
            {
                await content.Started.Task.WaitAsync(token);
                cancellation.Cancel();
                await content.Stopped.Task.WaitAsync(token);
            });
            var send = RequestFactory.Create(server.Url).Content(content).PostAsync(10000, cancellation.Token);
            await content.Canceled.Task.WaitAsync(TimeSpan.FromSeconds(5));
            try { Assert.False(send.IsCompleted); }
            finally { content.Release.TrySetResult(true); }
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => send);
            Assert.True(content.Stopped.Task.IsCompleted);
            await server.Completion;
        }
        [Fact]
        public async Task DownloadCancellation_WaitsForSerializationExitAsync()
        {
            using var content = new DelayedUploadContent();
            using var cancellation = new CancellationTokenSource();
            using var server = new LoopbackHttpServer(async (headers, network, token) =>
            {
                await content.Started.Task.WaitAsync(token);
                cancellation.Cancel();
                await content.Stopped.Task.WaitAsync(token);
            });
            var send = RequestFactory.Create(server.Url).Content(content).DownloadAsync(cancellationToken: cancellation.Token);
            await content.Canceled.Task.WaitAsync(TimeSpan.FromSeconds(5));
            try { Assert.False(send.IsCompleted); }
            finally { content.Release.TrySetResult(true); }
            await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            {
                await using var source = await send;
            });
            Assert.True(content.Stopped.Task.IsCompleted);
            await server.Completion;
        }
        [Fact]
        public async Task EarlyResponse_RetriesReplayableBodyOnLoopbackAsync()
        {
            int calls = 0;
            using var server = new LoopbackHttpServer(async (headers, network, token) =>
            {
                calls++;
                if (calls == 1)
                {
                    await LoopbackHttpServer.RespondAsync(network, token, "", 401);
                    return;
                }
                Assert.Equal("payload", await LoopbackHttpServer.ReadBodyAsync(headers, network, token));
                await LoopbackHttpServer.RespondAsync(network, token);
            }, 2);
            Assert.Equal("ok", await RequestFactory.Create(server.Url)
                .Body("payload", "text/plain").When(s => s == HttpStatusCode.Unauthorized)
                .ThenAsync(_ => Task.CompletedTask).PostAsync(10000));
            Assert.Equal(2, calls);
            await server.Completion;
        }
        private sealed class DelayedUploadContent : HttpContent
        {
            public TaskCompletionSource<bool> Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
            public TaskCompletionSource<bool> Canceled { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
            public TaskCompletionSource<bool> Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
            public TaskCompletionSource<bool> Stopped { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
            protected override Task SerializeToStreamAsync(Stream stream, TransportContext context)
                => SerializeToStreamAsync(stream, context, CancellationToken.None);
            protected override async Task SerializeToStreamAsync(Stream stream, TransportContext context, CancellationToken token)
            {
                await stream.WriteAsync(new byte[1], token);
                await stream.FlushAsync(token);
                Started.TrySetResult(true);
                try { await Task.Delay(Timeout.Infinite, token); }
                finally { Canceled.TrySetResult(true); await Release.Task; Stopped.TrySetResult(true); }
            }
            protected override bool TryComputeLength(out long length) { length = 1024 * 1024; return true; }
        }
        [Fact]
        public async Task HundredMegabyteUpload_UsesBoundedReadsOnLoopbackAsync()
        {
            const long length = 100L * 1024 * 1024;
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(20));
            var listener = new TcpListener(IPAddress.Loopback, 0); listener.Start();
            try
            {
                var url = $"http://127.0.0.1:{((IPEndPoint)listener.LocalEndpoint).Port}/";
                var server = Task.Run(async () =>
                {
                    using var socket = await listener.AcceptTcpClientAsync(timeout.Token); using var stream = socket.GetStream();
                    var header = new StringBuilder(); var single = new byte[1];
                    while (!header.ToString().EndsWith("\r\n\r\n", StringComparison.Ordinal))
                    { Assert.Equal(1, await stream.ReadAsync(single, timeout.Token)); header.Append((char)single[0]); }
                    Assert.Contains("Content-Length: 104857600", header.ToString());
                    var buffer = new byte[65536]; long received = 0;
                    while (received < length)
                    { int count = await stream.ReadAsync(buffer.AsMemory(0, (int)Math.Min(buffer.Length, length - received)), timeout.Token); Assert.True(count > 0); received += count; }
                    await stream.WriteAsync(Encoding.ASCII.GetBytes("HTTP/1.1 200 OK\r\nContent-Length: 2\r\nConnection: close\r\n\r\nok"), timeout.Token);
                    return received;
                });
                using var source = new GeneratedStream(length);
                Assert.Equal("ok", await RequestFactory.Create(url).Stream(source).PostAsync(20000, timeout.Token));
                Assert.Equal(length, await server); Assert.True(source.Disposed);
                Assert.InRange(source.LargestRead, 1, 131072);
            }
            finally { listener.Stop(); }
        }
        private sealed class GeneratedStream : Stream
        {
            private readonly long _length; private long _position;
            public int LargestRead { get; private set; } public bool Disposed { get; private set; }
            public GeneratedStream(long length) => _length = length;
            public override bool CanRead => !Disposed; public override bool CanSeek => true; public override bool CanWrite => false;
            public override long Length => _length; public override long Position { get => _position; set => _position = value; }
            public override int Read(byte[] buffer, int offset, int count)
            { LargestRead = Math.Max(LargestRead, count); int n = (int)Math.Min(count, _length - _position); Array.Clear(buffer, offset, n); _position += n; return n; }
            public override Task<int> ReadAsync(byte[] b, int o, int c, CancellationToken token) { token.ThrowIfCancellationRequested(); return Task.FromResult(Read(b, o, c)); }
            public override void Flush() { } public override long Seek(long o, SeekOrigin s) => throw new NotSupportedException(); public override void SetLength(long v) => throw new NotSupportedException(); public override void Write(byte[] b, int o, int c) => throw new NotSupportedException();
            protected override void Dispose(bool disposing) { Disposed = true; base.Dispose(disposing); }
        }
    }
}
