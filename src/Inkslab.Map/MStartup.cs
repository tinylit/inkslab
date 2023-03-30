using Inkslab.Annotations;
using System.Linq;
using System.Reflection;

namespace Inkslab.Map
{
    /// <summary>
    /// 配置启动项。
    /// </summary>
    public class MCStartup : IStartup
    {
        /// <summary>
        /// 功能码（100）。
        /// </summary>
        public int Code => 100;

        /// <summary>
        /// 权重（10）。
        /// </summary>
        public int Weight => 10;

        /// <summary>
        /// 启动。
        /// </summary>
        public void Startup()
        {
            var profileType = typeof(Profile);
            var ignoreType = typeof(IgnoreAttribute);
            var papperInstanceType = typeof(MapperInstance);
            var assemblies = AssemblyFinder.FindAll();

            foreach (var assemblyType in assemblies.SelectMany(x => x.GetTypes())
                .Where(x => x.IsClass && !x.IsAbstract && x.IsSubclassOf(profileType)))
            {
                if (assemblyType.IsIgnore())
                {
                    continue;
                }



                MapConfiguration.Instance.AddProfile(assemblyType);
            }
        }
    }

    /// <summary>
    /// 映射启动项。
    /// </summary>
    public class MStartup : IStartup
    {
        /// <summary>
        /// 功能码（100）。
        /// </summary>
        public int Code => 100;

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
