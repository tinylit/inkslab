using Inkslab.DI;
using System;
using System.Collections.Generic;
using Inkslab.DI.Options;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// 依赖注入服务扩展。
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// 依赖注入。
        /// </summary>
        /// <param name="services">服务集合。</param>
        /// <param name="options">依赖注入配置。</param>
        /// <returns>服务集合。</returns>
        public static IDependencyInjectionServices DependencyInjection(this IServiceCollection services, DependencyInjectionOptions options)
        {
            if (services is null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (options is null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            return DependencyInjection(services, options, Array.Empty<object>());
        }

        /// <summary>
        /// 依赖注入。
        /// </summary>
        /// <param name="services">服务集合。</param>
        /// <param name="options">依赖注入配置。</param>
        /// <param name="serviceObjects">指定创建“<see cref="IConfigureServices"/>”实现的构造函数注入服务对象。</param>
        /// <returns>服务集合。</returns>
        public static IDependencyInjectionServices DependencyInjection(this IServiceCollection services, DependencyInjectionOptions options, params object[] serviceObjects)
        {
            if (services is null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (options is null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            if (serviceObjects is null)
            {
                throw new ArgumentNullException(nameof(serviceObjects));
            }

            return DependencyInjection(services, options, new ServiceObjectServiceProvider(serviceObjects));
        }

        /// <summary>
        /// 依赖注入。
        /// </summary>
        /// <param name="services">服务集合。</param>
        /// <param name="options">依赖注入配置。</param>
        /// <param name="service">指定创建“<see cref="IConfigureServices"/>”实现的构造函数注入服务。</param>
        /// <returns>服务集合。</returns>
        public static IDependencyInjectionServices DependencyInjection(this IServiceCollection services, DependencyInjectionOptions options, IServiceProvider service)
        {
            if (services is null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (options is null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            if (service is null)
            {
                throw new ArgumentNullException(nameof(service));
            }

            return new DependencyInjectionServices(services, options, service);
        }

        private class ServiceObjectServiceProvider : IServiceProvider
        {
            private readonly Dictionary<Type, object> _serviceDic = new Dictionary<Type, object>();

            public ServiceObjectServiceProvider(object[] serviceObjects)
            {
                for (int i = 0; i < serviceObjects.Length; i++)
                {
                    var serviceObj = serviceObjects[i];

                    if (serviceObj is null)
                    {
                        continue;
                    }

                    var serviceType = serviceObj.GetType();

                    _serviceDic.Add(serviceType, serviceObj);
                }

                for (int i = 0; i < serviceObjects.Length; i++)
                {
                    var serviceObj = serviceObjects[i];

                    if (serviceObj is null)
                    {
                        continue;
                    }

                    var serviceType = serviceObj.GetType();

                    foreach (var interfaceType in serviceType.GetInterfaces())
                    {
                        if (interfaceType == typeof(IDisposable) || interfaceType == typeof(IAsyncDisposable))
                        {
                            continue;
                        }

                        _serviceDic[interfaceType] = serviceObj;
                    }

                    do
                    {
                        serviceType = serviceType.BaseType;

                        if (serviceType is null || serviceType == typeof(object))
                        {
                            break;
                        }

                        _serviceDic[serviceType] = serviceObj;
                    } while (true);
                }
            }

            public object GetService(Type serviceType)
            {
                if (_serviceDic.TryGetValue(serviceType, out object serviceObj))
                {
                    return serviceObj;
                }

                return null;
            }
        }
    }
}