namespace Inkslab.Keys
{
    /// <summary>
    /// 主键配置。
    /// </summary>
    public class KeyOptions
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="workerId">机器Id。</param>
        /// <param name="datacenterId">机房Id。</param>
        public KeyOptions(int workerId, int datacenterId)
        {
            WorkerId = workerId;
            DataCenterId = datacenterId;
        }

        /// <summary>
        /// 机器Id。
        /// </summary>
        public int WorkerId { get; }

        /// <summary>
        /// 机房Id。
        /// </summary>
        public int DataCenterId { get; }
    }
}
