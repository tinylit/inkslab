using System;

namespace Inkslab.Map
{
    /// <summary>
    /// 映射器。
    /// </summary>
    public interface IMapper
    {
        /// <summary>
        /// 对象映射。
        /// </summary>
        /// <param name="obj">数据源。</param>
        /// <typeparam name="T">目标类型。</typeparam>
        /// <returns>映射的对象。</returns>
        T Map<T>(object obj);

        /// <summary> 
        /// 对象映射。
        /// </summary>
        /// <param name="obj">数据源。</param>
        /// <param name="conversionType">目标类型。</param>
        /// <returns>映射的对象。</returns>
        object Map(object obj, Type conversionType);
    }
}
