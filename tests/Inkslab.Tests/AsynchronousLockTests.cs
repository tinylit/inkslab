using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Inkslab.Tests
{
    /// <summary>
    /// <see cref="AsynchronousLock"/> 测试。
    /// </summary>
    public class AsynchronousLockTests
    {
        /// <summary>
        /// 同步互斥测试：并发操作共享资源不应产生竞态。
        /// </summary>
        [Fact]
        public async Task Acquire_ShouldEnsureMutualExclusionAsync()
        {
            var asyncLock = new AsynchronousLock();
            int counter = 0;
            const int iterations = 1000;

            var tasks = new List<Task>(10);
            for (int i = 0; i < 10; i++)
            {
                tasks.Add(Task.Run(() =>
                {
                    for (int j = 0; j < iterations; j++)
                    {
                        using (asyncLock.Acquire())
                        {
                            var temp = counter;
                            Thread.SpinWait(10);
                            counter = temp + 1;
                        }
                    }
                }));
            }

            await Task.WhenAll(tasks);
            Assert.Equal(10 * iterations, counter);
        }

        /// <summary>
        /// 异步互斥测试：AcquireAsync并发操作共享资源不应竞态。
        /// </summary>
        [Fact]
        public async Task AcquireAsync_ShouldEnsureMutualExclusionAsync()
        {
            var asyncLock = new AsynchronousLock();
            int counter = 0;
            const int iterations = 500;

            var tasks = new List<Task>(10);
            for (int i = 0; i < 10; i++)
            {
                tasks.Add(Task.Run(async () =>
                {
                    for (int j = 0; j < iterations; j++)
                    {
                        using (await asyncLock.AcquireAsync())
                        {
                            var temp = counter;
                            await Task.Yield();
                            counter = temp + 1;
                        }
                    }
                }));
            }

            await Task.WhenAll(tasks);
            Assert.Equal(10 * iterations, counter);
        }

        /// <summary>
        /// 取消令牌应能取消异步锁等待。
        /// </summary>
        [Fact]
        public async Task AcquireAsync_WithCancellation_ShouldThrowAsync()
        {
            var asyncLock = new AsynchronousLock();
            using var cts = new CancellationTokenSource();

            // 先获取锁
            using (asyncLock.Acquire())
            {
                // 在锁内取消令牌
                cts.Cancel();

                // 另一个获取应因取消而抛出
                await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
                    await asyncLock.AcquireAsync(cts.Token));
            }
        }

        /// <summary>
        /// Dispose后应释放资源。
        /// </summary>
        [Fact]
        public void Dispose_ShouldReleaseResources()
        {
            var asyncLock = new AsynchronousLock();
            asyncLock.Dispose();

            Assert.Throws<ObjectDisposedException>(() => asyncLock.Acquire());
        }
    }
}
