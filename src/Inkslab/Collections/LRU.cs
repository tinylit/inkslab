using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Timers;
using Timer = System.Timers.Timer;

namespace Inkslab.Collections
{
    /// <summary>
    /// 【线程安全】根据 LRU 算法，移除不必要的数据。
    /// </summary>
    /// <typeparam name="TKey">键。</typeparam>
    /// <typeparam name="TValue">值。</typeparam>
    public class LRU<TKey, TValue>
    {
        private readonly int capacity;
        private readonly Func<TKey, TValue> factory;

        private readonly Timer timer;
        private bool timerIsRunning = false;

        private readonly Dictionary<TKey, Slot> cachings;

        private volatile bool processing = false;

        private readonly object lockObj = new object();
        private readonly object lockSolt = new object();

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

            cachings = new Dictionary<TKey, Slot>(capacity);

            timer = new Timer(interval);

            timer.Elapsed += Timer_Elapsed;

            timer.Stop();
        }

        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (Monitor.TryEnter(lockSolt)) //? 正在处理，定时任务放弃本次执行。
            {
                try
                {
                    var now = DateTime.Now;

                    var expire = now.AddMinutes(-10D);

                    var expireTicks = expire.Ticks;

                    var keys = cachings
                            .Where(x => x.Value.Ticks < expireTicks)
                            .Select(x => x.Key)
                            .ToList();

                    processing = true;

                    lock (lockObj)
                    {
                        for (int i = 0, len = keys.Count; i < len; i++)
                        {
                            cachings.Remove(keys[i]);
                        }
                    }

                    processing = false;

                    if (timerIsRunning && cachings.Count == 0)
                    {
                        timerIsRunning = false;

                        timer.Stop();
                    }
                }
                finally
                {
                    Monitor.Exit(lockSolt);
                }
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

            var ticks = DateTime.Now.Ticks;

            if (processing)
            {
                lock (lockSolt)
                {
                    return GetValue(key, ticks);
                }
            }

            return GetValue(key, ticks);
        }

        private TValue GetValue(TKey key, long ticks)
        {
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

                if (cachings.Count == capacity)
                {
                    lock (lockSolt)
                    {
                        if (cachings.Count == capacity)
                        {
                            var keys = cachings
                                .OrderByDescending(x => x.Value.Ticks / x.Value.Version)
                                .Select(x => x.Key)
                                .Take(Math.Max(capacity / 10, 1))
                                .ToList();

                            processing = true;

                            for (int i = 0, len = keys.Count; i < len; i++)
                            {
                                cachings.Remove(keys[i]);
                            }

                            processing = false;
                        }
                    }
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
