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
        /// <summary>
        /// 默认日期格式。
        /// </summary>
        public const string DefaultDateFormatString = "yyyy'-'MM'-'dd'T'HH':'mm':'ss.FFFFFFFK";

        /// <summary>
        /// 严格模式。若启用严格模式，找不到对应的属性时，抛异常；否则，按照 <see cref="PreserveSyntax"/> 规则返回。默认：true。
        /// </summary>
        public bool Strict { get; set; } = true;

        /// <summary>
        /// 当成员丢失时，保留语法。保留语法，则返回原始语法，否则，返回 “null”。默认：false。
        /// </summary>
        public bool PreserveSyntax { get; set; }

        /// <summary>
        /// ‘null’值处理。
        /// </summary>
        /// <returns></returns>
        public virtual string NullValue => string.Empty;

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
        /// <returns></returns>
        public virtual string Convert(object value)
        {
            if (value is null)
            {
                return NullValue;
            }

            switch (value)
            {
                case string text:
                    return ValuePackaging(text, typeof(string));
                case DateTime date:
                    return ValuePackaging(date.ToString(DateFormatString), typeof(DateTime));
                case IEnumerable enumerable:
                    {
                        var enumerator = enumerable.GetEnumerator();

                        if (!enumerator.MoveNext())
                        {
                            return NullValue;
                        }

                        while (enumerator.Current is null)
                        {
                            if (!enumerator.MoveNext())
                            {
                                return NullValue;
                            }
                        }

                        var sb = new StringBuilder();

                        sb.Append('[')
                            .Append(Convert(enumerator.Current));

                        while (enumerator.MoveNext())
                        {
                            if (enumerator.Current is null)
                            {
                                continue;
                            }

                            sb.Append(',')
                                .Append(Convert(enumerator.Current));
                        }

                        return sb.Append(']')
                            .ToString();
                    }
                default:
                    return ValuePackaging(value.ToString(), value.GetType());
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
