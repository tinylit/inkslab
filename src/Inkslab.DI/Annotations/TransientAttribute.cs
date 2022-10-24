using Microsoft.Extensions.DependencyInjection;
using System;

namespace Inkslab.DI.Annotations
{
    /// <summary>
    /// 瞬时注入。
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = false, Inherited = true)]
    public sealed class TransientAttribute : ServiceLifetimeAttribute
    {
        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public TransientAttribute() : base(ServiceLifetime.Transient) { }
    }
}
