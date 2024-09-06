using System.Threading.Tasks;

namespace System.Threading
{
    /// <summary>
    /// 异步锁 （悲观锁）。
    /// </summary>
    public sealed class AsynchronousLock : IDisposable
    {
        private readonly SemaphoreSlim _semaphore;
        private readonly IDisposable _releaser;

        /// <summary>
        /// 构造函数。
        /// </summary>
        public AsynchronousLock()
        {
            _semaphore = new SemaphoreSlim(1);
            _releaser = new Releaser(_semaphore);
        }

        /// <summary>
        /// 请求锁。
        /// </summary>
        /// <returns></returns>
        public IDisposable Acquire()
        {
            _semaphore.Wait();

            return _releaser;
        }

        /// <summary>
        /// 请求锁。
        /// </summary>
        /// <param name="cancellationToken">取消。</param>
        /// <returns></returns>
        public async Task<IDisposable> AcquireAsync(CancellationToken cancellationToken = default)
        {
            await _semaphore.WaitAsync(cancellationToken)
                .ConfigureAwait(false);

            return _releaser;
        }

        private sealed class Releaser : IDisposable
        {
            private readonly SemaphoreSlim _semaphore;

            public Releaser(SemaphoreSlim semaphore) => _semaphore = semaphore;

            public void Dispose() => _semaphore.Release();
        }

        /// <inheritdoc />
        public void Dispose() => _semaphore.Dispose();
    }
}
