using System;

namespace Inkslab.Annotations
{
    /// <summary>
    /// 不匹配。
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public sealed class MismatchAttribute : Attribute
    {
        /// <summary>
        /// 不匹配。
        /// </summary>
        /// <param name="name">不匹配的名称。</param>
        /// <exception cref="ArgumentException"><paramref name="name"/> 是 null 或空字符串或空白字符串。</exception>
        public MismatchAttribute(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException($"“{nameof(name)}”不能为 null 或空白。", nameof(name));
            }

            Name = name;
        }

        /// <summary>
        /// 名称。
        /// </summary>
        public string Name { get; }
    }
}
