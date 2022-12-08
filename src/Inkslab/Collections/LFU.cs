using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Inkslab.Collections
{
    /// <summary>
    /// 【线程安全】LFU 算法，移除最近最不常使用的数据。
    /// </summary>
    /// <typeparam name="T">元素类型。</typeparam>
    public class LFU<T>
    {
        private int version = 0;

        private readonly int capacity;
        private readonly IEqualityComparer<T> equalityComparer;
        private readonly object[] locks;
        private readonly SortedSet<Node> sortKeys;
        private readonly Dictionary<T, Node> keys;

        private readonly object lockObj = new object();

        private interface IComparerOrEquals<TItem> : IComparer<TItem>, IEqualityComparer<TItem>
        {
        }

        [DebuggerDisplay("{rank}:{Value}")]
        private class Node
        {
            private readonly T key;
            private readonly int weight = 0;

            private int version = 0;
            private int rank = 0;

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

                    //? 第一次优先加入缓存。
                    if (x.version == 0 && y.version > 0)
                    {
                        return 1;
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
        public LFU(int capacity, IEqualityComparer<T> equalityComparer)
        {
            if (capacity < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity));
            }

            this.capacity = capacity;
            this.equalityComparer = equalityComparer ??= EqualityComparer<T>.Default;

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

            locks = new object[capacity];

            for (int i = 0; i < capacity; i++)
            {
                locks[i] = new object();
            }
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
            var hashCode = equalityComparer.GetHashCode(addItem);

            var lockCode = (hashCode & 0x7fffffff) % locks.Length;

            removeItem = default;

            bool removeFlag = false;

            if (keys.TryGetValue(addItem, out Node node))
            {
                lock (lockObj)
                {
                    sortKeys.Remove(node);
                }

                goto label_tree;
            }

            lock (locks[lockCode])
            {
                if (keys.TryGetValue(addItem, out node))
                {
                    lock (lockObj)
                    {
                        sortKeys.Remove(node);
                    }

                    goto label_tree;
                }

                if (keys.Count == capacity)
                {
                    lock (lockObj)
                    {
                        var min = sortKeys.Min;

                        removeItem = min.Value;

                        if (keys.Remove(removeItem))
                        {
                            sortKeys.Remove(min);
                        }
                    }
                }

                keys.Add(addItem, node = new Node(addItem, version, capacity));
            }
label_tree:

            lock (lockObj)
            {
                node.Update(++version);

                sortKeys.Add(node);
            }

            return removeFlag;
        }
    }

    /// <summary>
    /// 【线程安全】LFU 算法，移除最近最不常使用的数据。
    /// </summary>
    /// <typeparam name="TKey">键。</typeparam>
    /// <typeparam name="TValue">值。</typeparam>
    public class LFU<TKey, TValue>
    {
        private readonly LFU<TKey> lfu;

        private readonly Dictionary<TKey, TValue> cachings;

        private readonly object lockObj = new object();

        /// <summary>
        /// 默认容量。
        /// </summary>
        public const int DefaultCapacity = 1000;

        /// <summary>
        /// 默认容量。
        /// </summary>
        public LFU() : this(DefaultCapacity)
        {
        }

        /// <summary>
        /// 指定最大容量。
        /// </summary>
        /// <param name="capacity">初始大小。</param>
        public LFU(int capacity) : this(capacity, null)
        {
        }

        /// <summary>
        /// 指定最大容量。
        /// </summary>
        /// <param name="capacity">初始大小。</param>
        /// <param name="comparer"> 在比较集中的值时使用的 <see cref="IEqualityComparer{T}"/> 实现，或为 null 以使用集类型的默认 <seealso cref="EqualityComparer{T}"/> 实现。</param>
        public LFU(int capacity, IEqualityComparer<TKey> comparer)
        {
            if (capacity < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity));
            }

            comparer ??= EqualityComparer<TKey>.Default;

            lfu = new LFU<TKey>(capacity, comparer);

            for (int i = 0; i < 3; i++)
            {
                if (capacity < 100)
                {
                    break;
                }

                if ((capacity & 1) == 0)
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
        /// <param name="factory">生成 <typeparamref name="TValue"/> 的工厂。</param>
        /// <returns>指定键使用构造函数工厂生成的值。</returns>
        public TValue GetOrCreate(TKey key, Func<TKey, TValue> factory)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            if (lfu.Overflow(key, out TKey removeKey))
            {
                lock (lockObj)
                {
                    cachings.Remove(removeKey);
                }
            }

            if (cachings.TryGetValue(key, out TValue value))
            {
                return value;
            }

            lock (lockObj)
            {
                if (cachings.TryGetValue(key, out value))
                {
                    return value;
                }

                value = factory.Invoke(key);

                cachings.Add(key, value);

                return value;
            }
        }
    }
}
