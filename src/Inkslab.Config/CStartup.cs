namespace Inkslab.Config
{
    /// <summary>
    /// 读取配置。
    /// </summary>
    public class CStartup : IStartup
    {
        /// <summary>
        /// 代码（200）。
        /// </summary>
        public int Code => 200;

        /// <summary>
        /// 权重。
        /// </summary>
        public int Weight => 1;

        /// <summary>
        /// 启动。
        /// </summary>
        public void Startup() => SingletonPools.TryAdd<IConfigHelper, DefaultConfigHelper>();
    }
}
