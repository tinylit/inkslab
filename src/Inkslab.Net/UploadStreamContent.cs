using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Inkslab.Net
{
    // Content owns the upload operation. The caller decides whether it also owns the source.
    internal sealed class UploadStreamContent : HttpContent
    {
        private readonly Stream _source;
        private readonly bool _ownsStream;
        private readonly long _startPosition;
        private CancellationToken _token;
        private bool _serialized;
        private int _disposed;

        public UploadStreamContent(Stream source, bool ownsStream)
        {
            _source = source;
            _ownsStream = ownsStream;
            if (source.CanSeek) { _startPosition = source.Position; }
        }

        internal void SetCancellationToken(CancellationToken token) => _token = token;

        protected override Task SerializeToStreamAsync(Stream stream, TransportContext context)
            => CopySourceAsync(stream, _token);

#if NET6_0_OR_GREATER
        protected override Task SerializeToStreamAsync(Stream stream, TransportContext context, CancellationToken token)
            => CopySourceAsync(stream, token);
#endif

        private Task CopySourceAsync(Stream destination, CancellationToken token)
        {
            if (Volatile.Read(ref _disposed) != 0) { throw new ObjectDisposedException(nameof(UploadStreamContent)); }
            if (_serialized)
            {
                if (!_source.CanSeek) { throw new InvalidOperationException("The upload stream cannot be replayed."); }
                _source.Position = _startPosition;
            }
            _serialized = true;
            return _source.CopyToAsync(destination, 81920, token);
        }

        protected override bool TryComputeLength(out long length)
        {
            length = _source.CanSeek ? _source.Length - _startPosition : 0;
            return _source.CanSeek;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && Interlocked.Exchange(ref _disposed, 1) == 0 && _ownsStream) { _source.Dispose(); }
            base.Dispose(disposing);
        }
    }
}
