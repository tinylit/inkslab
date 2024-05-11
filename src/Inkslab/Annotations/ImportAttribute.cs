using System;

namespace Inkslab.Annotations
{
    /// <summary>
    /// 导入。
    /// </summary>
    [AttributeUsage(AttributeTargets.Constructor | AttributeTargets.Property | AttributeTargets.Field)]
    public class ImportAttribute : Attribute
    {
    }
}