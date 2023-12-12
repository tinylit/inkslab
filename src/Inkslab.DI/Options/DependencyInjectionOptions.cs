using Insklab.Exceptions;
#if NET6_0_OR_GREATER
using Microsoft.AspNetCore.Mvc;
#endif
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Inkslab.DI.Options
{
    /// <summary>
    /// 依赖注入配置。
    /// </summary>
    public class DependencyInjectionOptions : Singleton<DependencyInjectionOptions>
    {
#if NET_Traditional
        private static readonly Type controllerType;
#else
        private static readonly Type controllerAttrType;
        private static readonly Type fromServicesAttrType;
#endif
        static DependencyInjectionOptions()
        {
#if NET6_0_OR_GREATER
            controllerAttrType = typeof(ControllerAttribute);
            fromServicesAttrType = typeof(FromServicesAttribute);
#elif NETSTANDARD2_1_OR_GREATER
            controllerAttrType = Type.GetType("Microsoft.AspNetCore.Mvc.ControllerAttribute, Microsoft.AspNetCore.Mvc.Core", false, true);
            fromServicesAttrType = Type.GetType("Microsoft.AspNetCore.Mvc.FromServicesAttribute, Microsoft.AspNetCore.Mvc.Core", false, true);
#else
            controllerType = Type.GetType("System.Web.Http.Controllers.IHttpController, System.Web.Http", false, true);
#endif
        }

        /// <summary>
        /// 最大依赖注入深度，默认：8。
        /// </summary>
        public int MaxDepth { get; set; } = 8;

        /// <summary>
        /// 参数注入的声明周期，默认：<see cref="ServiceLifetime.Scoped"/>。
        /// </summary>
        public ServiceLifetime Lifetime { get; set; } = ServiceLifetime.Scoped;

#if NET_Traditional
        /// <summary>
        /// 自动注入接口行为参数，默认：<see langword="false"/>。
        /// </summary>
        public bool DiControllerActionIsFromServicesParameters { get; set; }
#else
        /// <summary>
        /// 自动注入接口行为参数为 true 时，生效。默认：<see langword="true"/>。
        /// </summary>
        public bool DiControllerActionIsFromServicesParameters { get; set; } = true;
#endif

        /// <summary>
        /// 是否为路由接口的类型。
        /// </summary>
        /// <param name="type">类型。</param>
        /// <returns>是否为路由接口的类型。</returns>
        public virtual bool IsControllerType(Type type)
        {
            if (type is null)
            {
                throw new ArgumentNullException(nameof(type));
            }

#if NET_Traditional
            if (controllerType is null)
            {
                throw new NotImplementedException();
            }

            return controllerType.IsAssignableFrom(type);
#elif NET6_0_OR_GREATER
            return type.IsDefined(controllerAttrType, true);
#else
            if (controllerAttrType is null)
            {
                throw new NotImplementedException();
            }

            return type.IsDefined(controllerAttrType, true);
#endif
        }

        /// <summary>
        /// 路由行为参数是否需要从服务中注入。
        /// </summary>
        /// <param name="parameterInfo">路由行为方法的参数。</param>
        /// <returns>路由行为参数是否需要从服务中注入</returns>
        public virtual bool ActionParameterIsFromServices(ParameterInfo parameterInfo)
        {
#if NET_Traditional
            return false;
#elif NET6_0_OR_GREATER
            return parameterInfo.IsDefined(fromServicesAttrType, true);
#else
            if (fromServicesAttrType is null)
            {
                throw new NotImplementedException();
            }

            return parameterInfo.IsDefined(fromServicesAttrType, true);
#endif
        }

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
