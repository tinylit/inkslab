﻿using System;
using System.Xml.Serialization;

namespace Inkslab.Annotations
{
    /// <summary>
    /// 忽略的键。
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter | AttributeTargets.ReturnValue)]
    public class IgnoreAttribute : XmlIgnoreAttribute
    {
    }
}
