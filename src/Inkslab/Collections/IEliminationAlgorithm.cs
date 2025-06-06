namespace Inkslab.Collections
{
    /// <summary>
    /// 淘汰算法。。
    /// </summary>
    /// <typeparam name="T">元素类型。</typeparam>
    public interface IEliminationAlgorithm<T> where T : notnull
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

    /// <summary>
    /// 淘汰算法。。
    /// </summary>
    /// <typeparam name="TKey">键。</typeparam>
    /// <typeparam name="TValue">值。</typeparam>
    public interface IEliminationAlgorithm<TKey, TValue> where TKey : notnull
    {
        /// <summary>
        /// 有效元素数，
        /// </summary>
        int Count { get; }

        /// <summary>
        /// 获取值。
        /// </summary>
        /// <param name="key">键。</param>
        /// <returns>如果<paramref name="key"/>不存在时，使用实列默认规则生成值。</returns>
        TValue Get(TKey key);

        /// <summary>
        /// 尝试获取值。
        /// </summary>
        /// <param name="key">键。</param>
        /// <param name="value">值。</param>
        /// <returns>返回成功时，<paramref name="value"/>有值，否则为类型默认值。</returns>
        bool TryGet(TKey key, out TValue value);

        /// <summary>
        /// 设置值，如果不存在则添加新值。
        /// </summary>
        /// <param name="key">键。</param>
        /// <param name="value">值。</param>
        void Put(TKey key, TValue value);
    }
}