#pragma warning disable CS1591
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
namespace Inkslab.Net.Tests
{
    public class AdditionalStreamBoundaryTests
    {
        [Fact]
        public async Task DownloadDispose_ReleasesSourceOnceAsync()
        {
            var source = new CountedStream();
            var response = new HttpResponseMessage(HttpStatusCode.OK) { Content = new OwnedTestContent(source) };
            // Exercise the real attempt/content ownership pair without a production transport seam.
            // Reflection keeps this lifetime unit test available when Release removes friend access.
            var scopeType = typeof(RequestFactory).Assembly.GetType("Inkslab.Net.RequestAttemptScope", true);
            using var scope = (IDisposable)Activator.CreateInstance(scopeType, 10000D, CancellationToken.None);
            scopeType.GetMethod("Attach").Invoke(scope, new object[] { response });
            using var handler = new TestHttpMessageHandler((r, t) => Task.FromResult(response));
            using var client = handler.CreateClient();
            var download = await new TestRequestFactory(client).CreateRequestable("https://unit.test/").DownloadAsync();
            download.Dispose(); download.Dispose();
            Assert.Equal(1, source.Disposals);
        }
        private sealed class CountedStream : MemoryStream
        {
            public int Disposals { get; private set; }
            protected override void Dispose(bool disposing) { if (disposing) { Disposals++; } base.Dispose(disposing); }
        }
        private sealed class OwnedTestContent : HttpContent
        {
            private readonly Stream _source;
            public OwnedTestContent(Stream source) => _source = source;
            protected override Task<Stream> CreateContentReadStreamAsync() => Task.FromResult(_source);
            protected override Task SerializeToStreamAsync(Stream stream, TransportContext context) => throw new NotSupportedException();
            protected override bool TryComputeLength(out long length) { length = 0; return false; }
        }
        [Fact]
        public async Task MultipartBuildFailure_DisposesCreatedPartsAsync()
        {
            using var first = new TrackingStream("first");
            var missing = new FileInfo(Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".missing"));
            var request = RequestFactory.Create("https://unit.test/").Form(new Dictionary<string, object> { ["first"] = first, ["missing"] = missing }, "O");
            await Assert.ThrowsAsync<FileNotFoundException>(() => request.PostAsync());
            Assert.True(first.Disposed);
        }
        [Fact]
        public async Task MultipartBuildFailure_DisposesUnwrappedClaimedStreamsAsync()
        {
            using var tail = new TrackingStream("tail");
            var missing = new FileInfo(Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".missing"));
            var request = RequestFactory.Create("https://unit.test/").Form(new Dictionary<string, object> { ["missing"] = missing, ["tail"] = tail }, "O");
            await Assert.ThrowsAsync<FileNotFoundException>(() => request.PostAsync());
            Assert.True(tail.Disposed);
        }
        [Fact]
        public async Task FileInfo_RetryReopensAndSendsWholeFileAsync()
        {
            var path = Path.GetTempFileName();
            try
            {
                await File.WriteAllTextAsync(path, "file-payload");
                int calls = 0;
                using var handler = new TestHttpMessageHandler(async (r, t) =>
                {
                    calls++; Assert.Contains("file-payload", await r.Content.ReadAsStringAsync(t));
                    return TestHttpMessageHandler.Response("ok", calls == 1 ? HttpStatusCode.Unauthorized : HttpStatusCode.OK);
                });
                using var client = handler.CreateClient();
                var request = new TestRequestFactory(client).CreateRequestable("https://unit.test/")
                    .Form(new Dictionary<string, object> { ["upload"] = new FileInfo(path) }, "O")
                    .When(s => s == HttpStatusCode.Unauthorized).ThenAsync(_ => Task.CompletedTask);
                Assert.Equal("ok", await request.PostAsync()); Assert.Equal(2, calls);
                using var exclusive = File.Open(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
            }
            finally { File.Delete(path); }
        }
        [Fact]
        public async Task DownloadAsync_CallbackCancellationPreventsNextSendAsync()
        {
            int sends = 0;
            using var handler = new TestHttpMessageHandler((r, t) => { sends++; return Task.FromResult(TestHttpMessageHandler.Response("", HttpStatusCode.Unauthorized)); });
            using var client = handler.CreateClient();
            using var cancellation = new CancellationTokenSource();
            var started = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var callback = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var request = new TestRequestFactory(client).CreateRequestable("https://unit.test/")
                .When(s => true).ThenAsync(_ => { started.SetResult(true); return callback.Task; });
            var download = request.DownloadAsync(cancellationToken: cancellation.Token);
            try
            {
                await started.Task.WaitAsync(TimeSpan.FromSeconds(5));
                cancellation.Cancel();
                await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
                {
                    using var source = await download.WaitAsync(TimeSpan.FromSeconds(5));
                });
                Assert.Equal(1, sends);
            }
            finally { callback.TrySetResult(true); }
        }
    }
}
