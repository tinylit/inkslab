using System;
using System.Xml.Serialization;

namespace Inkslab.Annotations
{
    /// <summary>
    /// 忽略的键。
    /// </summary>
    [AttributeUsage(AttributeTargets.Constructor | AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
    public class IgnoreAttribute : XmlIgnoreAttribute
    {
    }
}
