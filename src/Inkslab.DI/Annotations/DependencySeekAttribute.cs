using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;

namespace Inkslab.DI.Annotations
{
    /// <summary>
    /// 依赖。
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = true)]
    public abstract class DependencySeekAttribute : Attribute
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        protected DependencySeekAttribute()
        {
        }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="lifetime">生存周期。</param>
        protected DependencySeekAttribute(ServiceLifetime lifetime)
        {
            Lifetime = lifetime;
        }

        /// <summary>
        /// 依赖项的生存周期（默认使用实现的生命周期）。
        /// </summary>
        public ServiceLifetime? Lifetime { get; }

        /// <summary>
        /// 查找更多的依赖项。
        /// </summary>
        /// <param name="implementationType">被标记类型的实现类。</param>
        /// <returns></returns>
        public abstract IEnumerable<Type> Dependencies(Type implementationType);
    }
}