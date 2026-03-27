using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Inkslab.Collections
{
    /// <summary>
    /// 【线程安全】LFU 算法，移除最近最不常使用的数据。使用分片锁降低高并发场景下的锁竞争。
    /// </summary>
    /// <typeparam name="T">元素类型。</typeparam>
    public class Lfu<T> : IEliminationAlgorithm<T> where T : notnull
    {
        private class Node
        {
            public T Key { get; }
            public int Frequency { get; set; }
            public Node Previous { get; set; }
            public Node Next { get; set; }

            public Node()
            {
                Frequency = 1;
            }

            public Node(T key) : this()
            {
                Key = key;
            }
        }

        private class FrequencyList
        {
            public Node Head { get; }
            public Node Tail { get; }
            public int Count { get; set; }

            public FrequencyList()
            {
                Head = new Node();
                Tail = new Node();

                Head.Next = Tail;
                Tail.Previous = Head;

                Count = 0;
            }

            public void AddToHead(Node node)
            {
                node.Next = Head.Next;
                node.Previous = Head;
                Head.Next!.Previous = node;
                Head.Next = node;

                Count++;
            }

            public void RemoveNode(Node node)
            {
                node.Previous!.Next = node.Next;
                node.Next!.Previous = node.Previous;

                Count--;
            }

            public Node RemoveTail()
            {
                var node = Tail.Previous!;
                RemoveNode(node);
                return node;
            }

            public bool IsEmpty() => Count == 0;
        }

        private class Shard
        {
            private readonly int _capacity;
            private int _minFrequency = 1;
            private readonly Dictionary<T, Node> _cachings;
            private readonly Dictionary<int, FrequencyList> _freqToNodes;
            private readonly object _lockObj = new object();

            public Shard(int capacity, IEqualityComparer<T> comparer)
            {
                _capacity = capacity;
                _freqToNodes = new Dictionary<int, FrequencyList>
                {
                    [1] = new FrequencyList()
                };

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
                        UpdateFrequency(node);

                        return false;
                    }

                    bool flag = false;

                    if (_cachings.Count >= _capacity)
                    {
                        flag = true;

                        obsoleteValue = Evict();
                    }

                    var newNode = new Node(value);
                    _cachings.Add(value, newNode);

                    if (!_freqToNodes.TryGetValue(1, out var freq1List))
                    {
                        freq1List = new FrequencyList();
                        _freqToNodes[1] = freq1List;
                    }

                    freq1List.AddToHead(newNode);
                    _minFrequency = 1;

                    return flag;
                }
            }

            private void UpdateFrequency(Node node)
            {
                var currentFreq = node.Frequency;

                if (currentFreq == int.MaxValue)
                {
                    return; // 频率已达上限，避免溢出
                }

                var newFreq = currentFreq + 1;

                if (!_freqToNodes.TryGetValue(newFreq, out var newFreqList))
                {
                    newFreqList = new FrequencyList();
                    _freqToNodes[newFreq] = newFreqList;
                }

                if (_freqToNodes.TryGetValue(currentFreq, out var freqList))
                {
                    freqList.RemoveNode(node);

                    if (freqList.IsEmpty())
                    {
                        _freqToNodes.Remove(currentFreq);

                        if (currentFreq == _minFrequency)
                        {
                            _minFrequency = newFreq;
                        }
                    }
                }

                node.Frequency = newFreq;
                newFreqList.AddToHead(node);
            }

            private T Evict()
            {
                var minFreqList = _freqToNodes[_minFrequency];
                var nodeToRemove = minFreqList.RemoveTail();
                _cachings.Remove(nodeToRemove.Key);

                if (minFreqList.IsEmpty())
                {
                    _freqToNodes.Remove(_minFrequency);
                }

                return nodeToRemove.Key;
            }
        }

        private readonly Shard[] _shards;
        private readonly int _shardMask;
        private readonly IEqualityComparer<T> _comparer;

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
        /// <param name="comparer">比较键时要使用的 <see cref="IEqualityComparer{T}"/> 实现，或者为 null，以便为键类型使用默认的 <seealso cref="EqualityComparer{T}"/> 。</param>
        public Lfu(int capacity, IEqualityComparer<T> comparer)
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

        /// <inheritdoc />
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
    /// 【线程安全】LFU 算法，移除最近最不常使用的数据。使用分片锁降低高并发场景下的锁竞争，工厂在分片锁内执行以避免重复调用。
    /// </summary>
    /// <typeparam name="TKey">键。</typeparam>
    /// <typeparam name="TValue">值。</typeparam>
    public class Lfu<TKey, TValue> : IEliminationAlgorithm<TKey, TValue> where TKey : notnull
    {
        private class Node
        {
            public TKey Key { get; }
            public TValue Value { get; set; }
            public int Frequency { get; set; }
            public Node Previous { get; set; }
            public Node Next { get; set; }

            public Node()
            {
                Frequency = 1;
            }

            public Node(TKey key, TValue value) : this()
            {
                Key = key;
                Value = value;
            }
        }

        private class FrequencyList
        {
            public Node Head { get; }
            public Node Tail { get; }
            public int Count { get; set; }

            public FrequencyList()
            {
                Head = new Node();
                Tail = new Node();

                Head.Next = Tail;
                Tail.Previous = Head;

                Count = 0;
            }

            public void AddToHead(Node node)
            {
                node.Next = Head.Next;
                node.Previous = Head;
                Head.Next!.Previous = node;
                Head.Next = node;
                Count++;
            }

            public void RemoveNode(Node node)
            {
                node.Previous!.Next = node.Next;
                node.Next!.Previous = node.Previous;
                Count--;
            }

            public Node RemoveTail()
            {
                var node = Tail.Previous!;
                RemoveNode(node);
                return node;
            }

            public bool IsEmpty() => Count == 0;
        }

        private class Shard
        {
            private readonly int _capacity;
            private readonly Func<TKey, TValue> _factory;
            private int _minFrequency = 1;
            private readonly Dictionary<TKey, Node> _cachings;
            private readonly Dictionary<int, FrequencyList> _freqToNodes;
            private readonly object _lockObj = new object();

            public Shard(int capacity, IEqualityComparer<TKey> comparer, Func<TKey, TValue> factory)
            {
                _capacity = capacity;
                _factory = factory;
                _freqToNodes = new Dictionary<int, FrequencyList>
                {
                    [1] = new FrequencyList()
                };

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
                        if (_cachings.TryGetValue(key, out var hitNode))
                        {
                            UpdateFrequency(hitNode);
                            return hitNode.Value;
                        }

                        // 工厂在分片锁内执行：同一分片内相同键不会重复调用工厂
                        var newValue = _factory.Invoke(key);

                        // 工厂成功后再执行淘汰，异常不会导致已有数据被误淘汰
                        if (_cachings.Count >= _capacity)
                        {
                            obsoleteValue = Evict();
                        }

                        var newNode = new Node(key, newValue);
                        _cachings.Add(key, newNode);

                        if (!_freqToNodes.TryGetValue(1, out var freq1List))
                        {
                            freq1List = new FrequencyList();
                            _freqToNodes[1] = freq1List;
                        }

                        freq1List.AddToHead(newNode);
                        _minFrequency = 1;

                        return newValue;
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
                        UpdateFrequency(node);

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

                            UpdateFrequency(node);

                            return;
                        }

                        if (_cachings.Count >= _capacity)
                        {
                            obsoleteValue = Evict();
                        }

                        var newNode = new Node(key, value);
                        _cachings.Add(key, newNode);

                        if (!_freqToNodes.TryGetValue(1, out var freq1List))
                        {
                            freq1List = new FrequencyList();
                            _freqToNodes[1] = freq1List;
                        }

                        freq1List.AddToHead(newNode);
                        _minFrequency = 1;
                    }
                }
                finally
                {
                    DisposeObsolete(obsoleteValue);
                }
            }

            private void UpdateFrequency(Node node)
            {
                var currentFreq = node.Frequency;

                if (currentFreq == int.MaxValue)
                {
                    return; // 频率已达上限，避免溢出
                }

                var newFreq = currentFreq + 1;

                if (!_freqToNodes.TryGetValue(newFreq, out var newFreqList))
                {
                    newFreqList = new FrequencyList();
                    _freqToNodes[newFreq] = newFreqList;
                }

                if (_freqToNodes.TryGetValue(currentFreq, out var freqList))
                {
                    freqList.RemoveNode(node);

                    if (freqList.IsEmpty())
                    {
                        _freqToNodes.Remove(currentFreq);

                        if (currentFreq == _minFrequency)
                        {
                            _minFrequency = newFreq;
                        }
                    }
                }

                node.Frequency = newFreq;
                newFreqList.AddToHead(node);
            }

            private TValue Evict()
            {
                var minFreqList = _freqToNodes[_minFrequency];
                var nodeToRemove = minFreqList.RemoveTail();
                _cachings.Remove(nodeToRemove.Key);

                if (minFreqList.IsEmpty())
                {
                    _freqToNodes.Remove(_minFrequency);
                }

                return nodeToRemove.Value;
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
            if (capacity <= 0)
            {
                throw new ArgumentException("Capacity must be greater than 0", nameof(capacity));
            }

            _comparer = comparer ?? EqualityComparer<TKey>.Default;

            int shardCount = ComputeShardCount(capacity);
            _shardMask = shardCount - 1;

            int perShardCapacity = capacity / shardCount;
            int remainder = capacity % shardCount;

            _shards = new Shard[shardCount];

            for (int i = 0; i < shardCount; i++)
            {
                int shardCapacity = perShardCapacity + (i < remainder ? 1 : 0);
                _shards[i] = new Shard(shardCapacity, _comparer, factory);
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

        /// <summary>
        /// <inheritdoc />
        /// </summary>
        /// <param name="key"><inheritdoc /></param>
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