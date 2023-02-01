using System;

namespace Inkslab.Net
{
    /// <summary>
    /// 请求数据验证。
    /// </summary>
    /// <typeparam name="T">结果类型。</typeparam>
    public interface IRequestableDataVerify<T>
    {
        /// <summary>
        /// 结果请求结果不满足<paramref name="predicate"/>时，会重复请求。
        /// 多个条件之间是且的关系。
        /// </summary>
        /// <param name="predicate">判断是否重试请求。</param>
        /// <returns></returns>
        IRequestableDataVerify<T> And(Predicate<T> predicate);

        /// <summary>
        /// 数据验证失败。
        /// </summary>
        /// <param name="throwError">需要抛的异常。</param>
        /// <returns>具备数据验证失败处理的请求能力。</returns>
        IRequestableDataVerifyFail<T> Fail(Func<T, Exception> throwError);
    }

    /// <summary>
    /// 具备数据验证失败重试的请求能力。
    /// </summary>
    /// <typeparam name="T">结果类型。</typeparam>
    /// <typeparam name="TResult">成功结果类型。</typeparam>
    public interface IRequestableDataVerifySuccess<T, TResult> : IRequestable<TResult>
    {
    }

    /// <summary>
    /// 具备数据验证失败处理的请求能力。
    /// </summary>
    /// <typeparam name="T">结果类型。</typeparam>
    public interface IRequestableDataVerifyFail<T> : IRequestable<T>
    {
        /// <summary>
        /// 设置重试次数。
        /// </summary>
        /// <param name="dataSuccess">成功数据。</param>
        /// <returns>具备数据验证失败重试的请求能力。</returns>
        IRequestableDataVerifySuccess<T, TResult> Success<TResult>(Func<T, TResult> dataSuccess);
    }
}
