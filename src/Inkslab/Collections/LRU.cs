using System;
using System.Collections.Generic;

namespace Inkslab.Collections
{
    /// <summary>
    /// 【线程安全】根据 LRU 算法，移除最近最久未使用的数据。
    /// </summary>
    /// <typeparam name="T">元素类型。</typeparam>
    public class LRU<T>
    {
        private readonly int capacity;
        private readonly object lockObject = new object();
        private readonly Dictionary<T, LinkedListNode<T>> keys;
        private readonly LinkedList<T> queue;

        /// <summary>
        /// 指定容器大小。
        /// </summary>
        /// <param name="capacity">容器大小。</param>
        /// <param name="comparer">比较键时要使用的 <see cref="IEqualityComparer{T}"/> 实现，或者为 null，以便为键类型使用默认的 <seealso cref="EqualityComparer{T}"/> 。</param>
        public LRU(int capacity, IEqualityComparer<T> comparer)
        {
            if (capacity < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity));
            }

            this.capacity = capacity;

            for (int i = 0; i < 3; i++)
            {
                if (capacity > 100 && (capacity & 1) == 0)
                {
                    capacity /= 2;

                    continue;
                }

                break;
            }

            queue = new LinkedList<T>();

            keys = new Dictionary<T, LinkedListNode<T>>(capacity, comparer ?? EqualityComparer<T>.Default);
        }

        /// <summary>
        /// 元素个数。
        /// </summary>
        public int Count => keys.Count;

        /// <summary>
        /// 若集合元素饱和，添加元素后，返回被淘汰的元素。
        /// </summary>
        /// <param name="addItem">添加的元素。</param>
        /// <param name="removeItem">溢出的元素。</param>
        /// <returns>元素是否溢出。</returns>
        public bool Overflow(T addItem, out T removeItem)
        {
            lock (lockObject)
            {
                bool removeFlag = false;

                if (keys.TryGetValue(addItem, out LinkedListNode<T> node))
                {
                    removeItem = default(T);

                    queue.Remove(node);
                }
                else if (keys.Count == capacity)
                {
                    removeFlag = true;

                    var last = queue.Last;

                    removeItem = last.Value;

                    if (keys.Remove(last.Value))
                    {
                        queue.Remove(last);
                    }
                }
                else
                {
                    removeItem = default(T);
                }

                keys[addItem] = queue.AddFirst(addItem);

                return removeFlag;
            }
        }
    }

    /// <summary>
    /// 【线程安全】根据 LRU 算法，移除最近最久未使用的数据。
    /// </summary>
    /// <typeparam name="TKey">键。</typeparam>
    /// <typeparam name="TValue">值。</typeparam>
    public class LRU<TKey, TValue>
    {
        private readonly LRU<TKey> lru;
        private readonly int capacity;
        private readonly Func<TKey, TValue> factory;

        private readonly Dictionary<TKey, TValue> cachings;

        private readonly object lockObj = new object();

        /// <summary>
        /// 默认容量。
        /// </summary>
        public const int DefaultCapacity = 1000;

        /// <summary>
        /// 默认容量。
        /// </summary>
        /// <param name="factory">生成 <typeparamref name="TValue"/> 的工厂。</param>
        /// <exception cref="ArgumentNullException"></exception>
        public LRU(Func<TKey, TValue> factory) : this(DefaultCapacity, factory)
        {
        }

        /// <summary>
        /// 指定最大容量。
        /// </summary>
        /// <param name="capacity">初始大小。</param>
        /// <param name="factory">生成 <typeparamref name="TValue"/> 的工厂。</param>
        public LRU(int capacity, Func<TKey, TValue> factory) : this(capacity, factory, null)
        {
        }

        /// <summary>
        /// 指定最大容量。
        /// </summary>
        /// <param name="capacity">初始大小。</param>
        /// <param name="factory">生成 <typeparamref name="TValue"/> 的工厂。</param>
        /// <param name="comparer"> 在比较集中的值时使用的 <see cref="IEqualityComparer{T}"/> 实现，或为 null 以使用集类型的默认 <seealso cref="EqualityComparer{T}"/> 实现。</param>
        public LRU(int capacity, Func<TKey, TValue> factory, IEqualityComparer<TKey> comparer)
        {
            if (capacity < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity));
            }

            if (comparer is null)
            {
                comparer = EqualityComparer<TKey>.Default;
            }

            this.capacity = capacity;
            this.factory = factory ?? throw new ArgumentNullException(nameof(factory));

            lru = new LRU<TKey>(capacity, comparer);

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
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            if (cachings.TryGetValue(key, out TValue value))
            {
                if (lru.Overflow(key, out TKey removeKey))
                {
                    lock (lockObj)
                    {
#if NET_Traditional
                        if (cachings.TryGetValue(removeKey, out TValue removeValue))
                        {
                            if (removeValue is IDisposable disposable)
                            {
                                disposable.Dispose();
                            }

                            cachings.Remove(removeKey);
                        }
#else
                        if (cachings.Remove(removeKey, out TValue removeValue))
                        {
                            if (removeValue is IDisposable disposable)
                            {
                                disposable.Dispose();
                            }
                        }
#endif
                    }
                }

                return value;
            }
label_ref:
            lock (lockObj)
            {
                if (lru.Overflow(key, out TKey removeKey))
                {
#if NET_Traditional
                        if (cachings.TryGetValue(removeKey, out TValue removeValue))
                        {
                            if (removeValue is IDisposable disposable)
                            {
                                disposable.Dispose();
                            }

                            cachings.Remove(removeKey);
                        }
#else
                    if (cachings.Remove(removeKey, out TValue removeValue))
                    {
                        if (removeValue is IDisposable disposable)
                        {
                            disposable.Dispose();
                        }
                    }
#endif
                }

                if (cachings.TryGetValue(key, out value))
                {
                    return value;
                }

                if (cachings.Count == capacity)
                {
                    goto label_ref; //? 释放锁，让移除方法得到锁。
                }

                value = factory.Invoke(key);

                cachings.Add(key, value);

                return value;
            }
        }
    }
}
