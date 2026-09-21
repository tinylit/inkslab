using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Inkslab.Net
{
    // Owns both directions of one HTTP attempt. A response never implicitly owns its RequestMessage.
    internal sealed class RequestAttemptScope : IDisposable
    {
        private readonly CancellationToken _caller;
        private readonly CancellationTokenSource _timeout = new CancellationTokenSource();
        private readonly CancellationTokenSource _linked;
        private CancellationTokenRegistration _abort;
        private HttpResponseMessage _response;
        private TrackedContent _upload;
        private int _disposed;
        public HttpRequestMessage Request { get; set; }
        public CancellationToken Token { get; }
        public RequestAttemptScope(double timeout, CancellationToken caller)
        {
            _caller = caller;
            try
            {
                _timeout.CancelAfter(TimeSpan.FromMilliseconds(timeout));
                _linked = CancellationTokenSource.CreateLinkedTokenSource(caller, _timeout.Token);
                Token = _linked.Token;
            }
            catch { _timeout.Dispose(); throw; }
        }
        public HttpContent Track(HttpContent content) => _upload = new TrackedContent(content, Token);
        public void Attach(HttpResponseMessage response)
        {
            _response = response;
            response.Content = new OwnedResponseContent(response.Content, this);
            // Also interrupts synchronous reads when the transport stream cooperates with Dispose.
            _abort = Token.Register(() => response.Dispose());
        }
        public Exception Classify(Exception error)
        {
            if (error is not OperationCanceledException and not IOException and not ObjectDisposedException and not HttpRequestException)
            {
                return error;
            }
            if (_caller.IsCancellationRequested) { return new OperationCanceledException("The request was canceled.", error, _caller); }
            if (_timeout.IsCancellationRequested) { return new TimeoutException("The HTTP transfer timed out.", error); }
            return error;
        }
        public async Task StopAsync()
        {
            Dispose();
            var completion = _upload?.Completion;
            if (completion != null)
            {
                try { await completion.ConfigureAwait(false); }
                catch { /* The original send/response determines the public error. */ }
            }
        }
        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0) { return; }
            _linked.Cancel();
            _abort.Dispose();
            _response?.Dispose();
            Request?.Dispose();
            _linked.Dispose();
            // Keep the timeout signal observable for classifying a read interrupted by disposal.
            _timeout.Dispose();
        }

        private sealed class TrackedContent : HttpContent
        {
            private readonly HttpContent _inner;
#if NET6_0_OR_GREATER
            private readonly CancellationToken _token;
#endif
            private readonly object _gate = new object();
            private bool _closed;
            public Task Completion { get; private set; }
            public TrackedContent(HttpContent inner, CancellationToken token)
            {
                _inner = inner;
#if NET6_0_OR_GREATER
                _token = token;
#endif
                SetUploadCancellation(inner, token);
                foreach (var header in inner.Headers) { Headers.TryAddWithoutValidation(header.Key, header.Value); }
            }
            private static void SetUploadCancellation(HttpContent content, CancellationToken token)
            {
                if (content is UploadStreamContent upload) { upload.SetCancellationToken(token); }
                else if (content is MultipartContent multipart)
                {
                    // Legacy HttpContent.CopyToAsync cannot forward a token to multipart parts.
                    foreach (var part in multipart) { SetUploadCancellation(part, token); }
                }
            }
            protected override Task SerializeToStreamAsync(Stream stream, TransportContext context)
            {
                lock (_gate)
                {
                    if (_closed) { throw new ObjectDisposedException(nameof(TrackedContent)); }
#if NET6_0_OR_GREATER
                    return Completion = _inner.CopyToAsync(stream, context, _token);
#else
                    return Completion = _inner.CopyToAsync(stream, context);
#endif
                }
            }
#if NET6_0_OR_GREATER
            protected override Task SerializeToStreamAsync(Stream stream, TransportContext context, CancellationToken token)
                => SerializeToStreamAsync(stream, context);
#endif
            protected override bool TryComputeLength(out long length)
            {
                length = _inner.Headers.ContentLength.GetValueOrDefault();
                return _inner.Headers.ContentLength.HasValue;
            }
            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    lock (_gate)
                    {
                        if (!_closed) { _closed = true; _inner.Dispose(); }
                    }
                }
                base.Dispose(disposing);
            }
        }
    }

    internal sealed class OwnedResponseContent : HttpContent
    {
        private readonly HttpContent _inner;
        private int _disposed;
        public RequestAttemptScope Scope { get; }
        public OwnedResponseContent(HttpContent inner, RequestAttemptScope scope)
        {
            _inner = inner; Scope = scope;
            if (inner != null)
            { foreach (var header in inner.Headers) { Headers.TryAddWithoutValidation(header.Key, header.Value); } }
        }
        protected override async Task SerializeToStreamAsync(Stream stream, TransportContext context)
        {
            if (_inner == null) { return; }
            Scope.Token.ThrowIfCancellationRequested();
#if NET6_0_OR_GREATER
            await _inner.CopyToAsync(stream, context, Scope.Token).ConfigureAwait(false);
#else
            // The response transport exposes its stream without buffering. This also forwards
            // attempt cancellation on targets whose HttpContent.CopyToAsync has no token overload.
            var source = await _inner.ReadAsStreamAsync().ConfigureAwait(false);
            await source.CopyToAsync(stream, 81920, Scope.Token).ConfigureAwait(false);
#endif
        }
        // DownloadAsync bypasses HttpContent's outer stream cache. Only the inner content owns
        // the transport stream; public ReadAsStreamAsync keeps the normal buffered-content path.
        internal Task<Stream> OpenStreamAsync(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
#if NET6_0_OR_GREATER
            return _inner == null ? Task.FromResult(Stream.Null) : _inner.ReadAsStreamAsync(token);
#else
            return _inner == null ? Task.FromResult(Stream.Null) : _inner.ReadAsStreamAsync();
#endif
        }
        protected override bool TryComputeLength(out long length)
        {
            length = _inner?.Headers.ContentLength.GetValueOrDefault() ?? 0;
            return _inner == null || _inner.Headers.ContentLength.HasValue;
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing && Interlocked.Exchange(ref _disposed, 1) == 0)
            {
                Scope.Dispose();
                _inner?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
