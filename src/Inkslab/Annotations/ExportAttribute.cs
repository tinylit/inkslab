using System;

namespace Inkslab.Annotations
{
    /// <summary>指定某个类型提供特定的服务。</summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface)]
    public class ExportAttribute : Attribute
    {
    }
}