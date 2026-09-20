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
    /// 支持业务谓词验证及最终响应实体校验。
    /// </summary>
    /// <typeparam name="T">结果类型。</typeparam>
    public interface IRequestableExtend<T> : IRequestableValidation<T>
    {
        /// <summary>
        /// 配置业务结果判断；不触发 HTTP 重试或 DataAnnotations 校验。
        /// </summary>
        /// <param name="dataVerify">结果验证函数。</param>
        /// <returns>数据验证请求能力。</returns>
        IRequestableDataVerify<T> DataVerify(Predicate<T> dataVerify);
    }
}
