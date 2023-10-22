﻿using Inkslab;
using Inkslab.Config;
#if !NET_Traditional
using Inkslab.Config.Options;
#endif
using Inkslab.Settings;
using Inkslab.Sugars;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.RegularExpressions;

namespace System
{
    using static Expression;

    /// <summary>
    /// 字符串扩展。
    /// </summary>
    public static class StringExtentions
    {
        private static readonly Regex PatternCamelCase = new Regex("_[a-z]", RegexOptions.Singleline | RegexOptions.Compiled);

        private static readonly Regex PatternPascalCase = new Regex("(^|_)[a-z]", RegexOptions.Singleline | RegexOptions.Compiled);

        private static readonly Regex PatternUrlCamelCase = new Regex("[A-Z]", RegexOptions.Singleline | RegexOptions.Compiled);

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
#if NETSTANDARD2_1_OR_GREATER
                        string value = name[1..];
#else
                        string value = name.Substring(1);
#endif

                        return string.Concat(char.ToString(char.ToLower(name[0])), PatternCamelCase.Replace(value, x =>
                        {
                            return char.ToUpper(x.Value[1]).ToString();
                        }));
                    }

                    return PatternCamelCase.Replace(name, x => char.ToUpper(x.Value[1]).ToString());

                case NamingType.UrlCase:
                    return PatternUrlCamelCase.Replace(name, x =>
                    {
                        if (x.Index == 0)
                        {
                            return x.Value.ToLower();
                        }

                        return string.Concat("_", x.Value.ToLower());
                    });
                case NamingType.PascalCase:
                    return PatternPascalCase.Replace(name, x =>
                    {
                        if (x.Index == 0)
                        {
                            return x.Value.ToUpper();
                        }

                        return char.ToUpper(x.Value[1]).ToString();
                    });
                default:
                    throw new NotSupportedException();
            }
        }

        /// <summary> 
        /// 帕斯卡命名法。 
        /// </summary>
        /// <param name="name">名称。</param>
        /// <returns></returns>
        public static string ToPascalCase(this string name) => ToNamingCase(name, NamingType.PascalCase);

        /// <summary>
        /// 驼峰命名。
        /// </summary>
        /// <param name="name">名称。</param>
        /// <returns></returns>
        public static string ToCamelCase(this string name) => ToNamingCase(name, NamingType.CamelCase);

        /// <summary> url命名法。</summary>
        /// <param name="name">名称。</param>
        /// <returns></returns>
        public static string ToUrlCase(this string name) => ToNamingCase(name, NamingType.UrlCase);

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

#if !NET_Traditional
        /// <summary>
        /// 配置文件读取（通过<see cref="IOptions{TOptions}.Value"/> 获取最新值）。
        /// </summary>
        /// <typeparam name="T">类型。</typeparam>
        /// <param name="configName">键。</param>
        /// <param name="defaultValue">默认值。</param>
        /// <returns></returns>
        public static IOptions<T> Options<T>(this string configName, T defaultValue = default) where T : class => ConfigHelper.Options(configName, defaultValue);
#endif
        
        /// <summary>
        /// 属性格式化语法糖(语法规则由“<see cref="IStringSugar"/>”的实现决定，默认实现为“<seealse cref="DefaultStringSugar"/>”)。
        /// </summary>
        /// <param name="value">字符串。</param>
        /// <param name="source">资源。</param>
        /// <param name="namingType">比较的命名方式。</param>
        /// <returns></returns>
        public static string StringSugar(this string value, object source, NamingType namingType = NamingType.Normal) => StringSugar(value, source, new DefaultSettings(namingType));

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