using System;

namespace Inkslab.Annotations
{
    /// <summary>Enables validation of an input DTO at a supported integration boundary.</summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = false)]
    public sealed class ValidateInputAttribute : Attribute
    {
    }
}
