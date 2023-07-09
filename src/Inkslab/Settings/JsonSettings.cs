using System;

namespace Inkslab.Settings
{
    /// <summary>
    /// JSON 属性设置(非数字格式的内容会加双引号)。
    /// </summary>
    public class JsonSettings : DefaultSettings
    {
        private const string DoubleQuotationMarks = "\"";

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="namingCase">命名规则。</param>
        public JsonSettings(NamingType namingCase) : base(namingCase)
        {
        }

        /// <summary>
        /// ‘null’值处理。
        /// </summary>
        /// <returns></returns>
        public override string NullValue => "null";

        /// <summary>
        /// 打包数据。
        /// </summary>
        /// <param name="value">数据。</param>
        /// <param name="typeToConvert">源数据类型。</param>
        /// <returns></returns>
        protected override string ValuePackaging(string value, Type typeToConvert)
        {
            if (typeToConvert.IsMini() && Regexs.IsNumber.IsMatch(value))
            {
                return value;
            }

            return string.Concat(DoubleQuotationMarks, value, DoubleQuotationMarks);
        }
    }
}
