using System;

namespace Inkslab.Map
{
    /// <summary>
    /// 映射启动项。
    /// </summary>
    public class MStartup : IStartup
    {
        private sealed class Mapper : IMapper
        {
            private readonly MapperInstance mapperInstance;

            public Mapper() => mapperInstance = new MapperInstance();

            public T Map<T>(object obj) => mapperInstance.Map<T>(obj);

            public object Map(object obj, Type destinationType) => mapperInstance.Map(obj, destinationType);
        }

        /// <summary>
        /// 功能码。
        /// </summary>
        public int Code => 100;

        /// <summary>
        /// 权重。
        /// </summary>
        public int Weight => 1;

        /// <summary>
        /// 启动。
        /// </summary>
        public void Startup() => SingletonPools.TryAdd<IMapper, Mapper>();
    }
}
