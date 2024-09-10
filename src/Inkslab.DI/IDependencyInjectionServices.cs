using System;
using Inkslab.DI.Options;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Inkslab.Annotations;

namespace Inkslab.DI
{
    /// <summary>
    /// 依赖注入。
    /// </summary>
    public interface IDependencyInjectionServices : IDisposable
    {
        /// <summary>
        /// 程序集。
        /// </summary>
        IReadOnlyCollection<Assembly> Assemblies { get; }

        /// <summary>
        /// 添加程序集。
        /// </summary>
        /// <param name="assembly">程序集。</param>
        /// <returns>依赖注入服务。</returns>
        IDependencyInjectionServices AddAssembly(Assembly assembly);

        /// <summary>
        /// 查找程序集。
        /// </summary>
        /// <param name="pattern">DLL过滤规则。<see cref="Directory.GetFiles(string, string)"/></param>
        /// <returns>依赖注入服务。</returns>
        IDependencyInjectionServices SeekAssemblies(string pattern = "*");

        /// <summary>
        /// 查找程序集。
        /// </summary>
        /// <param name="patterns">DLL过滤规则。<see cref="Directory.GetFiles(string, string)"/></param>
        /// <returns>依赖注入服务。</returns>
        IDependencyInjectionServices SeekAssemblies(params string[] patterns);

        /// <summary>
        /// 忽略该类型的注入(仅对“<see cref="ServiceDescriptor.ServiceType"/>”的过滤有效)。
        /// </summary>
        /// <param name="serviceType">服务类型。</param>
        /// <returns>依赖注入服务。</returns>
        IDependencyInjectionServices IgnoreType(Type serviceType);

        /// <summary>
        /// 忽略该类型的注入(仅对“<see cref="ServiceDescriptor.ServiceType"/>”的过滤有效)。
        /// </summary>
        /// <typeparam name="TService">服务类型。</typeparam>
        /// <returns>依赖注入服务。</returns>
        IDependencyInjectionServices IgnoreType<TService>();

        /// <summary>
        /// 查找并调用限定程序集中，所有实现 <see cref="IConfigureServices"/> 的方法 <see cref="IConfigureServices.ConfigureServices(IServiceCollection)"/> 注入约定（含私有类）。
        /// </summary>
        /// <returns>依赖注入服务。</returns>
        IDependencyInjectionServices ConfigureByDefined();

        /// <summary>
        /// 自动检查接口参数（属性 <see cref="DependencyInjectionServicesOptions.DiServicesActionIsFromServicesParameters"/> 为 <see langword="true"/> 时，方法参数也会注入）自动注入，并检查是否所有参数都成功注入。
        /// </summary>
        /// <param name="servicesOptions">注入配置。</param>
        /// <returns>依赖注入服务。</returns>
        IDependencyInjectionServices ConfigureServices(DependencyInjectionServicesOptions servicesOptions);

        /// <summary>
        /// 审查，遍历服务中的注入服务类，检查服务类的注入情况，补全依赖注入关系，确保服务能苟正常注入。
        /// </summary>
        /// <param name="match"></param>
        /// <returns></returns>
        IDependencyInjectionServices ConfigureByExamine(Predicate<Type> match);

        /// <summary>
        /// 自动检查接口参数，注入标记“<see cref="ExportAttribute"/>”的类型。
        /// </summary>
        /// <returns>依赖注入服务。</returns>
        IDependencyInjectionServices ConfigureByAuto();

        /// <summary>
        /// 配置服务（自动注入服务需要的所有依赖项），使用注入配置的声明周期注入。
        /// </summary>
        /// <typeparam name="TService">服务类型。</typeparam>
        /// <returns>依赖注入服务。</returns>
        IDependencyInjectionServices Add<TService>() where TService : class;

        /// <summary>
        /// 配置服务（自动注入服务需要的所有依赖项），使用注入配置的声明周期注入。
        /// </summary>
        /// <param name="serviceType">服务类型。</param>
        /// <returns>依赖注入服务。</returns>
        IDependencyInjectionServices Add(Type serviceType);

        /// <summary>
        /// 配置服务（自动注入服务需要的所有依赖项），使用注入配置的声明周期注入。
        /// </summary>
        /// <typeparam name="TService">服务类型。</typeparam>
        /// <typeparam name="TImplementation">服务实现。</typeparam>
        /// <returns>依赖注入服务。</returns>
        IDependencyInjectionServices Add<TService, TImplementation>() where TService : class where TImplementation : TService;

