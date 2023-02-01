using System;
using System.Net;
using System.Threading.Tasks;

namespace Inkslab.Net
{
    /// <summary>
    /// 带有条件的异步延续能力请求。
    /// </summary>
    public interface IThenRequestable
    {
        /// <summary>
        /// 所有条件都满足时，重试一次请求。
        /// 多个条件之间是且的关系。
        /// </summary>
        /// <param name="predicate">条件。</param>
        /// <returns>状态验证的请求能力。</returns>
        IThenConditionRequestable If(Predicate<HttpStatusCode> predicate);
    }

    /// <summary>
    /// 带有条件的延续能力请求。
    /// </summary>
    public interface IThenConditionRequestable : IRequestable<string>, IDeserializeRequestable, IStreamRequestable
    {
        /// <summary>
        /// 任意条件都满足时，重试一次请求。
        /// 多个条件之间是或的关系。
        /// </summary>
        /// <param name="predicate">条件。</param>
        /// <returns>状态验证的请求能力。</returns>
        IThenConditionRequestable Or(Predicate<HttpStatusCode> predicate);

        /// <summary>
        /// 新开一个重试机制，如果请求异常，会调用【<paramref name="thenAsync"/>】，并重试一次请求。
        /// </summary>
        /// <param name="thenAsync">异常处理事件。</param>
        /// <returns>带有条件的异步延续能力请求。</returns>
        IThenRequestable ThenAsync(Func<IRequestableBase, Task> thenAsync);
    }
}
