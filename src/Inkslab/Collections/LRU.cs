using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Timers;
using Timer = System.Timers.Timer;

namespace Inkslab.Collections
{
    /// <summary>
    /// 根据 LRU 算法，移除不必要的数据。
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    /// <typeparam name="TValue"></typeparam>
    public class LRU<TKey, TValue>
    {
        private readonly int capacity;
        private readonly Func<TKey, TValue> factory;

        private readonly Timer timer;

        private readonly List<Slot> slots;
        private readonly Dictionary<TKey, TValue> cachings;

        private readonly object lockObj = new object();

        private class Slot
        {
            public Slot(TKey key)
            {
                Key = key;
            }

            public TKey Key { get; }

            public DateTime ActiveTime { get; } = DateTime.Now;
        }

        private class SlotComparer : IComparer<Slot>
        {
            private SlotComparer() { }

            public int Compare(Slot x, Slot y)
            {
                if (x is null)
                {
                    if (y is null)
                    {
                        return 0;
                    }

                    return -1;
                }

                if (y is null)
                {
                    return 1;
                }

                return x.ActiveTime.CompareTo(y.ActiveTime);
            }

            public static IComparer<Slot> Instance = new SlotComparer();
        }

        private class SlotEqualityComparer : IEqualityComparer<Slot>
        {
            private SlotEqualityComparer() { }

            public bool Equals(Slot x, Slot y)
            {
                throw new NotImplementedException();
            }

            public int GetHashCode(Slot obj) => obj is null ? 0 : obj.Key.GetHashCode();


            public static IEqualityComparer<Slot> Instance = new SlotEqualityComparer();
        }

        /// <summary>
        /// 默认容量。
        /// </summary>
        public const int DefaultCapacity = 10000;

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
            this.capacity = capacity;
            this.factory = factory ?? throw new ArgumentNullException(nameof(factory));

#if NET_Traditional
            var interval = "lru-interval".Config(5D * 60D * 1000D);
#else
            var interval = "lru:interval".Config(5D * 60D * 1000D);
#endif

            slots = new List<Slot>(capacity + capacity);
            cachings = new Dictionary<TKey, TValue>(capacity);

            timer = new Timer(interval);

            timer.Elapsed += Timer_Elapsed;

            timer.Stop();
        }

        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            bool flag = true;

            var hashSet = new HashSet<TKey>();

            var now = DateTime.Now.AddMinutes(-10D);

            for (int i = slots.Count - 1; i >= 0; i--)
            {
                var slot = slots[i];

                if (slot.ActiveTime <= now)
                {
                    if (!hashSet.Contains(slot.Key))
                    {
                        if (flag = Monitor.TryEnter(lockObj)) //? 优先满足业务需求。
                        {
                            try
                            {
                                cachings.Remove(slot.Key);
                            }
                            finally
                            {
                                Monitor.Exit(lockObj);
                            }
                        }
                    }

                    if (flag)
                    {
                        slots.RemoveAt(i);
                    }
                    else //? 没能移除缓存时，不释放槽数据。
                    {
                        flag = true;
                    }

                }
                else if (!hashSet.Add(slot.Key))
                {
                    slots.RemoveAt(i);
                }
            }

            if (cachings.Count == 0)
            {
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

            try
            {
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

                    if (cachings.Count == capacity)
                    {
                        Eliminate(true);
                    }

                    cachings.Add(key, value = factory.Invoke(key));

                    return value;
                }
            }
            finally
            {
                if (!timer.Enabled)
                {
                    timer.Start();
                }

                if (slots.Count == slots.Capacity)
                {
                    Eliminate(false);
                }

                slots.Add(new Slot(key));
            }
        }

        private void Eliminate(bool deleteCaching)
        {
            var hashSet = new HashSet<TKey>();

            for (int i = slots.Count - 1; i >= 0; i--)
            {
                var slot = slots[i];

                if (hashSet.Add(slot.Key))
                {
                    if (hashSet.Count == capacity)
                    {
                        slots.RemoveRange(0, i);

                        if (deleteCaching)
                        {
                            cachings.Remove(slot.Key);
                        }

                        break;
                    }
                }
                else
                {
                    slots.RemoveAt(i);
                }
            }
        }
    }
}
