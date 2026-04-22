![Inkslab](inkslab.jpg 'Logo')

<!-- AI-META
Package: Inkslab.DI
Version: 1.2.23
TargetFrameworks: net461; netstandard2.1; net6.0
Namespace: Inkslab.DI, Inkslab.DI.Annotations, Inkslab.DI.Options, Microsoft.Extensions.DependencyInjection
Dependencies: Inkslab; Microsoft.Extensions.DependencyInjection.Abstractions
EntryType: IServiceCollection.DependencyInjection(DependencyInjectionOptions)
CoreInterface: IDependencyInjectionServices (src/Inkslab.DI/IDependencyInjectionServices.cs)
Hooks: IConfigureServices (src/Inkslab.DI/IConfigureServices.cs)
Attributes: SingletonAttribute, ScopedAttribute, TransientAttribute, DependencySeekAttribute
Keywords: dependency injection, auto-registration, convention, ServiceLifetime, assembly scanning, ASP.NET Core
-->

## Inkslab.DI 是什么？

**Inkslab.DI** 是 `Microsoft.Extensions.DependencyInjection.IServiceCollection` 的自动装配扩展：

- **按特性注册**：类上标注 `[Singleton]` / `[Scoped]` / `[Transient]` 自动注册。
- **按约定注册**：`ConfigureByAuto()` 根据控制器/动作参数反向拉起依赖。
- **按检查注册**：`ConfigureByExamine(type => ...)` 自定义筛选。
- **按 `IConfigureServices` 注册**：约定式手动注册点。
- **跨平台**：同一套 API 适用于 .NET Framework / .NET Standard / .NET 6+。

---

## 安装

```bash
dotnet add package Inkslab.DI
```

---

## 快速入门

### 最小可运行样例

```csharp
using Inkslab.DI;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();

services.DependencyInjection(new DependencyInjectionOptions())
        .SeekAssemblies("MyApp.*.dll")   // 1) 发现程序集
        .ConfigureByDefined()            // 2) 执行 IConfigureServices
        .ConfigureByAuto();              // 3) 自动按需注入

var provider = services.BuildServiceProvider();
var svc = provider.GetRequiredService<IMyService>();
```

### 使用特性声明生命周期

```csharp
public interface IMyService { }

[Singleton]                       // 单例
public class MyService : IMyService { }

[Scoped(Many = true)]             // 作用域 + 同时暴露多个契约
public class UserService : IUserReader, IUserWriter { }

[Transient]                       // 瞬时
public class Formatter { }
```

装配阶段自动按特性写入 `IServiceCollection`，无需手动 `AddSingleton` 等调用。

---

## 核心契约

### `IDependencyInjectionServices` [src/Inkslab.DI/IDependencyInjectionServices.cs](src/Inkslab.DI/IDependencyInjectionServices.cs)

构建器接口，支持流畅链式调用。

```csharp
public interface IDependencyInjectionServices : IDisposable
{
    IReadOnlyCollection<Assembly> Assemblies { get; }

    // 程序集范围
    IDependencyInjectionServices AddAssembly(Assembly assembly);
    IDependencyInjectionServices SeekAssemblies(string pattern = "*");
    IDependencyInjectionServices SeekAssemblies(params string[] patterns);

    // 忽略指定类型
    IDependencyInjectionServices IgnoreType(Type serviceType);
    IDependencyInjectionServices IgnoreType<TService>();

    // 三种装配模式
    IDependencyInjectionServices ConfigureByDefined();                                  // 运行所有 IConfigureServices
    IDependencyInjectionServices ConfigureServices(DependencyInjectionServicesOptions o); // 按 MVC/动作参数反向注入
    IDependencyInjectionServices ConfigureByAuto();                                     // 基于 ConfigureServices(Instance)
    IDependencyInjectionServices ConfigureByExamine(Predicate<Type> match);             // 自定义筛选

    // 显式注册（默认生命周期取自 DependencyInjectionOptions.Lifetime）
    IDependencyInjectionServices Add<TService>() where TService : class;
    IDependencyInjectionServices Add<TService, TImplementation>() where TService : class where TImplementation : TService;
    IDependencyInjectionServices Add(Type serviceType);
    IDependencyInjectionServices Add(Type serviceType, Type implementationType);

    // 指定生命周期
    IDependencyInjectionServices AddTransient<TService>() where TService : class;
    IDependencyInjectionServices AddScoped<TService>()    where TService : class;
    IDependencyInjectionServices AddSingleton<TService>() where TService : class;
    // ...以及对应的 (Type)、(Type,Type)、(TService,TImplementation) 重载
}
```

### `IConfigureServices` [src/Inkslab.DI/IConfigureServices.cs](src/Inkslab.DI/IConfigureServices.cs)

约定式服务注册钩子：实现该接口的类在 `ConfigureByDefined()` 阶段自动执行。

```csharp
public class MyModule : IConfigureServices
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<IFoo, Foo>();
        services.AddOptions<MyOptions>().BindConfiguration("MyOptions");
    }
}
```

