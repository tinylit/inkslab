using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Inkslab.Collections
{
    /// <summary>
    /// 【线程安全】LRU 算法，移除最近最少使用的数据。使用分片锁降低高并发场景下的锁竞争。
    /// </summary>
    /// <typeparam name="T">元素类型。</typeparam>
    public class Lru<T> : IEliminationAlgorithm<T> where T : notnull
    {
        private class Node
        {
            public T Value { get; }
            public Node Previous { get; set; }
            public Node Next { get; set; }

            public Node()
            {
            }

            public Node(T value)
            {
                Value = value;
            }
        }

        private class Shard
        {
            private readonly int _capacity;
            private readonly Node _head;
            private readonly Node _tail;
            private readonly Dictionary<T, Node> _cachings;
            private readonly object _lockObj = new object();

            public Shard(int capacity, IEqualityComparer<T> comparer)
            {
                _capacity = capacity;

                _head = new Node();
                _tail = new Node();
                _head.Next = _tail;
                _tail.Previous = _head;

                int dictCapacity = capacity;

                for (int i = 0; i < 3; i++)
                {
                    if (dictCapacity > 100 && (dictCapacity & 1) == 0)
                    {
                        dictCapacity /= 2;

                        continue;
                    }

                    break;
                }

                _cachings = new Dictionary<T, Node>(dictCapacity, comparer);
            }

            public int Count { get { lock (_lockObj) { return _cachings.Count; } } }

            public bool Put(T value, out T obsoleteValue)
            {
                obsoleteValue = default;

                lock (_lockObj)
                {
                    if (_cachings.TryGetValue(value, out var node))
                    {
                        MoveToHead(node);

                        return false;
                    }

                    bool flag = false;

                    if (_cachings.Count >= _capacity)
                    {
                        flag = true;

                        var lastNode = RemoveTail();
                        _cachings.Remove(lastNode.Value);
                        obsoleteValue = lastNode.Value;
                    }

                    var newNode = new Node(value);
                    _cachings.Add(value, newNode);
                    AddToHead(newNode);

                    return flag;
                }
            }

            private void MoveToHead(Node node)
            {
                RemoveNode(node);
                AddToHead(node);
            }

            private void AddToHead(Node node)
            {
                node.Previous = _head;
                node.Next = _head.Next;
                _head.Next!.Previous = node;
                _head.Next = node;
            }

            private static void RemoveNode(Node node)
            {
                node.Previous!.Next = node.Next;
                node.Next!.Previous = node.Previous;
            }

            private Node RemoveTail()
            {
                var lastNode = _tail.Previous!;
                RemoveNode(lastNode);
                return lastNode;
            }
        }

        private readonly Shard[] _shards;
        private readonly int _shardMask;
        private readonly IEqualityComparer<T> _comparer;

        /// <summary>
        /// 指定容器大小。
        /// </summary>
        /// <param name="capacity">容器大小。</param>
        public Lru(int capacity) : this(capacity, null)
        {

        }

        /// <summary>
        /// 指定容器大小。
        /// </summary>
        /// <param name="capacity">容器大小。</param>
        /// <param name="comparer">比较键时要使用的 <see cref="IEqualityComparer{T}"/> 实现，或者为 null，以便为键类型使用默认的 <seealso cref="EqualityComparer{T}"/> 。</param>
        public Lru(int capacity, IEqualityComparer<T> comparer)
        {
            if (capacity <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity));
            }

            _comparer = comparer ?? EqualityComparer<T>.Default;

            int shardCount = ComputeShardCount(capacity);
            _shardMask = shardCount - 1;

            int perShardCapacity = capacity / shardCount;
            int remainder = capacity % shardCount;

            _shards = new Shard[shardCount];

            for (int i = 0; i < shardCount; i++)
            {
                int shardCapacity = perShardCapacity + (i < remainder ? 1 : 0);
                _shards[i] = new Shard(shardCapacity, _comparer);
            }
        }

        /// <inheritdoc />
        public int Count
        {
            get
            {
                int count = 0;

                for (int i = 0; i < _shards.Length; i++)
                {
                    count += _shards[i].Count;
                }

                return count;
            }
        }

        /// <inheritdoc/>
        public bool Put(T value, out T obsoleteValue)
        {
            return GetShard(value).Put(value, out obsoleteValue);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Shard GetShard(T key)
        {
            var hash = _comparer.GetHashCode(key) & 0x7FFFFFFF;
            return _shards[hash & _shardMask];
        }

        private static int ComputeShardCount(int capacity)
        {
            int target = Environment.ProcessorCount;
            int shardCount = 1;

            while (shardCount < target && capacity / (shardCount * 2) >= 4)
            {
                shardCount <<= 1;
            }

            return shardCount;
        }
    }

    /// <summary>
    /// 【线程安全】LRU（Least Recently Used）缓存算法实现，自动移除最近最少使用的数据。使用分片锁降低高并发场景下的锁竞争，工厂在分片锁内执行以避免重复调用。
    /// </summary>
    /// <typeparam name="TKey">键类型。</typeparam>
    /// <typeparam name="TValue">值类型。</typeparam>
    public class Lru<TKey, TValue> : IEliminationAlgorithm<TKey, TValue> where TKey : notnull
    {
        private class Node
        {
            public TKey Key { get; }
            public TValue Value { get; set; }
            public Node Previous { get; set; }
            public Node Next { get; set; }

            public Node()
            {
            }

            public Node(TKey key, TValue value)
            {
                Key = key;
                Value = value;
            }
        }

        private class Shard
        {
            private readonly int _capacity;
            private readonly Func<TKey, TValue> _factory;
            private readonly Node _head;
            private readonly Node _tail;
            private readonly Dictionary<TKey, Node> _cachings;
            private readonly object _lockObj = new object();

            public Shard(int capacity, IEqualityComparer<TKey> comparer, Func<TKey, TValue> factory)
            {
                _capacity = capacity;
                _factory = factory;

                _head = new Node();
                _tail = new Node();
                _head.Next = _tail;
                _tail.Previous = _head;

                int dictCapacity = capacity;

                for (int i = 0; i < 3; i++)
                {
                    if (dictCapacity > 100 && (dictCapacity & 1) == 0)
                    {
                        dictCapacity /= 2;

                        continue;
                    }

                    break;
                }

                _cachings = new Dictionary<TKey, Node>(dictCapacity, comparer);
            }

            public int Count { get { lock (_lockObj) { return _cachings.Count; } } }

            public TValue Get(TKey key)
            {
                TValue obsoleteValue = default;

                try
                {
                    lock (_lockObj)
                    {
                        if (_cachings.TryGetValue(key, out var node))
                        {
                            MoveToHead(node);

                            return node.Value;
                        }

                        // 工厂在分片锁内执行：同一分片内相同键不会重复调用工厂
                        var value = _factory.Invoke(key);

                        // 工厂成功后再执行淘汰，异常不会导致已有数据被误淘汰
                        if (_cachings.Count >= _capacity)
                        {
                            var lastNode = RemoveTail();
                            _cachings.Remove(lastNode.Key);
                            obsoleteValue = lastNode.Value;
                        }

                        var newNode = new Node(key, value);
                        _cachings.Add(key, newNode);
                        AddToHead(newNode);

                        return value;
                    }
                }
                finally
                {
                    DisposeObsolete(obsoleteValue);
                }
            }

            public bool TryGet(TKey key, out TValue value)
            {
                lock (_lockObj)
                {
                    if (_cachings.TryGetValue(key, out var node))
                    {
                        MoveToHead(node);

                        value = node.Value;

                        return true;
                    }

                    value = default;

                    return false;
                }
            }

            public void Put(TKey key, TValue value)
            {
                TValue obsoleteValue = default;

                try
                {
                    lock (_lockObj)
                    {
                        if (_cachings.TryGetValue(key, out var node))
                        {
                            // 不同引用时释放旧值，防止资源泄漏
                            if (typeof(TValue).IsValueType || !ReferenceEquals(node.Value, value))
                            {
                                obsoleteValue = node.Value;
                            }

                            node.Value = value;

                            MoveToHead(node);

                            return;
                        }

                        if (_cachings.Count >= _capacity)
                        {
                            var lastNode = RemoveTail();
                            _cachings.Remove(lastNode.Key);
                            obsoleteValue = lastNode.Value;
                        }

                        var newNode = new Node(key, value);
                        _cachings.Add(key, newNode);
                        AddToHead(newNode);
                    }
                }
                finally
                {
                    DisposeObsolete(obsoleteValue);
                }
            }

            private void MoveToHead(Node node)
            {
                RemoveNode(node);
                AddToHead(node);
            }

            private void AddToHead(Node node)
            {
                node.Previous = _head;
                node.Next = _head.Next;
                _head.Next!.Previous = node;
                _head.Next = node;
            }

            private static void RemoveNode(Node node)
            {
                node.Previous!.Next = node.Next;
                node.Next!.Previous = node.Previous;
            }

            private Node RemoveTail()
            {
                var lastNode = _tail.Previous!;
                RemoveNode(lastNode);
                return lastNode;
            }
        }

        private readonly Shard[] _shards;
        private readonly int _shardMask;
        private readonly IEqualityComparer<TKey> _comparer;

        /// <summary>
        /// 默认容量。
        /// </summary>
        public const int DefaultCapacity = 1000;

        /// <summary>
        /// 默认容量。
        /// </summary>
        /// <param name="factory">生成 <typeparamref name="TValue"/> 的工厂。</param>
        public Lru(Func<TKey, TValue> factory) : this(DefaultCapacity, factory)
        {
        }

        /// <summary>
        /// 指定最大容量。
        /// </summary>
        /// <param name="capacity">初始大小。</param>
        /// <param name="factory">生成 <typeparamref name="TValue"/> 的工厂。</param>
        public Lru(int capacity, Func<TKey, TValue> factory) : this(capacity, null, factory)
        {
        }

        /// <summary>
        /// 指定最大容量。
        /// </summary>
        /// <param name="capacity">初始大小。</param>
        /// <param name="comparer"> 在比较集中的值时使用的 <see cref="IEqualityComparer{T}"/> 实现，或为 null 以使用集类型的默认 <seealso cref="EqualityComparer{T}"/> 实现。</param>
        /// <param name="factory">生成 <typeparamref name="TValue"/> 的工厂。</param>
        public Lru(int capacity, IEqualityComparer<TKey> comparer, Func<TKey, TValue> factory)
        {
            if (capacity <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity));
            }

            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
            _comparer = comparer ?? EqualityComparer<TKey>.Default;

            int shardCount = ComputeShardCount(capacity);
            _shardMask = shardCount - 1;

            int perShardCapacity = capacity / shardCount;
            int remainder = capacity % shardCount;

            _shards = new Shard[shardCount];

            for (int i = 0; i < shardCount; i++)
            {
                int shardCapacity = perShardCapacity + (i < remainder ? 1 : 0);
                _shards[i] = new Shard(shardCapacity, _comparer, _factory);
            }
        }

        private readonly Func<TKey, TValue> _factory;

        /// <inheritdoc />
        public int Count
        {
            get
            {
                int count = 0;

                for (int i = 0; i < _shards.Length; i++)
                {
                    count += _shards[i].Count;
                }

                return count;
            }
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <param name="key"><inheritdoc/></param>
        /// <returns>指定键使用构造函数工厂生成的值。</returns>
        public TValue Get(TKey key)
        {
            return GetShard(key).Get(key);
        }

        /// <inheritdoc/>
        public bool TryGet(TKey key, out TValue value)
        {
            return GetShard(key).TryGet(key, out value);
        }

        /// <inheritdoc/>
        public void Put(TKey key, TValue value)
        {
            GetShard(key).Put(key, value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Shard GetShard(TKey key)
        {
            var hash = _comparer.GetHashCode(key) & 0x7FFFFFFF;
            return _shards[hash & _shardMask];
        }

        private static int ComputeShardCount(int capacity)
        {
            int target = Environment.ProcessorCount;
            int shardCount = 1;

            while (shardCount < target && capacity / (shardCount * 2) >= 4)
            {
                shardCount <<= 1;
            }

            return shardCount;
        }

        private static void DisposeObsolete(TValue value)
        {
            if (value is IDisposable disposable)
            {
                disposable.Dispose();
            }
#if !NET_Traditional
            else if (value is IAsyncDisposable asyncDisposable)
            {
                var disposeTask = asyncDisposable.DisposeAsync();
                if (!disposeTask.IsCompletedSuccessfully)
                {
                    _ = disposeTask;
                }
            }
#endif
        }
    }
}
