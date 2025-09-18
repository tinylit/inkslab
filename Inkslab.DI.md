![Inkslab](inkslab.jpg 'Logo')

## "Inkslab.DI"是什么？

**Inkslab.DI** 是一套自动化、约定优先的依赖注入扩展，兼容 .NET Framework、.NET Standard 和 .NET Core。它支持自动发现、注册、配置服务，简化复杂项目的依赖管理，提升开发效率和可维护性。

---

## 🚀 快速入门

### 1. 安装包

```bash
PM> Install-Package Inkslab.DI
```

### 2. 基础用法

```csharp
using Inkslab.DI;
using Microsoft.Extensions.DependencyInjection;

// 创建服务集合
var services = new ServiceCollection();

// 配置依赖注入（自动扫描并注册）
services.DependencyInjection(new DependencyInjectionOptions());

// 构建服务提供者
var provider = services.BuildServiceProvider();

// 获取服务
var myService = provider.GetService<IMyService>();
```

### 3. 自动注册程序集

```csharp
// 自动查找并注册所有相关程序集
services.DependencyInjection(new DependencyInjectionOptions())
    .SeekAssemblies("*.dll");
```

---

## 🏗️ 接口契约

### 1. 依赖注入主接口

```csharp
/// <summary>
/// 依赖注入服务接口，支持自动发现、注册、配置服务。
/// </summary>
public interface IDependencyInjectionServices : IDisposable
{
    /// <summary>已注册的程序集集合。</summary>
    IReadOnlyCollection<Assembly> Assemblies { get; }
    /// <summary>添加程序集。</summary>
    IDependencyInjectionServices AddAssembly(Assembly assembly);
    /// <summary>按模式查找程序集。</summary>
    IDependencyInjectionServices SeekAssemblies(string pattern = "*");
    IDependencyInjectionServices SeekAssemblies(params string[] patterns);

    /// <summary>忽略指定类型的自动注入。</summary>
    IDependencyInjectionServices IgnoreType(Type serviceType);
    IDependencyInjectionServices IgnoreType<TService>();

    /// <summary>按定义配置服务。</summary>
    IDependencyInjectionServices ConfigureByDefined();
    /// <summary>按选项配置服务。</summary>
    IDependencyInjectionServices ConfigureServices(DependencyInjectionServicesOptions servicesOptions);
    /// <summary>自动配置服务。</summary>
    IDependencyInjectionServices ConfigureByAuto();
    /// <summary>按条件配置服务。</summary>
    IDependencyInjectionServices ConfigureByExamine(Predicate<Type> match);

    /// <summary>注册服务。</summary>
    IDependencyInjectionServices Add<TService>() where TService : class;
    IDependencyInjectionServices Add(Type serviceType);
    IDependencyInjectionServices Add<TService, TImplementation>() where TService : class where TImplementation : TService;
    IDependencyInjectionServices Add(Type serviceType, Type implementationType);
    IDependencyInjectionServices Add(Type serviceType, ServiceLifetime lifetime, Type implementationType);

    /// <summary>注册瞬态服务。</summary>
    IDependencyInjectionServices AddTransient<TService>() where TService : class;
    IDependencyInjectionServices AddTransient(Type serviceType);
    IDependencyInjectionServices AddTransient<TService, TImplementation>() where TService : class where TImplementation : TService;
    IDependencyInjectionServices AddTransient(Type serviceType, Type implementationType);

    /// <summary>注册单例服务。</summary>
    IDependencyInjectionServices AddSingleton<TService>() where TService : class;
    IDependencyInjectionServices AddSingleton(Type serviceType);
    IDependencyInjectionServices AddSingleton<TService, TImplementation>() where TService : class where TImplementation : TService;
    IDependencyInjectionServices AddSingleton(Type serviceType, Type implementationType);
}
```

### 2. 服务配置扩展

```csharp
/// <summary>
/// 服务配置扩展接口，支持自定义服务注册。
/// </summary>
public interface IConfigureServices
{
    void ConfigureServices(IServiceCollection services);
}
```

### 3. 配置选项

```csharp
/// <summary>
/// 依赖注入选项。
/// </summary>
public class DependencyInjectionOptions
{
    /// <summary>最大递归深度。</summary>
    public int MaxDepth { get; set; } = 8;
    /// <summary>默认生命周期。</summary>
    public ServiceLifetime Lifetime { get; set; } = ServiceLifetime.Scoped;
    /// <summary>忽略类型判断。</summary>
    public virtual bool Ignore(Type serviceType) => serviceType.IsNotPublic || serviceType.IsNested;
}
```

```csharp
/// <summary>
/// 依赖注入服务配置选项。
/// </summary>
public class DependencyInjectionServicesOptions
{
    /// <summary>是否从服务参数获取。</summary>
    public bool DiServicesActionIsFromServicesParameters { get; set; } = true;
    /// <summary>判断是否为服务类型。</summary>
    public virtual bool IsServicesType(Type type);
    /// <summary>判断参数是否来自服务。</summary>
    public virtual bool ActionParameterIsFromServices(ParameterInfo parameterInfo);
    /// <summary>单例实例。</summary>
    public static DependencyInjectionServicesOptions Instance { get; }
}
```

---

## 🏗️ 进阶用法

### 1. 忽略类型自动注入

```csharp
services.DependencyInjection(new DependencyInjectionOptions())
    .IgnoreType<IMyService>();
```

### 2. 按条件自动注册

```csharp
services.DependencyInjection(new DependencyInjectionOptions())
    .ConfigureByExamine(type => type.Name.EndsWith("Service"));
```

### 3. 自定义服务配置

实现 [`IConfigureServices`](src/Inkslab.DI/IConfigureServices.cs) 并自动调用：

```csharp
public class CustomConfigure : IConfigureServices
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<IMyService, MyServiceImpl>();
    }
}
```

---

## 🧑‍💻 单元测试参考

请参考 [tests/Inkslab.DI.Tests/](tests/Inkslab.DI.Tests/) 目录下的测试用例，涵盖自动注入、生命周期管理、服务查找等场景。

---

## 💡 常见问题与建议

- 支持自动发现和注册，实现约定优先，减少手动配置。
- 支持多种生命周期（Transient/Scoped/Singleton）。
- 可通过扩展接口和配置选项灵活定制注入行为。
- 推荐在大型项目中结合自动程序集扫描和自定义配置，提升开发效率。

---

## 📖 说明

Inkslab.DI 兼容 Microsoft.Extensions.DependencyInjection，支持主流 .NET 平台，适合微服务、Web、桌面等多种应用场景。详细源码请参考 [src/Inkslab.DI/](src/Inkslab.DI/) 目录。
