#pragma warning disable CS1591
using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Inkslab.Net.Options;
using Xunit;

namespace Inkslab.Net.Tests
{
    public class HttpStreamRegressionTests
    {
        private sealed class Factory : RequestFactory
        {
            protected override Task<HttpResponseMessage> SendAsync(RequestOptions options, CancellationToken token)
                => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StreamContent(new MemoryStream(Encoding.UTF8.GetBytes("download"))) });
        }

        [Fact]
        public async Task DownloadAsync_ReturnedStreamRemainsReadableAsync()
        {
            using var stream = await new Factory().CreateRequestable("https://unit.test/").DownloadAsync();
            using var reader = new StreamReader(stream);
            Assert.Equal("download", await reader.ReadToEndAsync());
        }

        [Fact]
        public void NativeStreamApiUsesOnlyTheExistingFactoryExtensionPoint()
        {
            Assert.NotNull(typeof(IRequestableEncoding).GetMethod("Content", new[] { typeof(HttpContent) }));
            Assert.NotNull(typeof(IRequestableEncoding).GetMethod("Stream", new[] { typeof(Stream), typeof(string), typeof(bool) }));
            Assert.Null(typeof(IStreamRequestable).GetMethod("DownloadToAsync"));
            Assert.Null(typeof(RequestFactory).GetConstructor(new[] { typeof(IRequestInitialize), typeof(HttpClient) }));
            Assert.Null(typeof(RequestFactory).Assembly.GetType("Inkslab.Net.NonDisposingStream"));
        }

        [Fact]
        public async Task DownloadDispose_RejectsReadsEvenWhenContentStreamRemainsReadableAsync()
        {
            using var stream = await new BorrowedContentFactory().CreateRequestable("https://unit.test/").DownloadAsync();
            stream.Dispose();
            Assert.False(stream.CanRead);
            Assert.Throws<ObjectDisposedException>(() => stream.ReadByte());
            await Assert.ThrowsAsync<ObjectDisposedException>(() => stream.ReadAsync(new byte[1], 0, 1));
        }

        private sealed class BorrowedContentFactory : RequestFactory
        {
            protected override Task<HttpResponseMessage> SendAsync(RequestOptions options, CancellationToken token)
                => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StreamContent(new ResistantStream()) });
        }

        private sealed class ResistantStream : MemoryStream
        {
            public ResistantStream() : base(Encoding.ASCII.GetBytes("body")) { }
            protected override void Dispose(bool disposing) { }
        }
    }
}
