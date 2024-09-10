using System.Linq;
using System.Reflection;

namespace Inkslab.Map
{
    /// <summary>
    /// 映射启动项。
    /// </summary>
    public class MStartup : IStartup
    {
        /// <summary>
        /// 功能码（150）。
        /// </summary>
        public int Code => 150;

        /// <summary>
        /// 权重（1）。
        /// </summary>
        public int Weight => 1;

        /// <summary>
        /// 启动。
        /// </summary>
        public void Startup() => SingletonPools.TryAdd<IMapper, MapperInstance>();
    }
}
