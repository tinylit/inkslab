using Inkslab.Serialize.Json;

namespace Inkslab.Json
{
    /// <summary>
    /// Json 启动器。
    /// </summary>
    public class JStartup : IStartup
    {
        /// <summary>
        /// 代码（300）。
        /// </summary>
        public int Code => 300;

        /// <summary>
        /// 权重。
        /// </summary>
        public int Weight => 1;

        /// <summary>
        /// 启动。
        /// </summary>
        public void Startup() => RuntimeServPools.TryAddSingleton<IJsonHelper, DefaultJsonHelper>();
    }
}
