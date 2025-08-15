using System;
using System.Collections.Generic;

namespace Inkslab.Collections
{
    /// <summary>
    /// 【线程安全】LFU 算法，移除最近最不常使用的数据。
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

        private readonly int _capacity;
        private int _minFrequency = 1;
        private readonly Dictionary<T, Node> _cachings;
        private readonly Dictionary<int, FrequencyList> _freqToNodes;
        private readonly object _lockObj = new object();

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
            if (capacity < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity));
            }

            _capacity = capacity;
            _freqToNodes = new Dictionary<int, FrequencyList>
            {
                [1] = new FrequencyList()
            };

            for (int i = 0; i < 3; i++)
            {
                if (capacity > 100 && (capacity & 1) == 0)
                {
                    capacity /= 2;

                    continue;
                }

                break;
            }

            _cachings = new Dictionary<T, Node>(capacity, comparer ?? EqualityComparer<T>.Default);
        }

        /// <inheritdoc />
        public int Count => _cachings.Count;

        /// <inheritdoc />
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
                _freqToNodes[1].AddToHead(newNode);
                _minFrequency = 1; // 新添加的节点频率为1

                return flag;
            }
        }

        private void UpdateFrequency(Node node)
        {
            var currentFreq = node.Frequency;
            var freqList = _freqToNodes[currentFreq];

            // 从当前频率链表移除
            freqList.RemoveNode(node);

            // 如果当前是最小频率链表且变空，更新最小频率
            if (currentFreq == _minFrequency && freqList.IsEmpty())
            {
                _minFrequency++;
            }

            // 增加节点频率
            node.Frequency++;
            var newFreq = node.Frequency;

            // 确保新频率链表存在
            if (!_freqToNodes.ContainsKey(newFreq))
            {
                _freqToNodes[newFreq] = new FrequencyList();
            }

            // 添加到新频率链表头部
            _freqToNodes[newFreq].AddToHead(node);
        }

        private T Evict()
        {
            var minFreqList = _freqToNodes[_minFrequency];
            var nodeToRemove = minFreqList.RemoveTail();
            _cachings.Remove(nodeToRemove.Key);

            // 如果最小频率链表变空，不需要立即更新_minFrequency
            // 因为下次访问会自动更新（或添加新节点会重置为1）

            return nodeToRemove.Key;
        }
    }

    /// <summary>
    /// 【线程安全】LFU 算法，移除最近最不常使用的数据。
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

        private readonly int _capacity;
        private readonly Func<TKey, TValue> _factory;
        private int _minFrequency = 1;
        private readonly Dictionary<TKey, Node> _cachings;
        private readonly Dictionary<int, FrequencyList> _freqToNodes;
        private readonly object _lockObj = new object();

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

            _capacity = capacity;
            _factory = factory;
            _freqToNodes = new Dictionary<int, FrequencyList>
            {
                [1] = new FrequencyList()
            };

            for (int i = 0; i < 3; i++)
            {
                if (capacity > 100 && (capacity & 1) == 0)
                {
                    capacity /= 2;

                    continue;
                }

                break;
            }

            _cachings = new Dictionary<TKey, Node>(capacity, comparer ?? EqualityComparer<TKey>.Default);
        }

        /// <inheritdoc />
        public int Count => _cachings.Count;

        /// <summary>
        /// <inheritdoc />
        /// </summary>
        /// <param name="key"><inheritdoc /></param>
        /// <returns>指定键使用构造函数工厂生成的值。</returns>
        public TValue Get(TKey key)
        {
            TValue obsoleteValue = default;

            try
            {
                lock (_lockObj)
                {
                    if (_cachings.TryGetValue(key, out var node))
                    {
                        UpdateFrequency(node);

                        return node.Value;
                    }

                    if (_cachings.Count >= _capacity)
                    {
                        obsoleteValue = Evict();
                    }

                    var value = _factory.Invoke(key);
                    var newNode = new Node(key, value);
                    _cachings.Add(key, newNode);
                    _freqToNodes[1].AddToHead(newNode);
                    _minFrequency = 1; // 新添加的节点频率为1

                    return value;
                }
            }
            finally
            {
                if (obsoleteValue is IDisposable disposableValue)
                {
                    disposableValue.Dispose();
                }
#if !NET_Traditional
                else if (obsoleteValue is IAsyncDisposable asyncDisposable)
                {
                    asyncDisposable.DisposeAsync().AsTask().Wait();
                }
#endif
            }
        }

        /// <inheritdoc/>
        public bool TryGet(TKey key, out TValue value)
        {
            if (_cachings.TryGetValue(key, out var node))
            {
                lock (_lockObj)
                {
                    UpdateFrequency(node);
                }

                value = node.Value;

                return true;
            }

            value = default;

            return false;
        }

        /// <inheritdoc/>
        public void Put(TKey key, TValue value)
        {
            TValue obsoleteValue = default;

            try
            {
                lock (_lockObj)
                {
                    if (_cachings.TryGetValue(key, out var node))
                    {
                        node.Value = value; // 更新值

                        UpdateFrequency(node);

                        return;
                    }

                    if (_cachings.Count >= _capacity)
                    {
                        obsoleteValue = Evict();
                    }

                    var newNode = new Node(key, value);
                    _cachings.Add(key, newNode);
                    _freqToNodes[1].AddToHead(newNode);
                    _minFrequency = 1; // 新添加的节点频率为1
                }
            }
            finally
            {
                if (obsoleteValue is IDisposable disposableValue)
                {
                    disposableValue.Dispose();
                }
#if !NET_Traditional
                else if (obsoleteValue is IAsyncDisposable asyncDisposable)
                {
                    asyncDisposable.DisposeAsync().AsTask().Wait();
                }
#endif
            }
        }

        private void UpdateFrequency(Node node)
        {
            var currentFreq = node.Frequency;
            var freqList = _freqToNodes[currentFreq];

            // 从当前频率链表移除
            freqList.RemoveNode(node);

            // 如果当前是最小频率链表且变空，更新最小频率
            if (currentFreq == _minFrequency && freqList.IsEmpty())
            {
                _minFrequency++;
            }

            // 增加节点频率
            node.Frequency++;
            var newFreq = node.Frequency;

            // 确保新频率链表存在
            if (!_freqToNodes.ContainsKey(newFreq))
            {
                _freqToNodes[newFreq] = new FrequencyList();
            }

            // 添加到新频率链表头部
            _freqToNodes[newFreq].AddToHead(node);
        }

        private TValue Evict()
        {
            var minFreqList = _freqToNodes[_minFrequency];
            var nodeToRemove = minFreqList.RemoveTail();
            _cachings.Remove(nodeToRemove.Key);

            // 如果最小频率链表变空，不需要立即更新_minFrequency
            // 因为下次访问会自动更新（或添加新节点会重置为1）
            return nodeToRemove.Value;
        }
    }
}