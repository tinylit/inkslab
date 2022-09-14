namespace Inkslab.Keys
{
    /// <summary>
    /// KeyGen 创建器。
    /// </summary>
    public interface IKeyGenFactory
    {
        /// <summary>
        /// 创建。
        /// </summary>
        /// <returns></returns>
        IKeyGen Create();
    }
}
