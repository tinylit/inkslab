using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Inkslab.Collections
{
    /// <summary>
    /// 【线程安全】LFU 算法，移除最近最不常使用的数据。
    /// </summary>
    /// <typeparam name="T">元素类型。</typeparam>
    public class Lfu<T> : IEliminationAlgorithm<T>
    {
        private int version;

        private readonly int capacity;
        private readonly SortedSet<Node> sortKeys;
        private readonly Dictionary<T, Node> keys;

        private readonly object lockKeys = new object();

        [DebuggerDisplay("{key}(rank:{rank})")]
        private class Node
        {
            private readonly T key;
            private readonly int weight;

            private int version;
            private int rank;

            public Node(T key, int ticks, int weight)
            {
                this.key = key;
                this.weight = weight;

                rank = weight + ticks;
            }

            public void Update(int ticks) => rank = weight * (++version) + ticks;

            public T Value => key;

            private class NodeComparer : IComparer<Node>
            {
                private readonly IEqualityComparer<T> comparer;

                public NodeComparer(IEqualityComparer<T> comparer)
                {
                    this.comparer = comparer;
                }

                public int Compare(Node x, Node y)
                {
                    if (x is null)
                    {
                        return -1;
                    }

                    if (y is null)
                    {
                        return 1;
                    }

                    if (ReferenceEquals(x, y))
                    {
                        return 0;
                    }

                    if (x.rank == y.rank)
                    {
                        return comparer.Equals(x.Value, y.Value)
                            ? 0
                            : x.Value is null
                                ? -1
                                : y.Value is null
                                    ? 1
                                    : comparer.GetHashCode(x.Value) - comparer.GetHashCode(y.Value);
                    }

                    return x.rank - y.rank;
                }
            }

            public static IComparer<Node> CreateComparer(IEqualityComparer<T> comparer) => new NodeComparer(comparer);
        }

        /// <summary>
        /// 指定容器大小。
        /// </summary>
        /// <param name="capacity">容器大小。</param>
        /// <param name="equalityComparer">比较键时要使用的 <see cref="IEqualityComparer{T}"/> 实现，或者为 null，以便为键类型使用默认的 <seealso cref="EqualityComparer{T}"/> 。</param>
        public Lfu(int capacity, IEqualityComparer<T> equalityComparer)
        {
            if (capacity < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity));
            }

            this.capacity = capacity;
            equalityComparer ??= EqualityComparer<T>.Default;

            keys = new Dictionary<T, Node>(capacity, equalityComparer);

            sortKeys = new SortedSet<Node>(Node.CreateComparer(equalityComparer));

            for (int i = 0; i < 3; i++)
            {
                if (capacity > 100 && (capacity & 1) == 0)
                {
                    capacity /= 2;

                    continue;
                }

                break;
            }
        }

        /// <inheritdoc />
        public int Count => keys.Count;

        /// <inheritdoc />
        public bool Put(T value, out T obsoleteValue)
        {
            lock (lockKeys)
            {
                obsoleteValue = default;

                if (keys.TryGetValue(value, out Node node)) //? 有节点数据。
                {
                    sortKeys.Remove(node);

                    node.Update(++version);

                    sortKeys.Add(node);

                    return false;
                }

                bool removeFlag = false;

                if (keys.Count == capacity)
                {
                    removeFlag = true;
                    
                    label_removeItem:
                    bool removeMinFlag = true;

                    while (removeMinFlag)
                    {
                        var min = sortKeys.Min;

                        obsoleteValue = min.Value;

                        removeMinFlag = sortKeys.Remove(min);

                        if (removeMinFlag && keys.Remove(obsoleteValue))
                        {
                            goto label_add;
                        }
                    }

                    sortKeys.Clear();

                    foreach (var kv in keys)
                    {
                        sortKeys.Add(kv.Value);
                    }

                    goto label_removeItem;
                }

                label_add:
                
                node = new Node(value, version, capacity);

                sortKeys.Add(node);

                keys.Add(value, node);

                return removeFlag;
            }
        }
    }

    /// <summary>
    /// 【线程安全】LFU 算法，移除最近最不常使用的数据。
    /// </summary>
    /// <typeparam name="TKey">键。</typeparam>
    /// <typeparam name="TValue">值。</typeparam>
    public class Lfu<TKey, TValue>
    {
        private readonly Lfu<TKey> lfu;

        private readonly Dictionary<TKey, TValue> cachings;
        private readonly Func<TKey, TValue> factory;
        private readonly int capacity;
        private readonly object lockObj = new object();

        /// <summary>
        /// 默认容量。
        /// </summary>
        public const int DefaultCapacity = 1000;

        /// <summary>
        /// 默认容量。
        /// </summary>
        /// <param name="factory">生成 <typeparamref name="TValue"/> 的工厂。</param>
        public Lfu(Func<TKey, TValue> factory) : this(DefaultCapacity, factory)
        {
        }

        /// <summary>
        /// 指定最大容量。
        /// </summary>
        /// <param name="capacity">初始大小。</param>
        /// <param name="factory">生成 <typeparamref name="TValue"/> 的工厂。</param>
        public Lfu(int capacity, Func<TKey, TValue> factory) : this(capacity, null, factory)
        {
        }

        /// <summary>
        /// 指定最大容量。
        /// </summary>
        /// <param name="capacity">初始大小。</param>
        /// <param name="comparer"> 在比较集中的值时使用的 <see cref="IEqualityComparer{T}"/> 实现，或为 null 以使用集类型的默认 <seealso cref="EqualityComparer{T}"/> 实现。</param>
        /// <param name="factory">生成 <typeparamref name="TValue"/> 的工厂。</param>
        public Lfu(int capacity, IEqualityComparer<TKey> comparer, Func<TKey, TValue> factory)
        {
            if (capacity < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity));
            }

            this.factory = factory ?? throw new ArgumentNullException(nameof(factory));

            this.capacity = capacity;

            comparer ??= EqualityComparer<TKey>.Default;

            lfu = new Lfu<TKey>(capacity, comparer);

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
                if (lfu.Put(key, out TKey obsoleteKey))
                {
                    lock (lockObj)
                    {
#if NET_Traditional
                        if (cachings.TryGetValue(obsoleteKey, out TValue obsoleteValue))
                        {
                            if (obsoleteValue is IDisposable disposable)
                            {
                                disposable.Dispose();
                            }

                            cachings.Remove(obsoleteKey);
                        }
#else
                        if (cachings.Remove(obsoleteKey, out TValue obsoleteValue))
                        {
                            if (obsoleteValue is IDisposable disposable)
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
                if (lfu.Put(key, out TKey removeKey))
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