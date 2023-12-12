using System;

namespace Inkslab.Exceptions
{
    /// <summary>
    /// 各服务层异常（<see cref="CodeException.ErrorCode"/>：10001 ~ 99999）。
    /// </summary>
    public class ServException : CodeException
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="message">错误消息。</param>
        /// <param name="errorCode">错误编码。</param>
        public ServException(string message, int errorCode = 10001) : base(message, errorCode)
        {
        }

        /// <summary>
        /// 异常。
        /// </summary>
        /// <param name="message">异常消息。</param>
        /// <param name="innerException">引发异常的异常。</param>
        /// <param name="errorCode">错误编码。</param>
        public ServException(string message, Exception innerException, int errorCode = 10001) : base(message, innerException, errorCode)
        {
        }
    }
}
