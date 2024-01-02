using System;
using System.Collections.Generic;
using System.Reflection;
using Inkslab.Exceptions;
using Microsoft.Extensions.DependencyInjection;

namespace Inkslab.DI.Options
{
    /// <summary>
    /// 依赖注入基础配置。
    /// </summary>
    public class DependencyInjectionBaseOptions
    {
        /// <summary>
        /// 最大依赖注入深度，默认：8。
        /// </summary>
        public int MaxDepth { get; set; } = 8;

        /// <summary>
        /// 参数注入的声明周期，默认：<see cref="ServiceLifetime.Scoped"/>。
        /// </summary>
        public ServiceLifetime Lifetime { get; set; } = ServiceLifetime.Scoped;
        
        /// <summary>
        /// 过滤服务类型。
        /// </summary>
        /// <param name="serviceType">服务类型。</param>
        /// <returns>是否过滤。</returns>
        public virtual bool Ignore(Type serviceType) => serviceType.IsNotPublic || serviceType.IsNested || serviceType.IsIgnore();

        /// <summary>
        /// 解决冲突实现类。
        /// </summary>
        /// <param name="serviceType">服务类。</param>
        /// <param name="implementationTypes">实现类集合。</param>
        /// <returns>实现类。</returns>
        public virtual Type ResolveConflictingTypes(Type serviceType, List<Type> implementationTypes) => throw new CodeException($"Service '{serviceType.Name}' analyzes to multiple independent service implementations (service implementations with no inheritance relationship), please specify the implementation!");
    }
}