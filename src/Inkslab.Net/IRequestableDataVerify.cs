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
        /// 数据验证失败。
        /// </summary>
        /// <typeparam name="TError">异常类型。</typeparam>
        /// <param name="throwError">验证失败的异常。</param>
        /// <returns>具备数据验证失败处理的请求能力。</returns>
        IRequestableDataVerifyFail<T> Fail<TError>(Func<T, TError> throwError) where TError : Exception;

        /// <summary>
        /// 映射业务谓词验证成功的结果；不触发 HTTP 重试。
        /// </summary>
        /// <typeparam name="TResult">验证成功返回的结果。</typeparam>
        /// <param name="dataVerifySuccess">成功数据。</param>
        /// <returns>具备业务谓词验证成功结果映射的请求能力。</returns>
        IRequestableDataVerifySuccess<T, TResult> Success<TResult>(Func<T, TResult> dataVerifySuccess);
    }

    /// <summary>
    /// 具备业务谓词验证成功结果映射的请求能力。
    /// </summary>
    /// <typeparam name="T">结果类型。</typeparam>
    /// <typeparam name="TResult">成功结果类型。</typeparam>
    public interface IRequestableDataVerifySuccess<T, TResult>
    {
        /// <summary>
        /// 数据验证失败。
        /// </summary>
        /// <param name="dataVerifyFail">需要返回的数据。</param>
        /// <returns>具备数据验证失败处理的请求能力。</returns>
        IRequestableDataVerifyFail<T, TResult> Fail(Func<T, TResult> dataVerifyFail);

        /// <summary>
        /// 数据验证失败。
        /// </summary>
        /// <typeparam name="TError">异常类型。</typeparam>
        /// <param name="throwError">验证失败的异常。</param>
        /// <returns>具备数据验证失败处理的请求能力。</returns>
        IRequestableDataVerifyFail<T, TResult> Fail<TError>(Func<T, TError> throwError) where TError : Exception;
    }

    /// <summary>
    /// 具备数据验证失败处理的请求能力。
    /// </summary>
    /// <typeparam name="T">结果类型。</typeparam>
    public interface IRequestableDataVerifyFail<T> : IRequestableValidation<T>
    {
    }

    /// <summary>
    /// 具备数据验证失败处理的请求能力。
    /// </summary>
    /// <typeparam name="T">结果类型。</typeparam>
    /// <typeparam name="TResult">失败结果类型。</typeparam>
    public interface IRequestableDataVerifyFail<T, TResult> : IRequestableValidation<TResult>
    {
    }
}
