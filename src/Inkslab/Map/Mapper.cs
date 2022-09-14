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
        /// 对象克隆。
        /// </summary>
        /// <param name="obj">数据源。</param>
        /// <param name="def">默认值。</param>
        /// <typeparam name="T">目标类型。</typeparam>
        /// <returns></returns>
        public static T Copy<T>(T obj, T def = default) where T : class => _mapper.Copy(obj, def);

        /// <summary>
        /// 对象映射。
        /// </summary>
        /// <param name="obj">数据源。</param>
        /// <param name="def">默认值。</param>
        /// <typeparam name="T">目标类型。</typeparam>
        /// <returns></returns>
        public static T Map<T>(object obj, T def = default) => _mapper.Map(obj, def);

        /// <summary> 
        /// 对象映射。
        /// </summary>
        /// <param name="obj">数据源。</param>
        /// <param name="conversionType">目标类型。</param>
        /// <returns></returns>
        public static object Map(object obj, Type conversionType) => _mapper.Map(obj, conversionType);
    }
}
