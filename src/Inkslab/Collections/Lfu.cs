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

        private readonly int _capacity;
        private readonly SortedSet<Node> _sortKeys;
        private readonly Dictionary<T, Node> _keys;

        private readonly object _lockKeys = new object();

        [DebuggerDisplay("{key}(rank:{rank})")]
        private class Node
        {
            private readonly T _key;
            private readonly int _weight;

            private int version;
            private int rank;

            public Node(T key, int ticks, int weight)
            {
                _key = key;
                _weight = weight;

                rank = weight + ticks;
            }

            public void Update(int ticks) => rank = _weight * (++version) + ticks;

            public T Value => _key;

            private class NodeComparer : IComparer<Node>
            {
                private readonly IEqualityComparer<T> _comparer;

                public NodeComparer(IEqualityComparer<T> comparer)
                {
                    _comparer = comparer;
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
                        return _comparer.Equals(x.Value, y.Value)
                            ? 0
                            : x.Value is null
                                ? -1
                                : y.Value is null
                                    ? 1
                                    : _comparer.GetHashCode(x.Value) - _comparer.GetHashCode(y.Value);
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
        public Lfu(int capacity) : this(capacity, null)
        {

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

            _capacity = capacity;
            equalityComparer ??= EqualityComparer<T>.Default;

            _keys = new Dictionary<T, Node>(capacity, equalityComparer);

            _sortKeys = new SortedSet<Node>(Node.CreateComparer(equalityComparer));

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
        public int Count => _keys.Count;

        /// <inheritdoc />
        public bool Put(T value, out T obsoleteValue)
        {
            lock (_lockKeys)
            {
                obsoleteValue = default;

                if (_keys.TryGetValue(value, out Node node)) //? 有节点数据。
                {
                    _sortKeys.Remove(node);

                    node.Update(++version);

                    _sortKeys.Add(node);

                    return false;
                }

                bool removeFlag = false;

                if (_keys.Count == _capacity)
                {
                    removeFlag = true;

label_removeItem:
                    bool removeMinFlag = true;

                    while (removeMinFlag)
                    {
                        var min = _sortKeys.Min;

                        obsoleteValue = min.Value;

                        removeMinFlag = _sortKeys.Remove(min);

                        if (removeMinFlag && _keys.Remove(obsoleteValue))
                        {
                            goto label_add;
                        }
                    }

                    _sortKeys.Clear();

                    foreach (var kv in _keys)
                    {
                        _sortKeys.Add(kv.Value);
                    }

                    goto label_removeItem;
                }

label_add:

                node = new Node(value, version, _capacity);

                _sortKeys.Add(node);

                _keys.Add(value, node);

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
        private readonly Lfu<TKey> _lfu;

        private readonly object _lockObj = new object();

        private readonly Func<TKey, TValue> _factory;

        private readonly Dictionary<TKey, TValue> _cachings;

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

            _factory = factory ?? throw new ArgumentNullException(nameof(factory));

            comparer ??= EqualityComparer<TKey>.Default;

            _lfu = new Lfu<TKey>(capacity, comparer);

            for (int i = 0; i < 3; i++)
            {
                if (capacity > 100 && (capacity & 1) == 0)
                {
                    capacity /= 2;

                    continue;
                }

                break;
            }

            _cachings = new Dictionary<TKey, TValue>(capacity, comparer);
        }

        /// <summary>
        /// 总数。
        /// </summary>
        public int Count => _cachings.Count;

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

            lock (_lockObj)
            {
                if (_lfu.Put(key, out TKey removeKey))
                {
#if NET_Traditional
                    if (_cachings.TryGetValue(removeKey, out TValue removeValue))
                    {
                        if (removeValue is IDisposable disposable)
                        {
                            disposable.Dispose();
                        }

                        _cachings.Remove(removeKey);
                    }
#else
                    if (_cachings.Remove(removeKey, out TValue removeValue))
                    {
                        if (removeValue is IDisposable disposable)
                        {
                            disposable.Dispose();
                        }
                        else if (removeValue is IAsyncDisposable asyncDisposable)
                        {
                            asyncDisposable.DisposeAsync().AsTask().Wait();
                        }
                    }
#endif
                }

                if (_cachings.TryGetValue(key, out var value))
                {
                    return value;
                }

                return _cachings[key] = _factory.Invoke(key);
            }
        }
    }
}