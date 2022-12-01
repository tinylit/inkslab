using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Timers;
using Timer = System.Timers.Timer;

namespace Inkslab.Collections
{
    /// <summary>
    /// 【线程安全】根据 LRU 算法，移除时间段内使用最少的数据。
    /// </summary>
    /// <typeparam name="TKey">键。</typeparam>
    /// <typeparam name="TValue">值。</typeparam>
    public class LFU<TKey, TValue>
    {
        private readonly int capacity;
        private readonly long intervalTicks;

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
                UpdateTicks = InitTicks = ticks;
            }

            public TValue Value { get; }

            public int Version { get; set; }

            public long InitTicks { get; set; }

            public long UpdateTicks { get; set; }
        }

        /// <summary>
        /// 默认容量。
        /// </summary>
        public const int DefaultCapacity = 1000;

        /// <summary>
        /// 构造函数。
        /// </summary>
        public LFU()
        {
#if NET_Traditional
            var interval = "interval-lfu".Config(5D * 60D * 1000D);
#else
            var interval = "interval:lfu".Config(5D * 60D * 1000D);
#endif

            timer = new Timer(interval);

            timer.Elapsed += Timer_Elapsed;

            timer.Stop();

            capacity = DefaultCapacity;

            intervalTicks = (long)(interval * TimeSpan.TicksPerMillisecond);

            cachings = new Dictionary<TKey, Slot>(capacity / 8);
        }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="capacity">最大容量。</param>
        public LFU(int capacity)
        {
#if NET_Traditional
            var interval = "interval-lfu".Config(5D * 60D * 1000D);
#else
            var interval = "interval:lfu".Config(5D * 60D * 1000D);
#endif

            timer = new Timer(interval);

            timer.Elapsed += Timer_Elapsed;

            timer.Stop();

            intervalTicks = (long)(interval * TimeSpan.TicksPerMillisecond);

            cachings = new Dictionary<TKey, Slot>(this.capacity = capacity);
        }

        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            var now = DateTime.Now;

            var expire = now.AddMinutes(-10D);

            var expireTicks = expire.Ticks;

            var keys = cachings
                    .Where(x => x.Value.UpdateTicks < expireTicks)
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
        /// <param name="factory">创建值的工厂。</param>
        /// <returns>指定键使用构造函数工厂生成的值。</returns>
        public TValue GetOrCreate(TKey key, Func<TKey, TValue> factory)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            if (factory is null)
            {
                throw new ArgumentNullException(nameof(factory));
            }

            return GetOrCreate(key, factory, DateTime.Now.Ticks);
        }

        private TValue GetOrCreate(TKey key, Func<TKey, TValue> factory, long ticks)
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

                if (ticks > slot.InitTicks + intervalTicks)
                {
                    slot.Version /= 2;
                    slot.InitTicks += intervalTicks / 2;
                }

                slot.Version++;
                slot.UpdateTicks = ticks;

                return slot.Value;
            }

label_locked:

            lock (lockObj)
            {
                if (cachings.TryGetValue(key, out slot))
                {
                    if (ticks > slot.InitTicks + intervalTicks)
                    {
                        slot.Version /= 2;
                        slot.InitTicks += intervalTicks / 2;
                    }

                    slot.Version++;

                    return slot.Value;
                }

                if (cachings.Count == capacity)
                {
                    var keys = cachings
                        .OrderByDescending(x => (ticks - x.Value.InitTicks) / x.Value.Version)
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
