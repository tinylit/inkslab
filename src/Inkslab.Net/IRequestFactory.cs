using System;

namespace Inkslab.Net
{
    /// <summary>
    /// 请求工厂。
    /// </summary>
    public interface IRequestFactory
    {
        /// <summary>
        /// 创建请求能力。
        /// </summary>
        /// <param name="requestUri">请求地址。</param>
        /// <returns>请求能力。</returns>
        IRequestable Create(string requestUri);
    }
}