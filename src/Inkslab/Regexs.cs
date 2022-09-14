using System.Text.RegularExpressions;

namespace Inkslab
{
    /// <summary>
    /// 正则表达式( 字段以 “Is” 开头时，代表完全匹配；否则，仅代表包含符合规则的内容)。
    /// </summary>
    /// <example>
    /// 例如：<see cref="Regexs.IsMail"/>.IsMatch(<seealso cref="string"/>)，代表内容是邮件地址。
    /// </example>
    public static class Regexs
    {
        /// <summary>
        /// 空白符。
        /// </summary>
        public const string WHITESPACE = "[\\x20\\t\\r\\n\\f]";

        /// <summary>
        /// 中文字符。
        /// </summary>
        public const string CHINESE_CHARACTER = "[\\u4e00-\\u9fa5]";

        /// <summary>
        /// 双字节字符。
        /// </summary>
        public const string DOUBLE_BYTE_CHARACTER = "[^\\x00-\\xff]";

        /// <summary>
        /// 邮件。
        /// </summary>
        public static readonly Regex IsMail = new Regex(@"^\w[-\w.+]*@([A-Za-z0-9][-A-Za-z0-9]+\.)+[A-Za-z]{2,14}$", RegexOptions.Compiled);

        /// <summary>
        /// 数字。
        /// </summary>
        public static readonly Regex IsNumber = new Regex("^[+-]?(0|[1-9][0-9]*)(\\.[0-9]+)?$", RegexOptions.Compiled);

        /// <summary>
        /// 含空白符。
        /// </summary>
        public static readonly Regex Whitespaces = new Regex(WHITESPACE + "+", RegexOptions.Compiled);

        /// <summary>
        /// 含中文字符。
        /// </summary>
        public static readonly Regex ChineseCharacters = new Regex(CHINESE_CHARACTER + "+", RegexOptions.Compiled);

        /// <summary>
        /// 含双字节字符（含中文字符）。
        /// </summary>
        public static readonly Regex DoubleByteCharacters = new Regex(DOUBLE_BYTE_CHARACTER + "+", RegexOptions.Compiled);
    }
}
