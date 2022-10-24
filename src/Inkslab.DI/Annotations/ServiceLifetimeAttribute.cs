using Microsoft.Extensions.DependencyInjection;
using System;

namespace Inkslab.DI.Annotations
{
    /// <summary>
    /// 服务生存周期。
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = false, Inherited = true)]
    public class ServiceLifetimeAttribute : Attribute
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="lifetime">生存周期。</param>
        public ServiceLifetimeAttribute(ServiceLifetime lifetime)
        {
            Lifetime = lifetime;
        }

        /// <summary>
        /// 服务生存周期。
        /// </summary>
        public ServiceLifetime Lifetime { get; }
    }
}
