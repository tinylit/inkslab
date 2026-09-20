#pragma warning disable CS1591
using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
namespace Inkslab.Net.Tests
{
    public class StreamingDownloadTests
    {
        [Fact]
        public async Task DownloadAsync_ReturnsBeforeBodyCompletesAsync()
        {
            using var server = new LoopbackHttpServer();
            var download = RequestFactory.Create(server.Url).DownloadAsync(10000);
            await server.HeadersSent.Task.WaitAsync(TimeSpan.FromSeconds(5));
            using var source = await download.WaitAsync(TimeSpan.FromSeconds(5));
            var bytes = await ReadPrefixAsync(source);
            Assert.Equal("abc", Encoding.ASCII.GetString(bytes));
            Assert.False(server.ReleaseBody.Task.IsCompleted);
            server.ReleaseBody.SetResult(true);
            using var reader = new StreamReader(source); Assert.Equal("def", await reader.ReadToEndAsync());
            await server.Completion;
        }
        [Fact]
        public async Task DownloadAsync_CancellationDuringBodyReleasesAttemptAsync()
        {
            using var server = new LoopbackHttpServer(); using var cancel = new CancellationTokenSource();
            using var source = await RequestFactory.Create(server.Url).DownloadAsync(10000, cancel.Token);
            var buffer = await ReadPrefixAsync(source);
            var read = source.ReadAsync(buffer).AsTask(); cancel.Cancel();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => read.WaitAsync(TimeSpan.FromSeconds(5)));
        }
        [Fact]
        public async Task DownloadAsync_BodyTimeoutIsNotLostAfterHeadersAsync()
        {
            using var server = new LoopbackHttpServer();
            using var source = await RequestFactory.Create(server.Url).DownloadAsync(500);
            var buffer = await ReadPrefixAsync(source);
            var error = await Assert.ThrowsAsync<TimeoutException>(() => source.ReadAsync(buffer).AsTask());
            Assert.NotNull(error.InnerException);
        }
        [Fact]
        public async Task DownloadAsync_DisposalRejectsLaterReadsOnTransportAsync()
        {
            using var server = new LoopbackHttpServer();
            var source = await RequestFactory.Create(server.Url).DownloadAsync();
            source.Dispose();
            Assert.False(source.CanRead);
            Assert.Throws<ObjectDisposedException>(() => source.ReadByte());
            await Assert.ThrowsAsync<ObjectDisposedException>(() => source.ReadAsync(new byte[1], 0, 1));
            await source.DisposeAsync();
        }
        [Fact]
        public async Task CustomCast_CanReadContentStreamWithinCallbackAsync()
        {
            using var server = new LoopbackHttpServer((headers, stream, token) => LoopbackHttpServer.RespondAsync(stream, token, "callback body"));
            Stream captured = null;
            var result = await RequestFactory.Create(server.Url).CustomCast(async (response, token) =>
            {
                captured = await response.Content.ReadAsStreamAsync(token);
                using var reader = new StreamReader(captured, Encoding.UTF8, false, 1024, true);
                return await reader.ReadToEndAsync();
            }).GetAsync();
            Assert.Equal("callback body", result);
            Assert.False(captured.CanRead);
            await server.Completion;
        }

        private static async Task<byte[]> ReadPrefixAsync(Stream source)
        {
            var buffer = new byte[3];
            int offset = 0;
            while (offset < buffer.Length)
            {
                int read = await source.ReadAsync(buffer.AsMemory(offset));
                Assert.InRange(read, 1, buffer.Length - offset);
                offset += read;
            }
            return buffer;
        }
    }
}