### `DependencyInjectionOptions` [src/Inkslab.DI/Options/DependencyInjectionOptions.cs](src/Inkslab.DI/Options/DependencyInjectionOptions.cs)

```csharp
public class DependencyInjectionOptions
{
    public int             MaxDepth { get; set; } = 8;                   // 自动注入最大递归深度
    public ServiceLifetime Lifetime { get; set; } = ServiceLifetime.Scoped; // 未标注特性时的默认生命周期

    public virtual bool Ignore(Type serviceType);                        // 默认：非 public 或嵌套类型忽略
    public virtual Type ResolveConflictingTypes(Type serviceType,
                                                List<Type> implementations); // 多实现冲突时的仲裁
}
```

### `DependencyInjectionServicesOptions` [src/Inkslab.DI/Options/DependencyInjectionServicesOptions.cs](src/Inkslab.DI/Options/DependencyInjectionServicesOptions.cs)

控制"从控制器动作参数反向拉起依赖"的行为。

```csharp
public class DependencyInjectionServicesOptions
{
    // net461 默认 false；其他目标默认 true
    public bool DiServicesActionIsFromServicesParameters { get; set; }

    public virtual bool IsServicesType(Type type);
    public virtual bool ActionParameterIsFromServices(ParameterInfo parameterInfo);

    public static DependencyInjectionServicesOptions Instance { get; }
}
```

---

## 生命周期特性

所有特性均继承自 [`ServiceLifetimeAttribute`](src/Inkslab.DI/Annotations/ServiceLifetimeAttribute.cs)，而该特性本身继承自 `Inkslab.Annotations.ExportAttribute`：`Many` 属性控制是否"同一实现暴露给其所有实现的接口"。

| 特性 | 生命周期 | 源文件 |
| --- | --- | --- |
| `[Singleton]` | `ServiceLifetime.Singleton` | [SingletonAttribute.cs](src/Inkslab.DI/Annotations/SingletonAttribute.cs) |
| `[Scoped]` | `ServiceLifetime.Scoped` | [ScopedAttribute.cs](src/Inkslab.DI/Annotations/ScopedAttribute.cs) |
| `[Transient]` | `ServiceLifetime.Transient` | [TransientAttribute.cs](src/Inkslab.DI/Annotations/TransientAttribute.cs) |

```csharp
[Singleton(Many = true)]              // 暴露该类实现的所有接口
public class Cache : ICache, IDisposable { }

[Scoped]
public class OrderService : IOrderService { }
```

### 依赖查找特性

[`DependencySeekAttribute`](src/Inkslab.DI/Annotations/DependencySeekAttribute.cs)：声明一个实现所依赖的类型族，供自动装配搜索。

```csharp
public class MyAttribute : DependencySeekAttribute
{
    public override IEnumerable<Type> Dependencies(Type implementationType) => /* ... */;
}
```

---

## 进阶用法

### 1. 忽略类型

```csharp
services.DependencyInjection(new DependencyInjectionOptions())
        .IgnoreType<IDontInjectMe>();
```

### 2. 条件注册

```csharp
services.DependencyInjection(new DependencyInjectionOptions())
        .ConfigureByExamine(t => t.Name.EndsWith("Service") && !t.IsAbstract);
```

### 3. 在 ASP.NET Core 中反向装配控制器依赖

```csharp
services.DependencyInjection(new DependencyInjectionOptions())
        .SeekAssemblies()
        .ConfigureServices(DependencyInjectionServicesOptions.Instance);
```

参见 [tests/Inkslab.DI.Tests/Startup.cs](tests/Inkslab.DI.Tests/Startup.cs)。

---

## 典型装配顺序

```csharp
services.DependencyInjection(new DependencyInjectionOptions
        {
            Lifetime = ServiceLifetime.Scoped,
            MaxDepth = 8
        })
        .SeekAssemblies("MyApp.*.dll")
        .ConfigureByDefined()       // 执行所有 IConfigureServices
        .ConfigureByAuto();         // 自动反向装配
```

---

## 单元测试

- [tests/Inkslab.DI.Tests/](tests/Inkslab.DI.Tests/) 提供了完整的 ASP.NET Core 宿主样例，覆盖自动扫描、特性标记、`IConfigureServices` 钩子及控制器动作参数注入。

---

## 常见问题

- **类未被注册**：确认其程序集已通过 `AddAssembly` / `SeekAssemblies` 纳入，且类型为 `public`（非 `nested`）。
- **多实现冲突**：重写 `DependencyInjectionOptions.ResolveConflictingTypes` 自行仲裁，或使用 `IgnoreType` 排除。
- **net461 下 Controller 参数未注入**：将 `DependencyInjectionServicesOptions.DiServicesActionIsFromServicesParameters` 显式置为 `true`。

---

## 说明

Inkslab.DI 与 `Microsoft.Extensions.DependencyInjection` 完全兼容，仅在其上添加装配约定，所有生成的 `ServiceDescriptor` 均为标准实现。
