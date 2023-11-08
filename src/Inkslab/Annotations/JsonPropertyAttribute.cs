using System;

namespace Inkslab.Annotations
{
    /// <summary>
    /// 元素。
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, AllowMultiple = false)]
    public class JsonPropertyAttribute : Attribute
    {
        /// <summary>
        /// 命名。
        /// </summary>
        /// <param name="name">元素名称。</param>
        public JsonPropertyAttribute(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException($"“{nameof(name)}”不能为 null 或空。", nameof(name));
            }

            Name = name;
        }

        /// <summary>
        /// 元素名称。
        /// </summary>
        public string Name { get; }
    }
}
