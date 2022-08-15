using System.Reflection;

namespace Insklab.Converters
{
    /// <summary>
    /// 属性转换器。
    /// </summary>
    public interface IConverter
    {
        /// <summary>
        /// 是否支持此类型的转换。
        /// </summary>
        /// <param name="propertyInfo">属性。</param>
        /// <returns></returns>
        bool CanConvert(PropertyInfo propertyInfo);

        /// <summary>
        /// 替换内容（解决对象类型）。
        /// </summary>
        /// <param name="propertyInfo">属性。</param>
        /// <param name="value">属性值。</param>
        /// <returns></returns>
        string Convert(PropertyInfo propertyInfo, object value);
    }
}
