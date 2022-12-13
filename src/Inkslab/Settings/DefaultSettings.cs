using System;
using System.Collections;
using System.Text;

namespace Inkslab.Settings
{

    /// <summary>
    /// 属性设置。
    /// </summary>
    public class DefaultSettings
    {
        private readonly NamingType namingCase;

        /// <summary>
        /// 默认日期格式。
        /// </summary>
        public const string DefaultDateFormatString = "yyyy'-'MM'-'dd'T'HH':'mm':'ss.FFFFFFFK";

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="namingCase">命名规则。</param>
        public DefaultSettings(NamingType namingCase) => this.namingCase = namingCase;

        /// <summary>
        /// 严格模式。若启用严格模式，找不到对应的属性时，抛异常；否则，返回空字符串。默认：false。
        /// </summary>
        public bool Strict { get; set; }

        /// <summary>
        /// ‘null’值处理。
        /// </summary>
        /// <returns></returns>
        public virtual string NullValue => string.Empty;

        /// <summary>
        /// 命名规范。默认：否。
        /// </summary>
        public NamingType NamingCase => namingCase;

        private string dateFormatString;

        /// <summary>
        /// 获取或设置如何系统。DateTime和系统。格式化DateTimeOffset值,写入JSON文本时，以及读取JSON文本时的期望日期格式。默认值是"yyyy'-'MM'-'dd'T'hh ':' MM':'ss.FFFFFFFK"。
        /// </summary>
        public string DateFormatString
        {
            get => dateFormatString ?? DefaultDateFormatString;
            set => dateFormatString = value;
        }

        /// <summary>
        /// 数据解决。
        /// </summary>
        /// <param name="value">内容。</param>
        /// <param name="packaging">包装数据。</param>
        /// <returns></returns>
        public string Convert(object value, bool packaging = true)
        {
            if (value is null)
            {
                if (packaging)
                {
                    return NullValue;
                }

                return null;
            }

            switch (value)
            {
                case string text:
                    return packaging ? ValuePackaging(text, typeof(string)) : text;
                case DateTime date:
                    return packaging ? ValuePackaging(date.ToString(DateFormatString), typeof(DateTime)) : date.ToString(DateFormatString);
                case IEnumerable enumerable:
                    {
                        var enumerator = enumerable.GetEnumerator();

                        if (!enumerator.MoveNext())
                        {
                            if (packaging)
                            {
                                return NullValue;
                            }

                            return null;
                        }

                        while (enumerator.Current is null)
                        {
                            if (!enumerator.MoveNext())
                            {
                                if (packaging)
                                {
                                    return NullValue;
                                }

                                return null;
                            }
                        }

                        var sb = new StringBuilder();

                        sb.Append("[")
                            .Append(Convert(enumerator.Current, true));

                        while (enumerator.MoveNext())
                        {
                            if (enumerator.Current is null)
                            {
                                continue;
                            }

                            sb.Append(",")
                                .Append(Convert(enumerator.Current, true));
                        }

                        return sb.Append("]")
                            .ToString();
                    }
                default:
                    return packaging ? ValuePackaging(value.ToString(), value.GetType()) : value.ToString();
            }
        }

        /// <summary>
        /// 打包数据。
        /// </summary>
        /// <param name="value">数据。</param>
        /// <param name="typeToConvert">源数据类型。</param>
        /// <returns></returns>
        protected virtual string ValuePackaging(string value, Type typeToConvert) => value;
    }
}
