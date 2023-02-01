using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Inkslab.Net
{
    /// <summary>
    /// 流请求能力。
    /// </summary>
    public interface IStreamRequestable
    {
        /// <summary>
        /// 下载文件。
        /// </summary>
        /// <param name="timeout">超时时间，单位：毫秒。</param>
        /// <param name="cancellationToken">可由其他对象或线程用以接收取消通知的取消标记。</param>
        /// <returns>请求流结果。</returns>
        Task<Stream> DownloadAsync(double timeout = 10000D, CancellationToken cancellationToken = default);
    }
}
