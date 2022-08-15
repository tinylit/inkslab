using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace Insklab.Collections
{
    /// <summary>
    /// 异步线程安全字典。
    /// </summary>
    /// <typeparam name="TKey">键。</typeparam>
    /// <typeparam name="TValue">值。</typeparam>
    public class AsynchronousDictionary<TKey, TValue> : IReadOnlyDictionary<TKey, TValue>, IReadOnlyCollection<KeyValuePair<TKey, TValue>>, IEnumerable<KeyValuePair<TKey, TValue>>, IEnumerable
    {
        private readonly ConcurrentDictionary<TKey, TValue> storage;
        private readonly AsynchronousLock asynchronousLock = new AsynchronousLock();

        /// <summary>
        /// 构造函数。
        /// </summary>
        public AsynchronousDictionary() => storage = new ConcurrentDictionary<TKey, TValue>();

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="comparer">键类型比较器。</param>
        public AsynchronousDictionary(IEqualityComparer<TKey> comparer) => storage = new ConcurrentDictionary<TKey, TValue>(comparer);

        /// <summary>
        /// 索引。
        /// </summary>
        /// <param name="key">键。</param>
        /// <returns>指定键<paramref name="key"/>对应的值。</returns>
        /// <exception cref="KeyNotFoundException">未找到指定键数据。</exception>
        public TValue this[TKey key] => storage[key];

        /// <summary>
        /// 字典键/值数。
        /// </summary>
        public int Count => storage.Count;

        /// <summary>
        /// 包含指定<paramref name="key"/>键。
        /// </summary>
        /// <param name="key">键。</param>
        /// <returns>是否包含指定<paramref name="key"/>键。</returns>
        public bool ContainsKey(TKey key) => storage.ContainsKey(key);

        /// <summary>
        /// 尝试获取索引值。
        /// </summary>
        /// <param name="key">键。</param>
        /// <param name="value">值。</param>
        /// <returns></returns>
        public bool TryGetValue(TKey key, out TValue value) => storage.TryGetValue(key, out value);

        /// <summary>
        /// 如果该键不存在，则将键/值对添加到字典中，返回新值；否则返回现有值。
        /// </summary>
        /// <param name="key">键。</param>
        /// <param name="value">值。</param>
        /// <returns>键的值。如果该键不存在，则返回新的值；否则返回现有值。</returns>
        public TValue GetOrAdd(TKey key, TValue value) => storage.GetOrAdd(key, value);

        /// <summary>
        /// 将键/值添加到字典中，如果键存在，则为已存在的值；否则，则使用指定的函数创建值，添加到缓存并返回。
        /// </summary>
        /// <param name="key">要添加的元素的键。</param>
        /// <param name="valueFactory">用于为键生成值的函数。</param>
        /// <returns></returns>
        public TValue GetOrAdd(TKey key, Func<TKey, TValue> valueFactory) => storage.GetOrAdd(key, valueFactory);

        /// <summary>
        /// 如果该键不存在，则将 <paramref name="addValue"/> 键/值对添加到字典中；否则，使用更新函数（<paramref name="updateValueFactory"/>）更新字典中的键/值对。
        /// </summary>
        /// <param name="key">键。</param>
        /// <param name="addValue">值。</param>
        /// <param name="updateValueFactory"创建更新值。></param>
        /// <returns>键的新值。若该键不存在，返回 <paramref name="addValue"/> ；否则，返回 <paramref name="updateValueFactory"/> 的结果。</returns>
        public TValue AddOrUpdate(TKey key, TValue addValue, Func<TKey, TValue, TValue> updateValueFactory) => storage.AddOrUpdate(key, addValue, updateValueFactory);

        /// <summary>
        /// 如果该键不存在，则使用添加函数（<paramref name="addValueFactory"/>）将键/值对添加到字典中；否则，使用更新函数（<paramref name="updateValueFactory"/>）更新字典中的键/值对。
        /// </summary>
        /// <param name="key">键。</param>
        /// <param name="addValueFactory">创建新值。</param>
        /// <param name="updateValueFactory">创建更新值。</param>
        /// <returns>键的新值。若该键存在，则返回 <paramref name="addValueFactory"/> 的结果；否则，返回 <paramref name="updateValueFactory"/> 的结果。</returns>
        public TValue AddOrUpdate(TKey key, Func<TKey, TValue> addValueFactory, Func<TKey, TValue, TValue> updateValueFactory) => storage.AddOrUpdate(key, addValueFactory, updateValueFactory);

        /// <summary>
        /// 将键/值添加到字典中，如果键存在，则为已存在的值；否则，则使用指定的函数创建值，添加到缓存并返回。
        /// </summary>
        /// <param name="key">要添加的元素的键。</param>
        /// <param name="valueFactory">用于为键生成值的函数。</param>
        /// <param name="cancellationToken">取消令牌。</param>
        /// <returns></returns>
        public Task<TValue> GetOrAddAsync(TKey key, Func<TKey, CancellationToken, Task<TValue>> valueFactory, CancellationToken cancellationToken = default)
        {
            if (valueFactory is null)
            {
                throw new ArgumentNullException(nameof(valueFactory));
            }

            return storage.TryGetValue(key, out var existentValue)
                ? Task.FromResult(existentValue)
                : GetOrAddInternalAsync(key, valueFactory, cancellationToken);
        }

        private async Task<TValue> GetOrAddInternalAsync(TKey key, Func<TKey, CancellationToken, Task<TValue>> valueFactory, CancellationToken cancellationToken)
        {
            using (await asynchronousLock.AcquireAsync(cancellationToken).ConfigureAwait(false))
            {
                if (storage.TryGetValue(key, out var existentValue))
                {
                    return existentValue;
                }

                existentValue = storage[key] = await valueFactory.Invoke(key, cancellationToken);

                return existentValue;
            }
        }

        /// <summary>
        /// 如果该键不存在，则将 <paramref name="addValue"/> 键/值对添加到字典中；否则，使用更新函数（<paramref name="updateValueFactory"/>）更新字典中的键/值对。
        /// </summary>
        /// <param name="key">键。</param>
        /// <param name="addValue">值。</param>
        /// <param name="updateValueFactory"创建更新值。></param>
        /// <returns>键的新值。若该键不存在，返回 <paramref name="addValue"/> ；否则，返回 <paramref name="updateValueFactory"/> 的结果。</returns>
        public Task<TValue> AddOrUpdateAsync(TKey key, TValue addValue, Func<TKey, TValue, CancellationToken, Task<TValue>> updateValueFactory, CancellationToken cancellationToken = default)
        {
            if (updateValueFactory is null)
            {
                throw new ArgumentNullException(nameof(updateValueFactory));
            }

            if (storage.TryGetValue(key, out TValue value))
            {
                return AddOrUpdateInternalAsync(key, value, updateValueFactory, cancellationToken);
            }

            return Task.FromResult(storage[key] = addValue);
        }

        /// <summary>
        /// 如果该键不存在，则使用添加函数（<paramref name="addValueFactory"/>）将键/值对添加到字典中；否则，使用更新函数（<paramref name="updateValueFactory"/>）更新字典中的键/值对。
        /// </summary>
        /// <param name="key">键。</param>
        /// <param name="addValueFactory">创建新值。</param>
        /// <param name="updateValueFactory">创建更新值。</param>
        /// <returns>键的新值。若该键存在，则返回 <paramref name="addValueFactory"/> 的结果；否则，返回 <paramref name="updateValueFactory"/> 的结果。</returns>
        public Task<TValue> AddOrUpdateAsync(TKey key, Func<TKey, CancellationToken, Task<TValue>> addValueFactory, Func<TKey, TValue, CancellationToken, Task<TValue>> updateValueFactory, CancellationToken cancellationToken = default)
        {
            if (addValueFactory is null)
            {
                throw new ArgumentNullException(nameof(addValueFactory));
            }

            if (updateValueFactory is null)
            {
                throw new ArgumentNullException(nameof(updateValueFactory));
            }

            if (storage.TryGetValue(key, out TValue value))
            {
                return AddOrUpdateInternalAsync(key, value, updateValueFactory, cancellationToken);
            }

            return AddOrUpdateInternalAsync(key, addValueFactory, cancellationToken);
        }

        private async Task<TValue> AddOrUpdateInternalAsync(TKey key, Func<TKey, CancellationToken, Task<TValue>> addValueFactory, CancellationToken cancellationToken)
        {
            using (await asynchronousLock.AcquireAsync(cancellationToken))
            {
                return storage[key] = await addValueFactory.Invoke(key, cancellationToken);
            }
        }

        private async Task<TValue> AddOrUpdateInternalAsync(TKey key, TValue value, Func<TKey, TValue, CancellationToken, Task<TValue>> updateValueFactory, CancellationToken cancellationToken)
        {
            using (await asynchronousLock.AcquireAsync(cancellationToken))
            {
                return storage[key] = await updateValueFactory.Invoke(key, value, cancellationToken);
            }
        }

        /// <summary>
        /// 尝试从字典中移除并返回具有指定键的值。
        /// </summary>
        /// <param name="key">键。</param>
        /// <param name="value"><paramref name="key"/> 键对应的值。</param>
        /// <returns>如果成功移除了对象，则为 true，否则为 false。</returns>
        public bool TryRemove(TKey key, out TValue value) => storage.TryRemove(key, out value);

        /// <summary>
        /// 如果具有 <paramref name="key"/> 的现有值等于 <paramref name="comparisonValue"/>，则将于 <paramref name="key"/> 关联的值更新为 <paramref name="newValue"/>。
        /// </summary>
        /// <param name="key">键。</param>
        /// <param name="newValue">新的值。</param>
        /// <param name="comparisonValue">与现有值对比的值。</param>
        /// <returns>如果 <paramref name="comparisonValue"/> 的值与 <paramref name="key"/> 的值相等且被替换为 <paramref name="comparisonValue"/> 时，返回 true，否则，返回 false。</returns>
        public bool TryUpdate(TKey key, TValue newValue, TValue comparisonValue) => storage.TryUpdate(key, newValue, comparisonValue);

        IEnumerable<TKey> IReadOnlyDictionary<TKey, TValue>.Keys => storage.Keys;

        IEnumerable<TValue> IReadOnlyDictionary<TKey, TValue>.Values => storage.Values;

        IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator() => storage.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => storage.GetEnumerator();
    }
}