        /// <summary>
        /// 配置服务（自动注入服务需要的所有依赖项），使用注入配置的声明周期注入。
        /// </summary>
        /// <param name="serviceType">服务类型。</param>
        /// <param name="implementationType">服务实现。</param>
        /// <returns>依赖注入服务。</returns>
        IDependencyInjectionServices Add(Type serviceType, Type implementationType);

        /// <summary>
        /// 配置服务（自动注入服务需要的所有依赖项）。
        /// </summary>
        /// <typeparam name="TService">服务类型。</typeparam>
        /// <returns>依赖注入服务。</returns>
        IDependencyInjectionServices AddTransient<TService>() where TService : class;

        /// <summary>
        /// 配置服务（自动注入服务需要的所有依赖项）。
        /// </summary>
        /// <param name="serviceType">服务类型。</param>
        /// <returns>依赖注入服务。</returns>
        IDependencyInjectionServices AddTransient(Type serviceType);

        /// <summary>
        /// 配置服务（自动注入服务需要的所有依赖项）。
        /// </summary>
        /// <typeparam name="TService">服务类型。</typeparam>
        /// <typeparam name="TImplementation">服务实现。</typeparam>
        /// <returns>依赖注入服务。</returns>
        IDependencyInjectionServices AddTransient<TService, TImplementation>() where TService : class where TImplementation : TService;

        /// <summary>
        /// 配置服务（自动注入服务需要的所有依赖项）。
        /// </summary>
        /// <param name="serviceType">服务类型。</param>
        /// <param name="implementationType">服务实现。</param>
        /// <returns>依赖注入服务。</returns>
        IDependencyInjectionServices AddTransient(Type serviceType, Type implementationType);

        /// <summary>
        /// 配置服务（自动注入服务需要的所有依赖项）。
        /// </summary>
        /// <typeparam name="TService">服务类型。</typeparam>
        /// <returns>依赖注入服务。</returns>
        IDependencyInjectionServices AddScoped<TService>() where TService : class;

        /// <summary>
        /// 配置服务（自动注入服务需要的所有依赖项）。
        /// </summary>
        /// <param name="serviceType">服务类型。</param>
        /// <returns>依赖注入服务。</returns>
        IDependencyInjectionServices AddScoped(Type serviceType);

        /// <summary>
        /// 配置服务（自动注入服务需要的所有依赖项）。
        /// </summary>
        /// <typeparam name="TService">服务类型。</typeparam>
        /// <typeparam name="TImplementation">服务实现。</typeparam>
        /// <returns>依赖注入服务。</returns>
        IDependencyInjectionServices AddScoped<TService, TImplementation>() where TService : class where TImplementation : TService;

        /// <summary>
        /// 配置服务（自动注入服务需要的所有依赖项）。
        /// </summary>
        /// <param name="serviceType">服务类型。</param>
        /// <param name="implementationType">服务实现。</param>
        /// <returns>依赖注入服务。</returns>
        IDependencyInjectionServices AddScoped(Type serviceType, Type implementationType);


        /// <summary>
        /// 配置服务（自动注入服务需要的所有依赖项）。
        /// </summary>
        /// <typeparam name="TService">服务类型。</typeparam>
        /// <returns>依赖注入服务。</returns>
        IDependencyInjectionServices AddSingleton<TService>() where TService : class;

        /// <summary>
        /// 配置服务（自动注入服务需要的所有依赖项）。
        /// </summary>
        /// <param name="serviceType">服务类型。</param>
        /// <returns>依赖注入服务。</returns>
        IDependencyInjectionServices AddSingleton(Type serviceType);

        /// <summary>
        /// 配置服务（自动注入服务需要的所有依赖项）。
        /// </summary>
        /// <typeparam name="TService">服务类型。</typeparam>
        /// <typeparam name="TImplementation">服务实现。</typeparam>
        /// <returns>依赖注入服务。</returns>
        IDependencyInjectionServices AddSingleton<TService, TImplementation>() where TService : class where TImplementation : TService;

        /// <summary>
        /// 配置服务（自动注入服务需要的所有依赖项）。
        /// </summary>
        /// <param name="serviceType">服务类型。</param>
        /// <param name="implementationType">服务实现。</param>
        /// <returns>依赖注入服务。</returns>
        IDependencyInjectionServices AddSingleton(Type serviceType, Type implementationType);
    }
}