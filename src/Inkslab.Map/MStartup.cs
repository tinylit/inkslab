﻿using System.Linq;
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
        /// 权重（1）。
        /// </summary>
        public int Weight => 1;

        /// <summary>
        /// 启动。
        /// </summary>
        public void Startup()
        {
            var profileType = typeof(Profile);
            var mapperInstanceType = typeof(MapperInstance);
            
            var assemblies = AssemblyFinder.FindAll();

            foreach (var assemblyType in assemblies.SelectMany(x => x.GetTypes())
                .Where(x => x.IsClass && !x.IsAbstract && x.IsSubclassOf(profileType)))
            {
                if (assemblyType.IsIgnore())
                {
                    continue;
                }

                if (assemblyType.IsSubclassOf(mapperInstanceType)) // 默认不添加全局配置。
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
