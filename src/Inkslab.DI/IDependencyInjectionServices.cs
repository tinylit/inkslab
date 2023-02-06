using Inkslab.DI.Options;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using XServiceCollection = Inkslab.DI.Collections.IServiceCollection;

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
        /// <returns>依赖注入。</returns>
        IDependencyInjectionServices RemoveAll();

        /// <summary>
        /// 移除程序集。
        /// </summary>
        /// <param name="match">匹配规则。</param>
        /// <returns>依赖注入。</returns>
        IDependencyInjectionServices RemoveAssemblies(Predicate<Assembly> match);

        /// <summary>
        /// 调用所有实现 <see cref="IConfigureServices"/> 的方法 <see cref="IConfigureServices.ConfigureServices(XServiceCollection)"/> 注入约定。
        /// </summary>
        IDependencyInjectionServices ConfigureByDefined();

        /// <summary>
        /// 配置自动注入。
        /// </summary>
        /// <param name="options">注入配置。</param>
        IServiceCollection ConfigureByAuto(DependencyInjectionOptions options);
    }
}
