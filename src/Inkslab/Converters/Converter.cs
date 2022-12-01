using System;
using System.Reflection;

namespace Inkslab.Converters
{
    /// <summary>
    /// 属性转换器。
    /// </summary>
    /// <typeparam name="T">数据类型。</typeparam>
    public abstract class Converter<T> : IConverter
    {
        /// <summary>
        /// 是否支持此类型的转换。
        /// </summary>
        /// <param name="propertyItem">属性。</param>
        /// <returns></returns>
        public bool CanConvert(PropertyInfo propertyItem) => propertyItem.PropertyType == typeof(T);

        /// <summary>
        /// 替换内容。
        /// </summary>
        /// <param name="propertyInfo">属性。</param>
        /// <param name="value">属性值。</param>
        /// <returns></returns>
        public string Convert(PropertyInfo propertyInfo, object value)
        {
            if (value is T typeValue)
            {
                return Convert(propertyInfo, typeValue);
            }

            throw new NotSupportedException();
        }

        /// <summary>
        /// 替换内容。
        /// </summary>
        /// <param name="propertyInfo">属性。</param>
        /// <param name="value">属性值。</param>
        /// <returns></returns>
        protected abstract string Convert(PropertyInfo propertyInfo, T value);
    }
}
