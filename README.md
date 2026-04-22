![Inkslab](inkslab.jpg 'Logo')

![GitHub](https://img.shields.io/github/license/tinylit/inkslab.svg)
![language](https://img.shields.io/github/languages/top/tinylit/inkslab.svg)
![codeSize](https://img.shields.io/github/languages/code-size/tinylit/inkslab.svg)
![AppVeyor](https://img.shields.io/appveyor/build/tinylit/inkslab)
![AppVeyor tests](https://img.shields.io/appveyor/tests/tinylit/inkslab)
[![GitHub issues](https://img.shields.io/github/issues-raw/tinylit/inkslab)](../../issues)

<!-- AI-META
Project: Inkslab
Version: 1.2.23
TargetFrameworks: net461; netstandard2.1; net6.0
Authors: 影子和树
License: MIT
Language: C# (LangVersion 9.0)
Modules: Inkslab, Inkslab.Config, Inkslab.Json, Inkslab.Map, Inkslab.DI, Inkslab.Net
Keywords: lightweight framework, DI, mapping, JSON, config, HTTP, snowflake, extensions
-->

## Inkslab 是什么？

**Inkslab** 是一套简单、高效、模块化的 .NET 轻量基础设施框架。它由若干独立 NuGet 包组成，围绕 `SingletonPools` 单例池与 `XStartup` 启动机制协同工作：**面向接口、约定优先、按需引用、零侵入替换**。

### 核心特性

- **统一 API 设计**：所有模块遵循 `接口契约 + 默认实现 + 单例池注册` 的一致模式。
- **自动启动**：[`XStartup`](src/Inkslab/XStartup.cs) 扫描并按 `Code`/`Weight` 顺序执行所有 [`IStartup`](src/Inkslab/IStartup.cs)。
- **语法糖扩展**：字符串、集合、日期、枚举、类型、反射、加密等扩展方法位于 [src/Inkslab/Extentions](src/Inkslab/Extentions)。
- **多框架支持**：`net461` / `netstandard2.1` / `net6.0`。
- **零侵入替换**：任何默认实现都可以通过 `SingletonPools.TryAdd<TService, TImplementation>()` 在启动前替换。

---

## 仓库结构

```
inkslab/
├─ src/
│  ├─ Inkslab/          # 核心：单例池、启动、扩展方法、KeyGen、PagedList、IMapper/IJsonHelper/IConfigHelper 契约
│  ├─ Inkslab.Config/   # IConfigHelper 默认实现（配置文件读取）
│  ├─ Inkslab.Json/     # IJsonHelper 默认实现（基于 Newtonsoft.Json）
│  ├─ Inkslab.Map/      # IMapper 默认实现（约定优先对象映射）
│  ├─ Inkslab.DI/       # IServiceCollection 自动装配扩展
│  └─ Inkslab.Net/      # HTTP 请求工厂 IRequestFactory
└─ tests/               # 对应各模块的单元测试
```

---

## NuGet 包一览

| 包 | 版本 | 下载 | 文档 | 用途 |
| --- | --- | --- | --- | --- |
| `Inkslab` | [![Nuget](https://img.shields.io/nuget/v/Inkslab.svg)](https://www.nuget.org/packages/Inkslab/) | ![Nuget](https://img.shields.io/nuget/dt/Inkslab) | 本文 | 核心基础设施 |
| `Inkslab.Config` | [![Nuget](https://img.shields.io/nuget/v/Inkslab.Config.svg)](https://www.nuget.org/packages/Inkslab.Config/) | ![Nuget](https://img.shields.io/nuget/dt/Inkslab.Config) | [Inkslab.Config.md](./Inkslab.Config.md) | 配置文件读取 |
| `Inkslab.Json` | [![Nuget](https://img.shields.io/nuget/v/Inkslab.Json.svg)](https://www.nuget.org/packages/Inkslab.Json/) | ![Nuget](https://img.shields.io/nuget/dt/Inkslab.Json) | [Inkslab.Json.md](./Inkslab.Json.md) | JSON 序列化 |
| `Inkslab.Map` | [![Nuget](https://img.shields.io/nuget/v/Inkslab.Map.svg)](https://www.nuget.org/packages/Inkslab.Map/) | ![Nuget](https://img.shields.io/nuget/dt/Inkslab.Map) | [Inkslab.Map.md](./Inkslab.Map.md) | 对象映射 |
| `Inkslab.DI` | [![Nuget](https://img.shields.io/nuget/v/Inkslab.DI.svg)](https://www.nuget.org/packages/Inkslab.DI/) | ![Nuget](https://img.shields.io/nuget/dt/Inkslab.DI) | [Inkslab.DI.md](./Inkslab.DI.md) | 依赖注入扩展 |
| `Inkslab.Net` | [![Nuget](https://img.shields.io/nuget/v/Inkslab.Net.svg)](https://www.nuget.org/packages/Inkslab.Net/) | ![Nuget](https://img.shields.io/nuget/dt/Inkslab.Net) | [Inkslab.Net.md](./Inkslab.Net.md) | HTTP 请求 |

---

## 快速入门

### 1. 安装核心包

```bash
dotnet add package Inkslab
```

### 2. 启动框架

框架使用 `XStartup` 发现并执行所有 `IStartup`（含其它 Inkslab.* 包的默认注册）。

```csharp
using Inkslab;

using (var startup = new XStartup())
{
    startup.DoStartup();
}
```

`XStartup` 构造函数支持按程序集模式、程序集实例、类型集合进行范围限定：

```csharp
new XStartup();                          // 扫描基目录所有程序集
new XStartup("MyApp.*.dll");             // 按通配符
new XStartup(new[] { typeof(Program).Assembly });
```

### 3. 按需引入实现包

仅添加 `Inkslab` 不会带入 JSON/Map/Net/Config 实现。按需引用即可；引用后无需任何代码，`XStartup` 会自动注册默认实现到单例池。

```bash
dotnet add package Inkslab.Json      # 注册 IJsonHelper
dotnet add package Inkslab.Config    # 注册 IConfigHelper
dotnet add package Inkslab.Map       # 注册 IMapper
dotnet add package Inkslab.Net       # 通过 DI 注入 IRequestFactory
dotnet add package Inkslab.DI        # 为 IServiceCollection 提供自动装配扩展
```

---

## 核心 API 速览

### 单例池 [`SingletonPools`](src/Inkslab/SingletonPools.cs)

所有服务契约的统一注册与获取入口。

```csharp
// 注册（返回 false 代表已注册）
SingletonPools.TryAdd<IJsonHelper, CustomJsonHelper>();
SingletonPools.TryAdd<IMyService>(new MyServiceImpl());
SingletonPools.TryAdd<IMyService>(() => new MyServiceImpl());

// 获取
var json = SingletonPools.Singleton<IJsonHelper>();
```

| 方法 | 说明 |
| --- | --- |
| `TryAdd<TService>()` | 注册无参默认实现 |
| `TryAdd<TService>(TService instance)` | 注册已有实例 |
| `TryAdd<TService>(Func<TService> factory)` | 注册工厂 |
| `TryAdd<TService, TImplementation>()` | 注册契约-实现映射 |
| `Singleton<TService>()` | 获取单例，未注册时按约定创建 |

### 启动项契约 [`IStartup`](src/Inkslab/IStartup.cs)

```csharp
public interface IStartup
{
    int Code { get; }      // 启动阶段编号（排序依据）
    int Weight { get; }    // 同阶段内权重
    void Startup();        // 注册逻辑（通常向 SingletonPools 注册默认实现）
}
```

实现此接口 + 放入被扫描的程序集即可自动被 `XStartup` 执行。

### 主键生成 [`KeyGen`](src/Inkslab/KeyGen.cs)

默认基于雪花算法。

```csharp
long id  = KeyGen.Id();    // long 主键
Key key  = KeyGen.New();   // 值对象封装
Key key2 = KeyGen.New(id); // 从已有 long 还原

// 自定义机房/机器号（启动前注册）
SingletonPools.TryAdd(new KeyOptions(workerId: 1, datacenterId: 1));
```

### 分页与懒加载

- [`PagedList<T>`](src/Inkslab/PagedList.cs)：`PageIndex` / `PageSize` / `Total`。
- [`LazyList<T>`](src/Inkslab/LazyList.cs)：`Offset` / `HasNext`，适合游标式加载。

```csharp
var page = new PagedList<User>(users, pageIndex: 1, pageSize: 20, total: 135);
```

### 命名风格 [`NamingType`](src/Inkslab/NamingType.cs)

```csharp
public enum NamingType
{
    Normal = 0,       // 原样
    CamelCase = 1,    // userName
    SnakeCase = 2,    // user_name
    PascalCase = 3,   // UserName
    KebabCase = 4     // user-name
}

"UserName".ToNamingCase(NamingType.SnakeCase);  // => "user_name"
```

### 缓存集合 [src/Inkslab/Collections](src/Inkslab/Collections)

- `Lru<T>` / `Lru<TKey, TValue>`：线程安全 LRU 淘汰。
- `Lfu<T>` / `Lfu<TKey, TValue>`：线程安全 LFU 淘汰。
- 共用 `IEliminationAlgorithm<T>` 契约，便于替换。

### 异步锁 [`AsynchronousLock`](src/Inkslab/Threading/AsynchronousLock.cs)

悲观异步锁（基于 `SemaphoreSlim`）：

```csharp
using (await _lock.AcquireAsync())
{
    // 临界区
}
```

### 异常体系 [src/Inkslab/Exceptions](src/Inkslab/Exceptions)

| 异常 | 用途 |
| --- | --- |
| `CodeException` | 基础异常（带错误码） |
| `BusiException` | 业务异常 |
| `ServException` | 服务层异常 |
| `SyntaxException` | 语法/配置错误 |

### 程序集发现 [`AssemblyFinder`](src/Inkslab/AssemblyFinder.cs)

```csharp
var asms = AssemblyFinder.Find("MyApp.*.dll");
```

---

## 扩展方法索引

> 全部位于 `Inkslab` 命名空间（部分定义在 `System` 以实现全局可见）。

| 分类 | 文件 | 主要方法 |
| --- | --- | --- |
| 字符串 | [`StringExtensions`](src/Inkslab/Extentions/StringExtensions.cs) | `ToNamingCase` · `ToPascalCase` · `ToSnakeCase` · `ToCamelCase` · `ToKebabCase` · `IsNull` · `IsEmpty` · `IsMail` · `Format` · `Config<T>` · `StringSugar` |
| 集合 | [`IEnumerableExtentions`](src/Inkslab/Extentions/IEnumerableExtentions.cs) | `Join` · `ForEach` · `Distinct` · `AlignOverall` · `Align` · `ZipEach` · `AlignEach` · `JoinEach` |
| 日期时间 | [`DateTimeExtensions`](src/Inkslab/Extentions/DateTimeExtensions.cs) | `StartOfDay/Week/Month/Quarter/Year` · `EndOfDay/...` · `IsToday/Yesterday/Tomorrow` · `IsWeekday/Weekend` · `IsSameDay/Week/Month/Year` · `WeekOfYear` · `IsLeapYear` · `NextWeekday` · `PreviousWeekday` · `WorkingDays` · `AddWorkingDays` · `ToUnixTimestamp[Milliseconds]` · `FromUnixTimestamp[Milliseconds]` · `GetAge` · `RoundTo/FloorTo/CeilingTo` |
| 枚举 | [`EnumExtensions`](src/Inkslab/Extentions/EnumExtensions.cs) | `GetText` · `ToInt32` · `ToInt64` · `ToValueString` · `ToValues` |
| 类型 | [`TypeExtensions`](src/Inkslab/Extentions/TypeExtensions.cs) | `IsMini` · `IsSimple` · `IsNullable` · `IsKeyValuePair` · `IsAmongOf` · `IsLike` |
| 反射 | [`ReflectionExtensions`](src/Inkslab/Extentions/ReflectionExtensions.cs) | `IsIgnore` · `GetDescription` |
| 加密 | [`CryptoExtensions`](src/Inkslab/Extentions/CryptoExtensions.cs) | `Md5` · `Encrypt` · `Decrypt`（`CryptoKind.DES` / `AES` / ...） |

> **日期周边界约定**：`UTC` 以周日为一周起始，`Local` 以周一为起始。

---

## 使用示例

### 字符串命名与配置

```csharp
"UserName".ToSnakeCase();                // "user_name"
"user_name".ToPascalCase();              // "UserName"

var conn = "ConnectionStrings:Default".Config<string>();
var cfg  = "AppSettings".Config<AppConfig>();
```

### 集合对齐与遍历

```csharp
var ordered = array2.AlignOverall(array1).ToList();  // array2 按 array1 重排
array1.JoinEach(array2, x => x, y => y.Id, (x, y) => { /* 内连接回调 */ });
```

### 加密

```csharp
"password".Md5();
var cipher = "data".Encrypt("Test@*$!", CryptoKind.DES);
var plain  = cipher.Decrypt("Test@*$!", CryptoKind.DES);
```

### JSON / 配置 / 映射（配合实现包）

```csharp
string json = JsonHelper.ToJson(obj, NamingType.CamelCase, indented: true);
var    obj2 = JsonHelper.Json<MyDto>(json, NamingType.CamelCase);

var    dbConn = "ConnectionStrings:Default".Config<string>();
var    dto    = Mapper.Map<UserDto>(user);
```

---

## 替换默认实现

任何 `IXxxHelper` 都可在启动前通过 `SingletonPools.TryAdd` 替换：

```csharp
public class MyJsonHelper : IJsonHelper { /* ... */ }

// 必须在 XStartup.DoStartup() 之前调用
SingletonPools.TryAdd<IJsonHelper, MyJsonHelper>();

using var startup = new XStartup();
startup.DoStartup();
```

---

## 单元测试

| 项目 | 覆盖范围 |
| --- | --- |
| [`tests/Inkslab.Tests`](tests/Inkslab.Tests) | 核心扩展、集合、加密、日期、异步锁等 |
| [`tests/Inkslab.Config.Tests`](tests/Inkslab.Config.Tests) | 配置读取 |
| [`tests/Inkslab.Json.Tests`](tests/Inkslab.Json.Tests) | JSON 序列化 |
| [`tests/Inkslab.Map.Tests`](tests/Inkslab.Map.Tests) | 对象映射 |
| [`tests/Inkslab.DI.Tests`](tests/Inkslab.DI.Tests) | 自动装配（ASP.NET Core 宿主） |
| [`tests/Inkslab.Net.Tests`](tests/Inkslab.Net.Tests) | HTTP 请求 |

运行：

```bash
dotnet test
```

---

## 构建与发布

- **版本管理**：[`Directory.Build.props`](Directory.Build.props) 统一 `Version=1.2.23`、`LangVersion=9.0`、`TreatWarningsAsErrors=true`、`GenerateDocumentationFile=true`。
- **打包**：[`build.ps1`](build.ps1) 对 6 个包逐个 `dotnet pack --configuration Release` 到 `.nupkgs/`。
- **CI**：[`appveyor.yml`](appveyor.yml)，`main` 分支自动发布到 NuGet。

---

## 贡献

1. Fork 并创建特性分支：`git checkout -b feature/xxx`
2. 补充或维护对应模块的单元测试。
3. 确保 `dotnet test` 全部通过。
4. 提交 PR。

---

## 许可证

[MIT](LICENSE)

---

[![Stargazers over time](https://starchart.cc/tinylit/inkslab.svg)](https://starchart.cc/tinylit/inkslab)