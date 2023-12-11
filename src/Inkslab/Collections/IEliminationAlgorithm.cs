namespace Inkslab.Collections
{
    /// <summary>
    /// 淘汰算法。。
    /// </summary>
    /// <typeparam name="T">元素类型。</typeparam>
    public interface IEliminationAlgorithm<T>
    {
        /// <summary>
        /// 有效元素数，
        /// </summary>
        int Count { get; }
        
        /// <summary>
        /// 添加值 <paramref name="value"/> 始终会添加成功，方法返回 true 时，代表有数据被淘汰，淘汰值为 <paramref name="obsoleteValue"/>。
        /// </summary>
        /// <param name="value">添加值。</param>
        /// <param name="obsoleteValue">被淘汰的值。</param>
        /// <returns>是否有值被淘汰。</returns>
        bool Put(T value, out T obsoleteValue);
    }
}