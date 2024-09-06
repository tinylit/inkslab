using System;

namespace Inkslab.Keys.Snowflake
{
    /// <summary>
    /// 雪花算法。
    /// </summary>
    public class SnowflakeKeyGen : IKeyGen
    {
        private class SnowflakeKey : Key
        {
            public SnowflakeKey(long value) : base(value) { }

            public override int WorkId => (int)(Value >> WorkerIdShift & MaxWorkerId);

            public override int DataCenterId => (int)(Value >> DatacenterIdShift & MaxDatacenterId);

            public override DateTime ToUniversalTime() => _unixEpoch.AddMilliseconds(Value >> TimestampLeftShift);
        }

        private static readonly DateTime _unixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
        private readonly long _workerId; // 这个就是代表了机器id
        private readonly long _datacenterId; // 这个就是代表了机房id

        private /* static */ long sequence; // 代表当前毫秒内已经生成了多少个主键

        private readonly Random _random = new Random();

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="workerId">机器ID。</param>
        /// <param name="datacenterId">机房ID。</param>
        public SnowflakeKeyGen(int workerId, int datacenterId)
        {
            // sanity check for workerId
            // 这儿不就检查了一下，要求就是你传递进来的机房id和机器id不能超过32，不能小于0
            if (workerId is > MaxWorkerId or < 0)
            {
                throw new ArgumentException(string.Format("worker Id can't be greater than {0} or less than 0", MaxWorkerId));
            }

            if (datacenterId is > MaxDatacenterId or < 0)
            {
                throw new ArgumentException(string.Format("datacenter Id can't be greater than {0} or less than 0", MaxDatacenterId));
            }
            _workerId = workerId;
            _datacenterId = datacenterId;
        }

        private const int WorkerIdBits = 5;
        private const int DatacenterIdBits = 5;

        // 这个是二进制运算，就是5 bit最多只能有31个数字，也就是说机器id最多只能是32以内
        private const int MaxWorkerId = -1 ^ (-1 << WorkerIdBits);

        // 这个是一个意思，就是5 bit最多只能有31个数字，机房id最多只能是32以内
        private const int MaxDatacenterId = -1 ^ (-1 << DatacenterIdBits);
        private const int SequenceBits = 12;
        private const int WorkerIdShift = SequenceBits;
        private const int DatacenterIdShift = SequenceBits + WorkerIdBits;
        private const int TimestampLeftShift = SequenceBits + WorkerIdBits + DatacenterIdBits;
        private const long SequenceMask = -1L ^ (-1L << SequenceBits);

        private /* static */ long lastTimestamp = -1L;

        private readonly object _lockObj = new object();

        /// <summary>
        /// 新ID。
        /// </summary>
        /// <returns></returns>
        public long Id()
        {
            lock (_lockObj)
            {
                long timestamp = TimeGen();

                if (timestamp < lastTimestamp)
                {
                    throw new Exception(string.Format("Clock moved backwards. Refusing to generate id for {0} milliseconds", lastTimestamp - timestamp));
                }

                // 下面是说假设在同一个毫秒内，又发送了一个请求生成一个id
                // 这个时候就得把sequence序号给递增1，最多就是4096
                if (lastTimestamp == timestamp)
                {
                    // 这个意思是说一个毫秒内最多只能有4096个数字，无论你传递多少进来，
                    //这个位运算保证始终就是在4096这个范围内，避免你自己传递个sequence超过了4096这个范围
                    sequence = (sequence + 1L) & SequenceMask;

                    if (sequence == 0L)
                    {
                        timestamp = NextGen(lastTimestamp);
                    }
                }
                else
                {
                    sequence = _random.Next(128);
                }

                lastTimestamp = timestamp;

                return (timestamp << TimestampLeftShift)
                        | (_datacenterId << DatacenterIdShift)
                        | (_workerId << WorkerIdShift)
                        | sequence;
            }
        }

        /// <summary>
        /// 生成指定键值的键。
        /// </summary>
        /// <param name="id">键值。</param>
        /// <returns></returns>
        public Key New(long id) => new SnowflakeKey(id);

        private static long TimeGen() => (long)(DateTime.UtcNow - _unixEpoch).TotalMilliseconds;

        private static long NextGen(long lastTimestamp)
        {
            long timestamp;

            do
            {
                timestamp = TimeGen();
            } while (timestamp == lastTimestamp);

            return timestamp;
        }
    }
}
