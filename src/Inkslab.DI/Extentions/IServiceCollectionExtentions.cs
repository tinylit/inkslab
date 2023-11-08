using Inkslab.DI;
using System;
using System.Collections.Generic;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// 依赖注入服务扩展。
    /// </summary>
    public static class IServiceCollectionExtentions
    {
        /// <summary>
        /// 依赖注入。
        /// </summary>
        /// <param name="services">服务集合。</param>
        /// <returns>服务集合。</returns>
        public static IDependencyInjectionServices DependencyInjection(this IServiceCollection services) => DependencyInjection(services, Array.Empty<object>());

        /// <summary>
        /// 依赖注入。
        /// </summary>
        /// <param name="services">服务集合。</param>
        /// <param name="serviceObjects">指定创建“<see cref="IConfigureServices"/>”实现的构造函数注入服务对象。</param>
        /// <returns>服务集合。</returns>
        public static IDependencyInjectionServices DependencyInjection(this IServiceCollection services, params object[] serviceObjects)
        {
            if (services is null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (serviceObjects is null)
            {
                throw new ArgumentNullException(nameof(serviceObjects));
            }

            return DependencyInjection(services, new ServiceObjectServiceProvider(serviceObjects));
        }

        /// <summary>
        /// 依赖注入。
        /// </summary>
        /// <param name="services">服务集合。</param>
        /// <param name="service">指定创建“<see cref="IConfigureServices"/>”实现的构造函数注入服务。</param>
        /// <returns>服务集合。</returns>
        public static IDependencyInjectionServices DependencyInjection(this IServiceCollection services, IServiceProvider service)
        {
            if (services is null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (service is null)
            {
                throw new ArgumentNullException(nameof(service));
            }

            return new DependencyInjectionServices(services, service);
        }

        private class ServiceObjectServiceProvider : IServiceProvider
        {
            private readonly Dictionary<Type, object> serviceDic = new Dictionary<Type, object>();

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

                    serviceDic.Add(serviceType, serviceObj);
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
                        if (interfaceType == typeof(IDisposable) | interfaceType == typeof(IAsyncDisposable))
                        {
                            continue;
                        }

                        serviceDic[interfaceType] = serviceObj;
                    }

                    do
                    {
                        serviceType = serviceType.BaseType;

                        if (serviceType is null || serviceType == typeof(object))
                        {
                            break;
                        }

                        serviceDic[serviceType] = serviceObj;

                    } while (true);
                }
            }

            public object GetService(Type serviceType)
            {
                if (serviceDic.TryGetValue(serviceType, out object serviceObj))
                {
                    return serviceObj;
                }

                return null;
            }
        }
    }
}
