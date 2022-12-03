using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Timers;
using Timer = System.Timers.Timer;

namespace Inkslab.Collections
{
    /// <summary>
    /// LRU。
    /// </summary>
    /// <typeparam name="T">元素类型。</typeparam>
    public class LRU<T>
    {
        private int refCount = -1;

        private readonly T[] arrays;
        private readonly int capacity;

        /// <summary>
        /// 指定容器大小。
        /// </summary>
        /// <param name="capacity">容器大小。</param>
        public LRU(int capacity)
        {
            if (capacity < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity));
            }

            this.capacity = capacity;

            arrays = new T[capacity];
        }

        /// <summary>
        /// 挤出元素。
        /// </summary>
        /// <param name="value">值。</param>
        /// <param name="removeValue">被移除的值。</param>
        public bool TrySqueeze(T value, out T removeValue)
        {
            int index = Interlocked.Increment(ref refCount);

            int offset = index % capacity;

            if (index >= capacity)
            {
                removeValue = arrays[offset];

                arrays[offset] = value;

                return true;
            }

            arrays[offset] = value;

            removeValue = default(T);

            return false;
        }

        /// <summary>
        /// 清空。
        /// </summary>
        public void Clear() => Interlocked.Exchange(ref refCount, -1);
    }

    /// <summary>
    /// 【线程安全】根据 LRU 算法，移除最近最少使用的数据。
    /// </summary>
    /// <typeparam name="TKey">键。</typeparam>
    /// <typeparam name="TValue">值。</typeparam>
    public class LRU<TKey, TValue>
    {
        private readonly int capacity;
        private readonly Func<TKey, TValue> factory;
        private readonly LRU<TKey> lru;

        private readonly Timer timer;
        private bool timerIsRunning = false;

        private readonly Dictionary<TKey, Slot> cachings;

        private volatile bool processing = false;

        private readonly object lockObj = new object();

        private class Slot
        {
            public Slot(TValue value, long ticks)
            {
                Version = 1;
                Value = value;
                Ticks = ticks;
            }

            public TValue Value { get; }

            public int Version { get; set; }

            public long Ticks { get; set; }
        }

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
        public LRU(int capacity, Func<TKey, TValue> factory)
        {
            if (capacity < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity));
            }

            this.capacity = capacity;
            this.factory = factory ?? throw new ArgumentNullException(nameof(factory));

#if NET_Traditional
            var interval = "interval-lru".Config(5D * 60D * 1000D);
#else
            var interval = "interval:lru".Config(5D * 60D * 1000D);
#endif

            timer = new Timer(interval);

            timer.Elapsed += Timer_Elapsed;

            timer.Stop();

            lru = new LRU<TKey>(capacity);

            for (int i = 0; i < 3; i++)
            {
                if ((capacity & 1) == 0)
                {
                    capacity /= 2;

                    continue;
                }

                break;
            }

            cachings = new Dictionary<TKey, Slot>(capacity);
        }

        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            var now = DateTime.Now;

            var expire = now.AddMinutes(-10D);

            var expireTicks = expire.Ticks;

            bool lockTaken = false;

            Monitor.TryEnter(lockObj, ref lockTaken);

            Dictionary<TKey, Slot> snapshotCachings;

            if (lockTaken)
            {
                try
                {
                    snapshotCachings = new Dictionary<TKey, Slot>(cachings);
                }
                finally
                {
                    Monitor.Enter(lockObj);
                }
            }
            else
            {
                return;
            }

            var keys = snapshotCachings
                .Where(x => x.Value.Ticks < expireTicks)
                .Select(x => x.Key)
                .ToList();

            if (keys.Count == 0)
            {
                return;
            }

            if (Monitor.TryEnter(lockObj, 1)) //? 在保证尽量清理的条件下，避免与槽锁发生线程死锁。
            {
                try
                {
                    processing = true;

                    for (int i = 0, len = keys.Count; i < len; i++)
                    {
                        cachings.Remove(keys[i]);
                    }

                    processing = false;
                }
                finally
                {
                    Monitor.Enter(lockObj);
                }
            }

            if (timerIsRunning && cachings.Count == 0)
            {
                timerIsRunning = false;

                lru.Clear();

                timer.Stop();
            }
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

            if (lru.TrySqueeze(key, out TKey removeKey))
            {
                if (cachings.Count == capacity)
                {
                    lock (lockObj)
                    {
                        if (cachings.Count == capacity)
                        {
                            cachings.Remove(removeKey);
                        }
                    }
                }
            }

            return Get(key, DateTime.Now.Ticks);
        }

        private TValue Get(TKey key, long ticks)
        {
            if (processing) //? 数据正在被处理。
            {
                goto label_locked;
            }

            if (cachings.TryGetValue(key, out Slot slot))
            {
                if (processing) //? 数据正在被处理。
                {
                    goto label_locked;
                }

                slot.Version++;
                slot.Ticks = ticks;

                return slot.Value;
            }

label_locked:

            lock (lockObj)
            {
                if (cachings.TryGetValue(key, out slot))
                {
                    slot.Version++;
                    slot.Ticks = ticks;

                    return slot.Value;
                }

                if (!timerIsRunning)
                {
                    timerIsRunning = true;

                    timer.Start();
                }

                var value = factory.Invoke(key);

                cachings[key] = new Slot(value, ticks);

                return value;
            }
        }
    }
}
