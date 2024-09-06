#if NET6_0_OR_GREATER
using Microsoft.AspNetCore.Mvc;
#endif
using System;
using System.Reflection;

namespace Inkslab.DI.Options
{
    /// <summary>
    /// 服务依赖注入配置。
    /// </summary>
    public class DependencyInjectionServicesOptions
    {
#if NET_Traditional
        private static readonly Type _controllerType;
#else
        private static readonly Type _controllerAttrType;
        private static readonly Type _fromServicesAttrType;
#endif
        static DependencyInjectionServicesOptions()
        {
#if NET6_0_OR_GREATER
            _controllerAttrType = typeof(ControllerAttribute);
            _fromServicesAttrType = typeof(FromServicesAttribute);
#elif NETSTANDARD2_1_OR_GREATER
            _controllerAttrType = Type.GetType("Microsoft.AspNetCore.Mvc.ControllerAttribute, Microsoft.AspNetCore.Mvc.Core", false, true);
            _fromServicesAttrType = Type.GetType("Microsoft.AspNetCore.Mvc.FromServicesAttribute, Microsoft.AspNetCore.Mvc.Core", false, true);
#else
            _controllerType = Type.GetType("System.Web.Http.Controllers.IHttpController, System.Web.Http", false, true);
#endif
        }

#if NET_Traditional
        /// <summary>
        /// 自动注入服务方法参数，默认：<see langword="false"/>。
        /// </summary>
        public bool DiServicesActionIsFromServicesParameters { get; set; }
#else
        /// <summary>
        /// 自动注入服务方法参数为 true 时，生效。默认：<see langword="true"/>。
        /// </summary>
        public bool DiServicesActionIsFromServicesParameters { get; set; } = true;
#endif

        /// <summary>
        /// 是否为服务接口的类型。
        /// </summary>
        /// <param name="type">类型。</param>
        /// <returns>是否为路由接口的类型。</returns>
        public virtual bool IsServicesType(Type type)
        {
            if (type is null)
            {
                throw new ArgumentNullException(nameof(type));
            }

#if NET_Traditional
            if (_controllerType is null)
            {
                throw new NotImplementedException();
            }

            return _controllerType.IsAssignableFrom(type);
#elif NET6_0_OR_GREATER
            return type.IsDefined(_controllerAttrType, true);
#else
            if (_controllerAttrType is null)
            {
                throw new NotImplementedException();
            }

            return type.IsDefined(_controllerAttrType, true);
#endif
        }

        /// <summary>
        /// 方案参数是否需要从服务中注入。
        /// </summary>
        /// <param name="parameterInfo">路由行为方法的参数。</param>
        /// <returns>路由行为参数是否需要从服务中注入</returns>
        public virtual bool ActionParameterIsFromServices(ParameterInfo parameterInfo)
        {
#if NET_Traditional
            return false;
#elif NET6_0_OR_GREATER
            return parameterInfo.IsDefined(_fromServicesAttrType, true);
#else
            if (_fromServicesAttrType is null)
            {
                throw new NotImplementedException();
            }

            return parameterInfo.IsDefined(_fromServicesAttrType, true);
#endif
        }

        /// <summary>
        /// 单列。
        /// </summary>
        public static DependencyInjectionServicesOptions Instance => Nested.Instance;
        
        private sealed class Nested
        {
            static Nested()
            {
            }

            public static readonly DependencyInjectionServicesOptions Instance = new DependencyInjectionServicesOptions();
        }
    }
}