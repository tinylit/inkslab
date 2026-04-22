using Inkslab;
using Inkslab.Config;
using Inkslab.Settings;
using Inkslab.Sugars;
using System.Text.RegularExpressions;

#pragma warning disable IDE0130 // 命名空间与文件夹结构不匹配
namespace System
#pragma warning restore IDE0130 // 命名空间与文件夹结构不匹配
{
    /// <summary>
    /// 字符串扩展。
    /// </summary>
    public static class StringExtensions
    {
        private static readonly Regex _patternCamelCase = new Regex("(_|-)[a-zA-Z]", RegexOptions.Singleline | RegexOptions.Compiled);

        private static readonly Regex _patternPascalCase = new Regex("(^[a-z])|(_|-)[a-zA-Z]", RegexOptions.Singleline | RegexOptions.Compiled);

        private static readonly Regex _patternSnakeCase = new Regex("[A-Z]|(-[a-zA-Z])", RegexOptions.Singleline | RegexOptions.Compiled);

        private static readonly Regex _patternKebabCase = new Regex("[A-Z]|(_[a-zA-Z])", RegexOptions.Singleline | RegexOptions.Compiled);

        //? ASCII 字母单字符字符串缓存，避免 char.ToString() 的短生命周期 string 分配。
        private static readonly string[] _asciiUpperStrings = BuildAsciiStrings(toUpper: true);
        private static readonly string[] _asciiLowerStrings = BuildAsciiStrings(toUpper: false);

        private static string[] BuildAsciiStrings(bool toUpper)
        {
            var arr = new string[128];
            for (int i = 0; i < arr.Length; i++)
            {
                char c = (char)i;
                arr[i] = (toUpper ? char.ToUpper(c) : char.ToLower(c)).ToString();
            }

            return arr;
        }

        private static string ToUpperString(char c) => c < 128 ? _asciiUpperStrings[c] : char.ToUpper(c).ToString();

        private static string ToLowerString(char c) => c < 128 ? _asciiLowerStrings[c] : char.ToLower(c).ToString();

        /// <summary>
        /// 命名。
        /// </summary>
        /// <param name="name">名称。</param>
        /// <param name="namingType">命名规则。</param>
        /// <returns></returns>
        public static string ToNamingCase(this string name, NamingType namingType)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException("名称不能为空！", nameof(name));
            }

            if (Regexs.Whitespaces.IsMatch(name))
            {
                throw new ArgumentException($"“{name}”不是一个有效的名称。", nameof(name));
            }

            switch (namingType)
            {
                case NamingType.Normal:
                    return name;
                case NamingType.CamelCase:

                    if (char.IsUpper(name[0]))
                    {
#if NET_Traditional
                        string value = name.Substring(1);
#else
                        string value = name[1..];
#endif
                        // 优化：复用 ASCII 单字符字符串缓存，避免 char.ToString() 分配
                        return char.ToLower(name[0]) + _patternCamelCase.Replace(value, x => ToUpperString(x.Value[1]));
                    }

                    return _patternCamelCase.Replace(name, x => ToUpperString(x.Value[1]));

                case NamingType.PascalCase:
                    return _patternPascalCase.Replace(name, x => x.Index == 0
                        ? x.Value.ToUpper()
                        : ToUpperString(x.Value[1])
                    );

                case NamingType.SnakeCase:
                    return _patternSnakeCase.Replace(name, x => x.Index == 0
                        ? x.Value.ToLower()
                        : "_" + (x.Value.Length == 2
                            ? ToLowerString(x.Value[1])
                            : x.Value.ToLower())
                    );

                case NamingType.KebabCase:
                    return _patternKebabCase.Replace(name, x => x.Index == 0
                        ? x.Value.ToLower()
                        : "-" + (x.Value.Length == 2
                            ? ToLowerString(x.Value[1])
                            : x.Value.ToLower())
                    );

                default:
                    throw new NotSupportedException();
            }
        }

        /// <summary> 
        /// 帕斯卡命名法。 
        /// </summary>
        /// <param name="name">名称。</param>
        public static string ToPascalCase(this string name) => ToNamingCase(name, NamingType.PascalCase);

        /// <summary> 
        /// 蛇形命名。
        /// </summary>
        /// <param name="name">名称。</param>
        public static string ToSnakeCase(this string name) => ToNamingCase(name, NamingType.SnakeCase);

        /// <summary>
        /// 驼峰命名。
        /// </summary>
        /// <param name="name">名称。</param>
        public static string ToCamelCase(this string name) => ToNamingCase(name, NamingType.CamelCase);

        /// <summary> 
        /// 短横线命名。
        /// </summary>
        /// <param name="name">名称。</param>
        public static string ToKebabCase(this string name) => ToNamingCase(name, NamingType.KebabCase);

        /// <summary>
        /// 是否为NULL。
        /// </summary>
        /// <param name="value">字符串。</param>
        /// <returns></returns>
        public static bool IsNull(this string value) => value is null;

        /// <summary>
        /// 指示指定的字符串是 null 或是 空字符串 ("")。
        /// </summary>
        /// <param name="value">字符串。</param>
        /// <returns></returns>
        public static bool IsEmpty(this string value) => value is null || value.Length == 0;

        /// <summary>
        /// 格式化字符串。
        /// </summary>
        /// <returns></returns>
        public static string Format(this string format, object arg0) => string.Format(format, arg0);

        /// <summary>
        /// 格式化字符串。
        /// </summary>
        /// <returns></returns>
        public static string Format(this string format, object arg0, object arg1) => string.Format(format, arg0, arg1);

        /// <summary>
        /// 格式化字符串。
        /// </summary>
        /// <returns></returns>
        public static string Format(this string format, object arg0, object arg1, object arg2) => string.Format(format, arg0, arg1, arg2);

        /// <summary>
        /// 格式化字符串。
        /// </summary>
        /// <returns></returns>
        public static string Format(this string format, params object[] args) => string.Format(format, args);

        /// <summary>
        /// 内容是邮箱。
        /// </summary>
        /// <param name="value">字符串。</param>
        /// <returns></returns>
        public static bool IsMail(this string value) => Regexs.IsMail.IsMatch(value);

        /// <summary>
        /// 配置文件读取。
        /// </summary>
        /// <typeparam name="T">读取数据类型。</typeparam>
        /// <param name="configName">健。</param>
        /// <param name="defaultValue">默认值。</param>
        /// <returns></returns>
        public static T Config<T>(this string configName, T defaultValue = default) => ConfigHelper.Get(configName, defaultValue);

        /// <summary>
        /// 属性格式化语法糖(语法规则由“<see cref="IStringSugar"/>”的实现决定，默认实现为“<seealse cref="DefaultStringSugar"/>”)。
        /// </summary>
        /// <param name="value">字符串。</param>
        /// <param name="source">资源。</param>
        /// <returns></returns>
        public static string StringSugar(this string value, object source) => StringSugar(value, source, Singleton<DefaultSettings>.Instance);

        /// <summary>
        /// 属性格式化语法糖(语法规则由“<see cref="IStringSugar"/>”的实现决定，默认实现为“<seealse cref="DefaultStringSugar"/>”)。
        /// </summary>
        /// <param name="value">字符串。</param>
        /// <param name="source">资源。</param>
        /// <param name="settings">属性配置。</param>
        /// <returns></returns>
        public static string StringSugar(this string value, object source, DefaultSettings settings)
        {
            if (source is null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (settings is null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            var stringSugar = SingletonPools.Singleton<IStringSugar, DefaultStringSugar>();

            var sugar = stringSugar.CreateSugar(source, settings);

            return stringSugar
                .RegularExpression
                .Replace(value, sugar.Format);
        }
    }
}