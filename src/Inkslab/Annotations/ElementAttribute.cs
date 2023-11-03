using System;
using System.Xml.Serialization;

namespace Inkslab.Annotations
{
    /// <summary>
    /// 元素。
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, AllowMultiple = false)]
    public class ElementAttribute : XmlElementAttribute
    {
        /// <summary>
        /// 命名。
        /// </summary>
        /// <param name="elementName">元素名称。</param>
        public ElementAttribute(string elementName) : base(elementName)
        {
        }
    }
}
