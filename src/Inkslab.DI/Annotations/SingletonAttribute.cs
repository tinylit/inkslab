using Microsoft.Extensions.DependencyInjection;
using System;

namespace Inkslab.DI.Annotations
{
    /// <summary>
    /// 单例注入。
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = false, Inherited = true)]
    public sealed class SingletonAttribute : ServiceLifetimeAttribute
    {
        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public SingletonAttribute() : base(ServiceLifetime.Singleton) { }
    }
}
