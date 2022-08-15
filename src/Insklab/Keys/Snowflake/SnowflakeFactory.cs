using System;

namespace Insklab.Keys.Snowflake
{
    /// <summary>
    /// 雪花算法创建器。
    /// </summary>
    public class SnowflakeFactory : IKeyGenFactory
    {
        private readonly int workerId = DEFAULT_WORKER_ID;
        private readonly int datacenterId = DEFAULT_DATACENTER_ID;

        /// <summary>
        /// 默认机器ID。
        /// </summary>
        public static int DEFAULT_WORKER_ID = 0;

        /// <summary>
        /// 默认机房ID。
        /// </summary>
        public static int DEFAULT_DATACENTER_ID = 0;

        /// <summary>
        /// 构造函数。
        /// </summary>
        public SnowflakeFactory() { }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="workerId">机器ID。</param>
        /// <param name="datacenterId">机房ID。</param>
        public SnowflakeFactory(int workerId, int datacenterId)
        {
            this.workerId = workerId;
            this.datacenterId = datacenterId;
        }

        /// <summary>
        /// 创建。
        /// </summary>
        /// <returns></returns>
        public IKeyGen Create() => new SnowflakeKeyGen(workerId, datacenterId);
    }
}
