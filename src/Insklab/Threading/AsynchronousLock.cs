using System.Threading.Tasks;

namespace System.Threading
{
    /// <summary>
    /// 异步锁 （悲观锁）。
    /// </summary>
    public sealed class AsynchronousLock : IDisposable
    {
        private readonly SemaphoreSlim semaphore;
        private readonly IDisposable releaser;

        /// <summary>
        /// 构造函数。
        /// </summary>
        public AsynchronousLock()
        {
            semaphore = new SemaphoreSlim(1);
            releaser = new Releaser(semaphore);
        }

        /// <summary>
        /// 请求锁。
        /// </summary>
        /// <returns></returns>
        public IDisposable Acquire()
        {
            semaphore.Wait();

            return releaser;
        }

        /// <summary>
        /// 请求锁。
        /// </summary>
        /// <param name="cancellationToken">取消。</param>
        /// <returns></returns>
        public async Task<IDisposable> AcquireAsync(CancellationToken cancellationToken = default)
        {
            await semaphore.WaitAsync(cancellationToken)
                .ConfigureAwait(false);

            return releaser;
        }

        private sealed class Releaser : IDisposable
        {
            private readonly SemaphoreSlim semaphore;

            public Releaser(SemaphoreSlim semaphore) => this.semaphore = semaphore;

            public void Dispose() => semaphore.Release();
        }

        /// <inheritdoc />
        public void Dispose() => semaphore.Dispose();
    }
}
