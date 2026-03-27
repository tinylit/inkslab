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

                if (!_freqToNodes.TryGetValue(1, out var freq1List))
                {
                    freq1List = new FrequencyList();
                    _freqToNodes[1] = freq1List;
                }

                freq1List.AddToHead(newNode);
                _minFrequency = 1; // 新添加的节点频率为1

                return flag;
            }
        }

        private void UpdateFrequency(Node node)
        {
            var currentFreq = node.Frequency;
            var newFreq = currentFreq + 1;

            // 确保新频率链表存在（在修改状态之前创建，保证原子性）
            if (!_freqToNodes.TryGetValue(newFreq, out var newFreqList))
            {
                newFreqList = new FrequencyList();
                _freqToNodes[newFreq] = newFreqList;
            }

            // 从当前频率链表移除
            if (_freqToNodes.TryGetValue(currentFreq, out var freqList))
            {
                freqList.RemoveNode(node);

                // 清理空的频率链表，防止内存膨胀
                if (freqList.IsEmpty())
                {
                    _freqToNodes.Remove(currentFreq);

                    if (currentFreq == _minFrequency)
                    {
                        _minFrequency = newFreq;
                    }
                }
            }

            // 更新节点频率并添加到新频率链表
            node.Frequency = newFreq;
            newFreqList.AddToHead(node);
        }

        private T Evict()
        {
            var minFreqList = _freqToNodes[_minFrequency];
            var nodeToRemove = minFreqList.RemoveTail();
            _cachings.Remove(nodeToRemove.Key);

            // 清理空的频率链表，防止内存膨胀
            if (minFreqList.IsEmpty())
            {
                _freqToNodes.Remove(_minFrequency);
            }

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

                    if (!_freqToNodes.TryGetValue(1, out var freq1List))
                    {
                        freq1List = new FrequencyList();
                        _freqToNodes[1] = freq1List;
                    }

                    freq1List.AddToHead(newNode);
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
                    try
                    {
                        asyncDisposable.DisposeAsync().GetAwaiter().GetResult();
                    }
                    catch
                    {
                        // 忽略释放异常，避免破坏主流程
                    }
                }
#endif
            }
        }

        /// <inheritdoc/>
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

                    if (!_freqToNodes.TryGetValue(1, out var freq1List))
                    {
                        freq1List = new FrequencyList();
                        _freqToNodes[1] = freq1List;
                    }

                    freq1List.AddToHead(newNode);
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
                    try
                    {
                        asyncDisposable.DisposeAsync().GetAwaiter().GetResult();
                    }
                    catch
                    {
                        // 忽略释放异常，避免破坏主流程
                    }
                }
#endif
            }
        }

        private void UpdateFrequency(Node node)
        {
            var currentFreq = node.Frequency;
            var newFreq = currentFreq + 1;

            // 确保新频率链表存在（在修改状态之前创建，保证原子性）
            if (!_freqToNodes.TryGetValue(newFreq, out var newFreqList))
            {
                newFreqList = new FrequencyList();
                _freqToNodes[newFreq] = newFreqList;
            }

            // 从当前频率链表移除
            if (_freqToNodes.TryGetValue(currentFreq, out var freqList))
            {
                freqList.RemoveNode(node);

                // 清理空的频率链表，防止内存膨胀
                if (freqList.IsEmpty())
                {
                    _freqToNodes.Remove(currentFreq);

                    if (currentFreq == _minFrequency)
                    {
                        _minFrequency = newFreq;
                    }
                }
            }

            // 更新节点频率并添加到新频率链表
            node.Frequency = newFreq;
            newFreqList.AddToHead(node);
        }

        private TValue Evict()
        {
            var minFreqList = _freqToNodes[_minFrequency];
            var nodeToRemove = minFreqList.RemoveTail();
            _cachings.Remove(nodeToRemove.Key);

            // 清理空的频率链表，防止内存膨胀
            if (minFreqList.IsEmpty())
            {
                _freqToNodes.Remove(_minFrequency);
            }

            return nodeToRemove.Value;
        }
    }
}