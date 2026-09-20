using Inkslab.Net.Validation;

namespace Inkslab.Net
{
    /// <summary>
    /// 对最终响应结果执行 DataAnnotations 校验的请求能力。
    /// </summary>
    /// <typeparam name="T">最终结果类型。</typeparam>
    public interface IRequestableValidation<T> : IRequestable<T>
    {
        /// <summary>
        /// 在业务结果处理之后显式校验最终实体，优先于 ValidateOutput 标记且只校验一次。
        /// 普通值和集合跳过实体校验。
        /// </summary>
        /// <param name="options">调用时快照的校验选项，省略时使用默认值。</param>
        /// <returns>只具备执行能力的请求，不能再添加业务结果处理。</returns>
        IRequestable<T> Validation(ValidationOptions options = default);
    }
}
