using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Inkslab.Net
{
    /// <summary>
    /// 请求能力。
    /// </summary>
    public interface IRequestable : IRequestableBase<IRequestable>, IRequestableEncoding
    {
        /// <summary>
        /// 使用编码，默认:UTF-8。
        /// </summary>
        /// <param name="encoding">编码。</param>
        IRequestableEncoding UseEncoding(Encoding encoding);
    }

    /// <summary>
    /// 请求能力。
    /// </summary>
    /// <typeparam name="T">结果类型。</typeparam>
    public interface IRequestable<T>
    {
        /// <summary>
        /// GET 请求。
        /// </summary>
        /// <param name="timeout">超时时间，单位：毫秒。</param>
        /// <param name="cancellationToken">可由其他对象或线程用以接收取消通知的取消标记。</param>
        /// <returns>请求结果。</returns>
        Task<T> GetAsync(double timeout = 1000D, CancellationToken cancellationToken = default);

        /// <summary>
        /// DELETE 请求。
        /// </summary>
        /// <param name="timeout">超时时间，单位：毫秒。</param>
        /// <param name="cancellationToken">可由其他对象或线程用以接收取消通知的取消标记。</param>
        /// <returns>请求结果。</returns>
        Task<T> DeleteAsync(double timeout = 1000D, CancellationToken cancellationToken = default);

        /// <summary>
        /// POST 请求。
        /// </summary>
        /// <param name="timeout">超时时间，单位：毫秒。</param>
        /// <param name="cancellationToken">可由其他对象或线程用以接收取消通知的取消标记。</param>
        /// <returns>请求结果。</returns>
        Task<T> PostAsync(double timeout = 1000D, CancellationToken cancellationToken = default);

        /// <summary>
        /// POST 请求。
        /// </summary>
        /// <param name="timeout">超时时间，单位：毫秒。</param>
        /// <param name="cancellationToken">可由其他对象或线程用以接收取消通知的取消标记。</param>
        /// <returns>请求结果。</returns>
        Task<T> PutAsync(double timeout = 1000D, CancellationToken cancellationToken = default);

        /// <summary>
        /// HEAD 请求。
        /// </summary>
        /// <param name="timeout">超时时间，单位：毫秒。</param>
        /// <param name="cancellationToken">可由其他对象或线程用以接收取消通知的取消标记。</param>
        /// <returns>请求结果。</returns>
        Task<T> HeadAsync(double timeout = 1000D, CancellationToken cancellationToken = default);

        /// <summary>
        /// PATCH 请求。
        /// </summary>
        /// <param name="timeout">超时时间，单位：毫秒。</param>
        /// <param name="cancellationToken">可由其他对象或线程用以接收取消通知的取消标记。</param>
        /// <returns>请求结果。</returns>
        Task<T> PatchAsync(double timeout = 1000D, CancellationToken cancellationToken = default);

        /// <summary>
        /// 数据返回XML格式的结果，将转为指定类型。
        /// </summary>
        /// <param name="method">求取方式。</param>
        /// <param name="timeout">超时时间，单位：毫秒。</param>
        /// <param name="cancellationToken">可由其他对象或线程用以接收取消通知的取消标记。</param>
        /// <returns>请求结果。</returns>
        Task<T> SendAsync(string method, double timeout = 1000D, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// 请求数据验证。
    /// </summary>
    /// <typeparam name="T">结果类型。</typeparam>
    public interface IRequestableExtend<T> : IRequestable<T>
    {
        /// <summary>
        /// 结果请求结果不满足<paramref name="dataVerify"/>时，会重复请求。
        /// </summary>
        /// <param name="dataVerify">结果验证函数。</param>
        /// <returns>数据验证请求能力。</returns>
        IRequestableDataVerify<T> DataVerify(Predicate<T> dataVerify);
    }
}
