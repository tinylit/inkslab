﻿using Inkslab.Annotations;
using Inkslab.Collections;
using System;
using System.ComponentModel;
using System.Reflection;

namespace System.Reflection
{
    /// <summary>
    /// 反射扩展。
    /// </summary>
    public static class ReflectionExtentions
    {
        private static readonly LRU<MemberInfo, string> namings = new LRU<MemberInfo, string>(x =>
        {
            var namingAttr = x.GetCustomAttribute<NamingAttribute>();

            if (namingAttr is null)
            {
                var attr = x.DeclaringType?.GetCustomAttribute<NamingAttribute>();

                return attr is null ? x.Name : x.Name.ToNamingCase(attr.NamingType);
            }

            return namingAttr.Name ?? x.Name.ToNamingCase(namingAttr.NamingType);
        });

        private static readonly LRU<MemberInfo, string> descriptions = new LRU<MemberInfo, string>(x =>
        {
            var attr = x.GetCustomAttribute<DescriptionAttribute>();

            return attr is null ? x.Name : attr.Description;
        });

        private static readonly LRU<MemberInfo, bool> ignores = new LRU<MemberInfo, bool>(x => x.IsDefined(typeof(IgnoreAttribute), true));

        /// <summary>
        /// 是否被忽略。
        /// </summary>
        /// <param name="memberInfo">成员。</param>
        /// <returns>是否忽略。</returns>
        public static bool IsIgnore(this MemberInfo memberInfo) => ignores.Get(memberInfo);

        /// <summary>
        /// 获得成员命名。
        /// </summary>
        /// <param name="memberInfo">成员。</param>
        /// <returns>命名。</returns>
        public static string GetNaming(this MemberInfo memberInfo) => namings.Get(memberInfo);

        /// <summary>
        /// 获得成员描述。
        /// </summary>
        /// <param name="memberInfo">成员。</param>
        /// <returns>描述。</returns>
        public static string GetDescription(this MemberInfo memberInfo) => descriptions.Get(memberInfo);
    }
}
