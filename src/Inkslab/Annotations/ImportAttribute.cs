using System;

namespace Inkslab.Annotations
{
    /// <summary>
    /// 导入。
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
    public class ImportAttribute : Attribute
    {
    }
}