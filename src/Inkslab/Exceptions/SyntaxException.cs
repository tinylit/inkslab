using System;
using System.Data;

namespace Insklab.Exceptions
{
    /// <summary>
    /// 语法异常（<see cref="CodeException.ErrorCode"/>：100001 ~ 999999）。
    /// </summary>
    public class SyntaxException : SyntaxErrorException
    {
        /// <summary>
        /// 错误码。
        /// </summary>
        public int ErrorCode { get; }

        /// <summary>
        /// 异常。
        /// </summary>
        /// <param name="message">异常消息。</param>
        /// <param name="errorCode">错误编码。</param>
        public SyntaxException(string message, int errorCode = 100001)
            : base(message)
        {
            ErrorCode = errorCode;
        }

        /// <summary>
        /// 异常。
        /// </summary>
        /// <param name="message">异常消息。</param>
        /// <param name="innerException">引发异常的异常。</param>
        /// <param name="errorCode">错误编码。</param>
        public SyntaxException(string message, Exception innerException, int errorCode = 100001) : base(message, innerException)
        {
            ErrorCode = errorCode;
        }
    }
}
