namespace Inkslab.Net
{
    /// <summary>
    /// 请求初始化。
    /// </summary>
    public interface IRequestInitialize
    {
        /// <summary>
        /// 初始化请求器。
        /// </summary>
        /// <param name="requestable">基础请求器。</param>
        void Initialize(IRequestableBase requestable);
    }
}
