using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Inkslab.Exceptions;
using Microsoft.Extensions.DependencyInjection;

namespace Inkslab.DI.Options
{
    /// <summary>
    /// 依赖注入基础配置。
    /// </summary>
    public class DependencyInjectionOptions
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
        public virtual bool Ignore(Type serviceType) => serviceType.IsNotPublic || serviceType.IsNested;

        /// <summary>
        /// 解决冲突实现类。
        /// </summary>
        /// <param name="serviceType">服务类。</param>
        /// <param name="implementationTypes">实现类集合。</param>
        /// <returns>实现类。</returns>
        public virtual Type ResolveConflictingTypes(Type serviceType, List<Type> implementationTypes)
        {
            var sb = new StringBuilder(200);

            sb.Append("Service \'")
                .Append(serviceType.Name)
                .Append("\' analyzes to multiple independent service implementations[");

            for (int i = 0; i < implementationTypes.Count; i++)
            {
                if (i > 0)
                {
                    sb.Append(',');
                }

                sb.Append(implementationTypes[i].Name);
            }

            sb.Append("] (service implementations with no inheritance relationship), please specify the implementation!");

            throw new CodeException(sb.ToString());
        }
    }
}