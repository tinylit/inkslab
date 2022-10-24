using Inkslab.DI.Collections;

namespace Inkslab.DI
{
    /// <summary>
    /// 配置服务。
    /// </summary>
    public interface IConfigureServices
    {
        /// <summary>
        /// 服务配置。
        /// </summary>
        /// <param name="services"></param>
        void ConfigureServices(IServiceCollection services);
    }
}
