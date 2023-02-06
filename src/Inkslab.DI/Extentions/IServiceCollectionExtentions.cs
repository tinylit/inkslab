using Inkslab.DI;
using System;

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
        public static IDependencyInjectionServices DependencyInjection(this IServiceCollection services)
        {
            if (services is null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            return new Inkslab.DI.DependencyInjectionServices(services);
        }
    }
}
