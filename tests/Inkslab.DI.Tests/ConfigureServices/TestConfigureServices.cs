using Inkslab.DI.Tests.DependencyInjections;
using Microsoft.Extensions.DependencyInjection;

namespace Inkslab.DI.Tests.ConfigureServices
{
    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public class TestConfigureServices : IConfigureServices
    {
        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <param name="services">服务配置。</param>
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton(SingletonTest.Instance);
        }
    }
}
