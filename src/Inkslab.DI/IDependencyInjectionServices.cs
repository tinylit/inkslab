using Inkslab.DI.Options;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace Inkslab.DI
{
    /// <summary>
    /// 依赖注入。
    /// </summary>
    public interface IDependencyInjectionServices
    {
        /// <summary>
        /// 程序集。
        /// </summary>
        ICollection<Assembly> Assemblies { get; }

        /// <summary>
        /// 查找程序集。
        /// </summary>
        /// <param name="pattern">DLL过滤规则。<see cref="Directory.GetFiles(string, string)"/></param>
        /// <returns>依赖注入。</returns>
        IDependencyInjectionServices SeekAssemblies(string pattern = "*");

        /// <summary>
        /// 查找程序集。
        /// </summary>
        /// <param name="patterns">DLL过滤规则。<see cref="Directory.GetFiles(string, string)"/></param>
        /// <returns>依赖注入。</returns>
        IDependencyInjectionServices SeekAssemblies(params string[] patterns);

        /// <summary>
        /// 程序集。
        /// </summary>
        /// <param name="assembly">程序集。</param>
        /// <returns>依赖注入。</returns>
        IDependencyInjectionServices AddAssembly(Assembly assembly);

        /// <summary>
        /// 程序集。
        /// </summary>
        /// <param name="assemblies">程序集集合。</param>
        /// <returns>依赖注入。</returns>
        IDependencyInjectionServices AddAssemblies(params Assembly[] assemblies);

        /// <summary>
        /// 移除程序集。
        /// </summary>
        /// <param name="match">匹配规则。</param>
        /// <returns>依赖注入。</returns>
        IDependencyInjectionServices RemoveAssemblies(Predicate<Assembly> match);

        /// <summary>
        /// 查找并调用限定程序集中，所有实现 <see cref="IConfigureServices"/> 的方法 <see cref="IConfigureServices.ConfigureServices(IServiceCollection)"/> 注入约定。
        /// </summary>
        IDependencyInjectionServices ConfigureByDefined();

        /// <summary>
        /// 自动检查接口参数（属性 <see cref="DependencyInjectionOptions.DiControllerActionIsFromServicesParameters"/> 为 true 时，方法参数也会注入）自动注入，并检查是否所有参数都成功注入。
        /// </summary>
        /// <param name="options">注入配置。</param>
        IServiceCollection ConfigureByAuto(DependencyInjectionOptions options);
    }
}
