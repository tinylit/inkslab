using System;

namespace Inkslab.Map
{
    /// <summary>
    /// 映射。
    /// </summary>
    public static class Mapper
    {
        private static readonly IMapper _mapper;

        /// <summary>
        /// inheritdoc
        /// </summary>
        static Mapper() => _mapper = RuntimeServPools.Singleton<IMapper>();

        /// <summary>
        /// 对象映射。
        /// </summary>
        /// <param name="obj">数据源。</param>
        /// <typeparam name="T">目标类型。</typeparam>
        /// <returns></returns>
        public static T Map<T>(object obj) => _mapper.Map<T>(obj);

        /// <summary> 
        /// 对象映射。
        /// </summary>
        /// <param name="obj">数据源。</param>
        /// <param name="destinationType">目标类型。</param>
        /// <returns></returns>
        public static object Map(object obj, Type destinationType) => _mapper.Map(obj, destinationType);
    }
}
