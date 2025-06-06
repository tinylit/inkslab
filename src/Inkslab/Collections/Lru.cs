using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Inkslab.Collections
{
    /// <summary>
    /// 【线程安全】LRU 算法，移除最近最少使用的数据。
    /// </summary>
    /// <typeparam name="T">元素类型。</typeparam>
    public class Lru<T> : IEliminationAlgorithm<T> where T : notnull
    {
        /// <summary>
        /// 双向链表节点，存储缓存项的键和值。
        /// </summary>
        private class Node
        {
            /// <summary>
            /// 节点对应的值。
            /// </summary>
            public T Value { get; }

            /// <summary>
            /// 前一个节点。
            /// </summary>
            public Node Previous { get; set; }

            /// <summary>
            /// 后一个节点。
            /// </summary>
            public Node Next { get; set; }

            /// <summary>
            /// 默认构造函数。
            /// </summary>
            public Node()
            {

            }

            /// <summary>
            /// 使用指定值初始化节点。
            /// </summary>
            /// <param name="value">值。</param>
            public Node(T value)
            {
                Value = value;
            }
        }

        /// <summary>
        /// 缓存容量上限。
        /// </summary>
        private readonly int _capacity;
        private readonly IEqualityComparer<T> _comparer;

        /// <summary>
        /// 链表头部哨兵节点（最常用）。
        /// </summary>
        private readonly Node _head;

        /// <summary>
        /// 链表尾部哨兵节点（最少用）。
        /// </summary>
        private readonly Node _tail;

        /// <summary>
        /// 用于快速查找节点的字典。
        /// </summary>
        private readonly ConcurrentDictionary<T, Node> _keys;

        /// <summary>
        /// 线程同步锁。
        /// </summary>
        private readonly object _lockObj = new object();


        /// <inheritdoc />
        public int Count => _keys.Count;

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
            if (capacity < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity));
            }

            _capacity = capacity;
            _comparer = comparer ?? EqualityComparer<T>.Default;

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

            // 初始化双向链表（带哨兵节点）
            _head = new Node();
            _tail = new Node();
            _head.Next = _tail;
            _tail.Previous = _head;
            _keys = new ConcurrentDictionary<T, Node>(concurrencyLevel, capacity, _comparer);
        }

        /// <summary>
        /// 将指定节点移动到链表头部。
        /// </summary>
        /// <param name="node">要移动的节点。</param>
        private void MoveToHead(Node node)
        {
            RemoveNode(node);
            AddToHead(node);
        }

        /// <summary>
        /// 将节点添加到链表头部。
        /// </summary>
        /// <param name="node">要添加的节点。</param>
        private void AddToHead(Node node)
        {
            node.Previous = _head;
            node.Next = _head.Next;
            _head.Next!.Previous = node;
            _head.Next = node;
        }

        /// <summary>
        /// 从链表中移除指定节点。
        /// </summary>
        /// <param name="node">要移除的节点。</param>
        private static void RemoveNode(Node node)
        {
            node.Previous!.Next = node.Next;
            node.Next!.Previous = node.Previous;
        }

        /// <summary>
        /// 移除链表尾部节点（最久未使用），并返回该节点。
        /// </summary>
        /// <returns>被移除的节点。</returns>
        private Node RemoveTail()
        {
            var lastNode = _tail.Previous!;
            RemoveNode(lastNode);
            return lastNode;
        }

        /// <inheritdoc/>
        public bool Put(T value, out T obsoleteValue)
        {
            obsoleteValue = default;

            lock (_lockObj)
            {
                if (_keys.TryGetValue(value, out var node))
                {
                    MoveToHead(node);

                    return false;
                }

                if (_keys.Count >= _capacity)
                {
                    var lastNode = RemoveTail();

                    _keys.TryRemove(lastNode.Value, out node);

                    obsoleteValue = node.Value;
                }

                var newNode = new Node(value);

                _keys[value] = newNode;

                AddToHead(newNode);

                return true;
            }
        }
    }

    /// <summary>
    /// 【线程安全】LRU（Least Recently Used）缓存算法实现，自动移除最近最少使用的数据。
    /// </summary>
    /// <typeparam name="TKey">键类型。</typeparam>
    /// <typeparam name="TValue">值类型。</typeparam>
    public class Lru<TKey, TValue> : IEliminationAlgorithm<TKey, TValue> where TKey : notnull
    {
        /// <summary>
        /// 缓存容量上限。
        /// </summary>
        private readonly int _capacity;

        private readonly Func<TKey, TValue> _factory;

        /// <summary>
        /// 链表头部哨兵节点（最常用）。
        /// </summary>
        private readonly Node _head;

        /// <summary>
        /// 链表尾部哨兵节点（最少用）。
        /// </summary>
        private readonly Node _tail;

        /// <summary>
        /// 用于快速查找节点的字典。
        /// </summary>
        private readonly ConcurrentDictionary<TKey, Node> _keys;

        /// <summary>
        /// 线程同步锁。
        /// </summary>
        private readonly object _lockObj = new object();

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
            if (capacity < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity));
            }

            _factory = factory ?? throw new ArgumentNullException(nameof(factory));

            _capacity = capacity;

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

            // 初始化双向链表（带哨兵节点）
            _head = new Node();
            _tail = new Node();
            _head.Next = _tail;
            _tail.Previous = _head;
            _keys = new ConcurrentDictionary<TKey, Node>(concurrencyLevel, capacity, comparer ?? EqualityComparer<TKey>.Default);
        }

        /// <summary>
        /// 双向链表节点，存储缓存项的键和值。
        /// </summary>
        private class Node
        {
            /// <summary>
            /// Gets the key associated with the current element.
            /// </summary>
            public TKey Key { get; }

            /// <summary>
            /// 节点对应的值。
            /// </summary>
            public TValue Value { get; set; }

            /// <summary>
            /// 前一个节点。
            /// </summary>
            public Node Previous { get; set; }

            /// <summary>
            /// 后一个节点。
            /// </summary>
            public Node Next { get; set; }

            /// <summary>
            /// 默认构造函数。
            /// </summary>
            public Node()
            {

            }

            /// <summary>
            /// 使用指定值初始化节点。
            /// </summary>
            /// <param name="key">键。</param>
            /// <param name="value">值。</param>
            public Node(TKey key, TValue value)
            {
                Key = key;
                Value = value;
            }
        }


        /// <inheritdoc />
        public int Count => _keys.Count;

        /// <summary>
        /// 将指定节点移动到链表头部。
        /// </summary>
        /// <param name="node">要移动的节点。</param>
        private void MoveToHead(Node node)
        {
            RemoveNode(node);
            AddToHead(node);
        }

        /// <summary>
        /// 将节点添加到链表头部。
        /// </summary>
        /// <param name="node">要添加的节点。</param>
        private void AddToHead(Node node)
        {
            node.Previous = _head;
            node.Next = _head.Next;
            _head.Next!.Previous = node;
            _head.Next = node;
        }

        /// <summary>
        /// 从链表中移除指定节点。
        /// </summary>
        /// <param name="node">要移除的节点。</param>
        private static void RemoveNode(Node node)
        {
            node.Previous!.Next = node.Next;
            node.Next!.Previous = node.Previous;
        }

        /// <summary>
        /// 移除链表尾部节点（最久未使用），并返回该节点。
        /// </summary>
        /// <returns>被移除的节点。</returns>
        private Node RemoveTail()
        {
            var lastNode = _tail.Previous!;

            RemoveNode(lastNode);

            return lastNode;
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <param name="key"><inheritdoc/></param>
        /// <returns>指定键使用构造函数工厂生成的值。</returns>
        public TValue Get(TKey key)
        {
            if (_keys.TryGetValue(key, out var node))
            {
                lock (_lockObj)
                {
                    MoveToHead(node);
                }

                return node.Value;
            }

            TValue obsoleteValue = default;

            try
            {
                lock (_lockObj)
                {
                    if (_keys.TryGetValue(key, out node))
                    {
                        MoveToHead(node);

                        return node.Value;
                    }

                    if (_keys.Count >= _capacity)
                    {
                        var lastNode = RemoveTail();

                        _keys.TryRemove(lastNode.Key, out _);

                        obsoleteValue = lastNode.Value;
                    }

                    var value = _factory.Invoke(key);

                    var newNode = new Node(key, value);

                    _keys[key] = newNode;

                    AddToHead(newNode);

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
            if (_keys.TryGetValue(key, out var node))
            {
                lock (_lockObj)
                {
                    MoveToHead(node);
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
                    if (_keys.TryGetValue(key, out var node))
                    {
                        node.Value = value; // 更新值

                        MoveToHead(node);

                        return;
                    }

                    if (_keys.Count >= _capacity)
                    {
                        var lastNode = RemoveTail();

                        _keys.TryRemove(lastNode.Key, out _);

                        obsoleteValue = lastNode.Value;
                    }

                    var newNode = new Node(key, value);

                    _keys[key] = newNode;

                    AddToHead(newNode);
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
    }
}
