using Inkslab.Annotations;
using Inkslab.Collections;
using System.ComponentModel;

namespace System.Reflection
{
    /// <summary>
    /// 反射扩展。
    /// </summary>
    public static class ReflectionExtensions
    {
        private static readonly LFU<MemberInfo, string> descriptions = new LFU<MemberInfo, string>(x =>
        {
            var attr = x.GetCustomAttribute<DescriptionAttribute>();

            return attr is null ? x.Name : attr.Description;
        });

        private static readonly LFU<MemberInfo, bool> ignores = new LFU<MemberInfo, bool>(x => x.IsDefined(typeof(IgnoreAttribute), true));

        /// <summary>
        /// 是否被忽略。
        /// </summary>
        /// <param name="memberInfo">成员。</param>
        /// <returns>是否忽略。</returns>
        public static bool IsIgnore(this MemberInfo memberInfo) => ignores.Get(memberInfo);

        /// <summary>
        /// 获得成员描述。
        /// </summary>
        /// <param name="memberInfo">成员。</param>
        /// <returns>描述。</returns>
        public static string GetDescription(this MemberInfo memberInfo) => descriptions.Get(memberInfo);
    }
}
