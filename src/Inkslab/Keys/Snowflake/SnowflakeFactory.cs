using System;

namespace Inkslab.Keys.Snowflake
{
    /// <summary>
    /// 雪花算法创建器。
    /// </summary>
    public class SnowflakeFactory : IKeyGenFactory
    {
        private readonly int _workerId = DEFAULT_WORKER_ID;
        private readonly int _datacenterId = DEFAULT_DATACENTER_ID;

        /// <summary>
        /// 默认机器ID。
        /// </summary>
        public const int DEFAULT_WORKER_ID = 0;

        /// <summary>
        /// 默认机房ID。
        /// </summary>
        public const int DEFAULT_DATACENTER_ID = 0;

        /// <summary>
        /// 构造函数。
        /// </summary>
        public SnowflakeFactory() { }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="options">配置。</param>
        public SnowflakeFactory(KeyOptions options)
        {
            if (options is null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            _workerId = options.WorkerId;
            _datacenterId = options.DataCenterId;
        }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="workerId">机器ID。</param>
        /// <param name="datacenterId">机房ID。</param>
        public SnowflakeFactory(int workerId, int datacenterId)
        {
            _workerId = workerId;
            _datacenterId = datacenterId;
        }

        /// <summary>
        /// 创建。
        /// </summary>
        /// <returns></returns>
        public IKeyGen Create() => new SnowflakeKeyGen(_workerId, _datacenterId);
    }
}
