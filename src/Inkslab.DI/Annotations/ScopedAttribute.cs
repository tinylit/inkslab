using Microsoft.Extensions.DependencyInjection;
using System;

namespace Inkslab.DI.Annotations
{
    /// <summary>
    /// 范围注入。
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = false, Inherited = true)]
    public class ScopedAttribute : ServiceLifetimeAttribute
    {
        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public ScopedAttribute() : base(ServiceLifetime.Scoped) { }
    }
}
