using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace Inkslab.Collections
{
    /// <summary>
    /// 【线程安全】LRS 算法，移除最近最少被搜索到的数据。
    /// </summary>
    /// <typeparam name="T">元素类型。</typeparam>
    public class Lrs<T> : IEliminationAlgorithm<T>
    {
        private int refCount = -1;

        private readonly int capacity;
        private readonly IEqualityComparer<T> comparer;
        private readonly T[] arrays;
        private readonly ConcurrentDictionary<T, int> keys;

        /// <summary>
        /// 指定容器大小。
        /// </summary>
        /// <param name="capacity">容器大小。</param>
        public Lrs(int capacity) : this(capacity, null)
        {

        }

        /// <summary>
        /// 指定容器大小。
        /// </summary>
        /// <param name="capacity">容器大小。</param>
        /// <param name="comparer">比较键时要使用的 <see cref="IEqualityComparer{T}"/> 实现，或者为 null，以便为键类型使用默认的 <seealso cref="EqualityComparer{T}"/> 。</param>
        public Lrs(int capacity, IEqualityComparer<T> comparer)
        {
            if (capacity < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity));
            }

            this.capacity = capacity;
            this.comparer = comparer ?? EqualityComparer<T>.Default;

            arrays = new T[capacity];

            int concurrencyLevel = capacity;

            for (int i = 0; i < 3; i++)
            {
                if (concurrencyLevel > 100 && (concurrencyLevel & 1) == 0)
                {
                    concurrencyLevel /= 2;

                    continue;
                }

                break;
            }

            keys = new ConcurrentDictionary<T, int>(concurrencyLevel, capacity, this.comparer);
        }

        /// <inheritdoc />
        public int Count => keys.Count;

        /// <inheritdoc />
        public bool Put(T value, out T obsoleteValue)
        {
            int index = Interlocked.Increment(ref refCount);

            int offset = index % capacity;

            obsoleteValue = arrays[offset];

            if (comparer.Equals(value, obsoleteValue))
            {
                return false;
            }

            bool flag = false;

            if (index >= capacity)
            {
                if (keys.Count == capacity || keys.TryGetValue(obsoleteValue, out int local) && local == offset)
                {
                    flag = true;

                    do
                    {
                        if (keys.TryRemove(obsoleteValue, out _))
                        {
                            break;
                        }
                    } while (keys.ContainsKey(obsoleteValue));
                }
                else //? 字典不存在，但被移除成功，代表值出现过，并被销毁了。
                {
                    flag = true;
                }
            }

            keys[value] = offset;

            arrays[offset] = value;

            return flag;
        }
    }


    /// <summary>
    /// 【线程安全】根据 LRU 算法，移除最近最久未使用的数据。
    /// </summary>
    /// <typeparam name="TKey">键。</typeparam>
    /// <typeparam name="TValue">值。</typeparam>
    public class Lrs<TKey, TValue>
    {
        private readonly Lrs<TKey> lrs;

        private readonly object lockObj = new object();
        private readonly Func<TKey, TValue> factory;

        private readonly Dictionary<TKey, TValue> cachings;

        /// <summary>
        /// 默认容量。
        /// </summary>
        public const int DefaultCapacity = 1000;

        /// <summary>
        /// 默认容量。
        /// </summary>
        /// <param name="factory">生成 <typeparamref name="TValue"/> 的工厂。</param>
        /// <exception cref="ArgumentNullException"></exception>
        public Lrs(Func<TKey, TValue> factory) : this(DefaultCapacity, factory)
        {
        }

        /// <summary>
        /// 指定最大容量。
        /// </summary>
        /// <param name="capacity">初始大小。</param>
        /// <param name="factory">生成 <typeparamref name="TValue"/> 的工厂。</param>
        /// <param name="comparer"> 在比较集中的值时使用的 <see cref="IEqualityComparer{T}"/> 实现，或为 null 以使用集类型的默认 <seealso cref="EqualityComparer{T}"/> 实现。</param>
        public Lrs(int capacity, Func<TKey, TValue> factory, IEqualityComparer<TKey> comparer = null)
        {
            if (capacity < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity));
            }

            this.factory = factory ?? throw new ArgumentNullException(nameof(factory));

            comparer ??= EqualityComparer<TKey>.Default;

            lrs = new Lrs<TKey>(capacity, comparer);

            for (int i = 0; i < 3; i++)
            {
                if (capacity > 100 && (capacity & 1) == 0)
                {
                    capacity /= 2;

                    continue;
                }

                break;
            }

            cachings = new Dictionary<TKey, TValue>(capacity, comparer);
        }

        /// <summary>
        /// 总数。
        /// </summary>
        public int Count => cachings.Count;

        /// <summary>
        /// 获取值。
        /// </summary>
        /// <param name="key">键。</param>
        /// <returns>指定键使用构造函数工厂生成的值。</returns>
        public TValue Get(TKey key)
        {
            if (key is null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            lock (lockObj)
            {
                if (lrs.Put(key, out TKey obsoleteKey))
                {
#if NET_Traditional
                if (cachings.TryGetValue(obsoleteKey, out TValue obsoleteValue))
                {
                    cachings.Remove(obsoleteKey);

                    if (obsoleteValue is IDisposable disposable)
                    {
                        disposable.Dispose();
                    }
                }
#else
                if (cachings.Remove(obsoleteKey, out TValue obsoleteValue))
                {
                    if (obsoleteValue is IDisposable disposable)
                    {
                        disposable.Dispose();
                    }
                    else if (obsoleteValue is IAsyncDisposable asyncDisposable)
                    {
                        asyncDisposable.DisposeAsync().AsTask().Wait();
                    }
                }
#endif
                }

                if (cachings.TryGetValue(key, out var value))
                {
                    return value;
                }

                return cachings[key] = factory.Invoke(key);
            }
        }
    }
}