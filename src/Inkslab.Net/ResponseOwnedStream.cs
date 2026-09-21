using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Inkslab.Net
{
    internal sealed class ResponseOwnedStream : Stream
    {
        private readonly Stream _inner;
        private readonly HttpResponseMessage _response;
        private readonly RequestAttemptScope _scope;
        private int _disposed;
        public ResponseOwnedStream(Stream inner, HttpResponseMessage response)
        {
            _inner = inner;
            _response = response;
            _scope = (response.Content as OwnedResponseContent)?.Scope;
        }
        private Stream Inner
        {
            get
            {
                if (Volatile.Read(ref _disposed) != 0) { throw new ObjectDisposedException(nameof(ResponseOwnedStream)); }
                return _inner;
            }
        }
        public override bool CanRead => Volatile.Read(ref _disposed) == 0 && _inner.CanRead;
        public override bool CanSeek => Volatile.Read(ref _disposed) == 0 && _inner.CanSeek;
        public override bool CanWrite => Volatile.Read(ref _disposed) == 0 && _inner.CanWrite;
        public override long Length => Inner.Length;
        public override long Position { get => Inner.Position; set => Inner.Position = value; }
        public override void Flush() => Inner.Flush();
        public override Task FlushAsync(CancellationToken token) => Inner.FlushAsync(token);
        public override long Seek(long offset, SeekOrigin origin) => Inner.Seek(offset, origin);
        public override void SetLength(long value) => Inner.SetLength(value);
        public override void Write(byte[] buffer, int offset, int count) => Inner.Write(buffer, offset, count);
        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken token) => Inner.WriteAsync(buffer, offset, count, token);
#if !NET_Traditional
        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken token = default) => Inner.WriteAsync(buffer, token);
#endif
        private Exception Classify(Exception exception, CancellationToken token = default)
        {
            if (token.IsCancellationRequested && (exception is IOException || exception is ObjectDisposedException || exception is OperationCanceledException))
            { return new OperationCanceledException("The read was canceled.", exception, token); }
            return _scope?.Classify(exception) ?? exception;
        }
        public override int Read(byte[] buffer, int offset, int count)
        {
            var source = Inner;
            try { _scope?.Token.ThrowIfCancellationRequested(); return source.Read(buffer, offset, count); }
            catch (Exception ex) { var classified = Classify(ex); Dispose(); if (ReferenceEquals(ex, classified)) { throw; } throw classified; }
        }
        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken token)
        {
            var source = Inner;
            using var linked = LinkReadToken(token, out var effectiveToken);
            try { return await source.ReadAsync(buffer, offset, count, effectiveToken).ConfigureAwait(false); }
            catch (Exception ex) { var classified = Classify(ex, token); Dispose(); if (ReferenceEquals(ex, classified)) { throw; } throw classified; }
        }
#if !NET_Traditional
        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken token = default)
        {
            var source = Inner;
            using var linked = LinkReadToken(token, out var effectiveToken);
            try { return await source.ReadAsync(buffer, effectiveToken).ConfigureAwait(false); }
            catch (Exception ex) { var classified = Classify(ex, token); Dispose(); if (ReferenceEquals(ex, classified)) { throw; } throw classified; }
        }
        public override async ValueTask DisposeAsync() => await CloseAsync().ConfigureAwait(false);
#endif
        private CancellationTokenSource LinkReadToken(CancellationToken token, out CancellationToken effectiveToken)
        {
            var attemptToken = _scope?.Token ?? default;
            if (!token.CanBeCanceled) { effectiveToken = attemptToken; return null; }
            if (!attemptToken.CanBeCanceled || token == attemptToken) { effectiveToken = token; return null; }
            var linked = CancellationTokenSource.CreateLinkedTokenSource(token, attemptToken);
            effectiveToken = linked.Token;
            return linked;
        }
        internal async Task CloseAsync()
        {
            Dispose();
            if (_scope != null) { await _scope.StopAsync().ConfigureAwait(false); }
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing && Interlocked.Exchange(ref _disposed, 1) == 0)
            {
                _response.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
