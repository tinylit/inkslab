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
![Inkslab](inkslab.jpg 'Logo')

![GitHub](https://img.shields.io/github/license/tinylit/inkslab.svg)
![language](https://img.shields.io/github/languages/top/tinylit/inkslab.svg)
![codeSize](https://img.shields.io/github/languages/code-size/tinylit/inkslab.svg)
![AppVeyor](https://img.shields.io/appveyor/build/tinylit/inkslab)
![AppVeyor tests](https://img.shields.io/appveyor/tests/tinylit/inkslab)
[![GitHub issues](https://img.shields.io/github/issues-raw/tinylit/inkslab)](../../issues)

## "Inkslab"是什么？

Inkslab 是一套简单、高效的轻量级框架，专注于现代化 C# 开发体验。框架采用模块化设计，提供统一的 API 接口，涵盖了对象映射、配置读取、序列化、依赖注入等核心功能。

### 🎯 核心特性

- **统一API设计** - 所有模块遵循一致的设计原则和API风格
- **语法糖扩展** - 基于扩展方法的语法糖，提升开发效率
- **模块化架构** - 按需引用，最小化依赖
- **自动启动机制** - [`XStartup`](src/Inkslab/XStartup.cs) 自动发现和注册组件
- **多框架支持** - 支持 .NET Framework 4.6.1+、.NET Standard 2.1、.NET 6.0+

## 🚀 快速入门

### 安装

```bash
PM> Install-Package Inkslab
```

### 基础配置

```csharp
using Inkslab;

// 框架自动启动（推荐）
using (var startup = new XStartup())
{
    startup.DoStartup();
}
```

## 📦 NuGet 包

| Package | NuGet | Downloads | 描述 |
| ------- | ----- | --------- | ---- |
| Inkslab | [![Inkslab](https://img.shields.io/nuget/v/inkslab.svg)](https://www.nuget.org/packages/inkslab/) | ![Nuget](https://img.shields.io/nuget/dt/Inkslab) | 核心框架 |
| Inkslab.Config | [![Inkslab.Config](https://img.shields.io/nuget/v/inkslab.config.svg)](https://www.nuget.org/packages/inkslab.config/) | ![Nuget](https://img.shields.io/nuget/dt/Inkslab.Config) | [配置文件读取](./Inkslab.Config.md) |
| Inkslab.Json | [![Inkslab.Json](https://img.shields.io/nuget/v/inkslab.json.svg)](https://www.nuget.org/packages/inkslab.json/) | ![Nuget](https://img.shields.io/nuget/dt/Inkslab.Json) | [JSON 序列化](./Inkslab.Json.md) |
| Inkslab.Map | [![Inkslab.Map](https://img.shields.io/nuget/v/inkslab.map.svg)](https://www.nuget.org/packages/inkslab.map/) | ![Nuget](https://img.shields.io/nuget/dt/Inkslab.Map) | [对象映射](./Inkslab.Map.md) |
| Inkslab.DI | [![Inkslab.DI](https://img.shields.io/nuget/v/inkslab.di.svg)](https://www.nuget.org/packages/inkslab.di/) | ![Nuget](https://img.shields.io/nuget/dt/Inkslab.DI) | 依赖注入扩展 |
| Inkslab.Net | [![Inkslab.Net](https://img.shields.io/nuget/v/inkslab.net.svg)](https://www.nuget.org/packages/inkslab.net/) | ![Nuget](https://img.shields.io/nuget/dt/Inkslab.Net) | [HTTP 请求组件](./Inkslab.Net.md) |

## 🏗️ 核心技术架构

### 1. 扩展方法体系

Inkslab 基于 C# 扩展方法构建了一套完整的语法糖体系，位于 [`src/Inkslab/Extentions/`](src/Inkslab/Extentions/) 目录：

#### 字符串扩展 ([`StringExtensions`](src/Inkslab/Extentions/StringExtensions.cs))

```csharp
// 命名规范转换
string camelCase = "UserName".ToCamelCase();        // → "userName"
string snakeCase = "UserName".ToSnakeCase();        // → "user_name"  
string pascalCase = "user_name".ToPascalCase();     // → "UserName"
string kebabCase = "UserName".ToKebabCase();        // → "user-name"

// 统一命名转换API
string result = "UserName".ToNamingCase(NamingType.SnakeCase); // → "user_name"

// 配置读取语法糖
string dbConnection = "ConnectionStrings:Default".Config<string>();
var appSettings = "AppSettings".Config<AppConfig>();
```

#### 集合扩展 ([`IEnumerableExtentions`](src/Inkslab/Extentions/IEnumerableExtentions.cs))

```csharp
/// <summary>
/// 内容对齐。
/// </summary>
public void AlignTest()
{
    var array1 = new List<int> { 1, 2, 3, 4, 5, 6, 7 };
    var array2 = new List<int> { 4, 5, 1, 2, 3, 6, 7 };

    //? 将 array2 按照 array1 的集合排序。
    var array3 = array2
        .AlignOverall(array1)
        .ToList();

    //? 比较两个集合相同下标位，值是否相同。
    array3.ZipEach(array1, Assert.Equal);
}

/// <summary>
/// 内容遍历。
/// </summary>
public void JoinEachTest()
{
    var array1 = new List<int> { 1, 2, 3, 4, 5, 6, 7 };
    var array2 = new List<DistinctA>();

    var r = new Random();

    for (int i = 0, len = 50; i < len; i++)
    {
        array2.Add(new DistinctA
        {
            Id = r.Next(len),
            Name = i.ToString(),
            Date = DateTime.Now
        });
    }
    //? 与 Join 逻辑相同，但不需要返回新的集合。
    array1.JoinEach(array2, x => x, y => y.Id, (x, y) =>
    {
        Assert.Equal(x, y.Id);
    });
}
```

#### 加密扩展 ([`CryptoExtensions`](src/Inkslab/Extentions/CryptoExtensions.cs))

```csharp
// 常用哈希算法
string md5 = "password".Md5();
string encrypt = "data".Encrypt("Test@*$!", CryptoKind.DES); // 加密
string decrypt = "data".Decrypt("Test@*$!", CryptoKind.DES); // 解密
```

#### 日期时间扩展 ([`DateTimeExtensions`](src/Inkslab/Extentions/DateTimeExtensions.cs))
##### 自动根据提供时间是**Utc** / **Local** 自动处理一周的第一天和最后一天。
* **Utc**：周日为一周的第一天；周六为一周的最后一天。
* **Local**：周一为一周的第一天；周日为一周的最后一天。

### 2. 序列化框架

#### JSON 序列化 (基于 Newtonsoft.Json)

核心实现：[`DefaultJsonHelper`](src/Inkslab.Json/DefaultJsonHelper.cs)

```csharp
// 基础用法
string json = JsonHelper.ToJson(obj);
T result = JsonHelper.Json<T>(json);

// 命名规范支持
string json = JsonHelper.ToJson(obj, NamingType.CamelCase);
var result = JsonHelper.Json<User>(json, NamingType.SnakeCase);

// 格式化输出
string prettyJson = JsonHelper.ToJson(obj, indented: true);
```

#### 自定义JSON序列化器

```csharp
public class CustomJsonHelper : IJsonHelper
{
    public string ToJson<T>(T jsonObj, NamingType namingType = NamingType.Normal, bool indented = false)
    {
        // 自定义序列化逻辑
        var settings = new JsonSerializerSettings();
        
        // 根据命名规范配置
        switch (namingType)
        {
            case NamingType.CamelCase:
                settings.ContractResolver = new CamelCasePropertyNamesContractResolver();
                break;
            case NamingType.SnakeCase:
                settings.ContractResolver = new DefaultContractResolver 
                { 
                    NamingStrategy = new SnakeCaseNamingStrategy() 
                };
                break;
        }
        
        return JsonConvert.SerializeObject(jsonObj, indented ? Formatting.Indented : Formatting.None, settings);
    }
    
    public T Json<T>(string json, NamingType namingType = NamingType.Normal)
    {
        // 自定义反序列化逻辑
    }
}

// 注册自定义实现
SingletonPools.TryAdd<IJsonHelper, CustomJsonHelper>();
```

### 3. 语法糖适配器 ([`AdapterSugar`](src/Inkslab/Sugars/AdapterSugar.cs))

Inkslab 框架通过 **AdapterSugar<T>** 实现了自动化的语法糖适配机制，极大提升了扩展性和运行时性能。其核心流程如下：

#### **1. 类型与成员信息初始化**

- 自动获取泛型参数 **T** 及正则相关类型（如 **Match**、**Group**、**Capture** 等）的反射信息。
- 构建表达式树所需的参数表达式。

#### **2. 方法发现与遍历**

- 自动发现 **T** 类型下所有公开实例方法。
- 仅处理参数数量大于0且返回类型为 **string** 的方法。

#### **3. 参数分析与表达式构建**

- 支持参数类型：**Match**、**GroupCollection**、**Group**、**CaptureCollection**、**Capture**、**string**、**bool**。
- 根据参数类型，自动生成变量声明、赋值、条件判断及参数列表。
- 不支持类型将抛出异常，确保类型安全。

#### **4. 条件与特性处理**

- 支持 **MatchAttribute** 指定正则分组名。
- 支持 **MismatchAttribute** 补充不匹配条件。
- 汇总所有条件表达式，生成最终的匹配条件。

#### **5. 表达式树编译**

- 条件表达式编译为 **Func<Match, bool>**，用于判断当前正则匹配是否适用该方法。
- 方法调用表达式编译为 **Func<T, Match, string>**，用于执行实际转换逻辑。

#### **6. 适配器缓存**

- 每个方法生成一个适配器（包含条件判断与转换逻辑），自动缓存到静态列表，供后续格式化调用时高效匹配和执行。

```csharp
public abstract class AdapterSugar<T> : ISugar where T : AdapterSugar<T>, ISugar
{
    // 适配器模式实现
    private class Adapter
    {
        public Func<Match, bool> CanConvert { get; set; }
        public Func<T, Match, string> Convert { get; set; }
    }
    
    // 撤销操作
    public bool Undo { get; private set; }
    
    // 格式化方法
    public string Format(Match match);
}
```

#### 自定义语法糖示例
```csharp
  public class StringSugar : AdapterSugar<StringSugar>
  {
      private readonly object _source;
      private readonly DefaultSettings _settings;
      private readonly SyntaxPool _syntaxPool;

      public StringSugar(object source, DefaultSettings settings, SyntaxPool syntaxPool)
      {
          _source = source ?? throw new ArgumentNullException(nameof(source));
          _settings = settings ?? throw new ArgumentNullException(nameof(settings));
          _syntaxPool = syntaxPool ?? throw new ArgumentNullException(nameof(syntaxPool));
      }

      [Mismatch("token")] //? 不匹配 token。
      public string Single(string name, string format) => _syntaxPool.GetValue(this, _source, _settings, name, format);

      [Mismatch("token")] //? 不匹配 token。
      public string Single(string name) => _syntaxPool.GetValue(this, _source, _settings, name);

      public string Combination(string pre, string token, string name, string format) => _syntaxPool.GetValue(this, _source, _settings, pre, token, name, format);

      public string Combination(string pre, string token, string name) => _syntaxPool.GetValue(this, _source, _settings, pre, token, name);
  }
```

### 4. 单例池管理 ([`SingletonPools`](src/Inkslab/))

```csharp
// 注册单例
SingletonPools.TryAdd<IService, ServiceImpl>();
SingletonPools.TryAdd(new ServiceInstance());

// 获取单例
var service = SingletonPools.Singleton<IService>();
```

## 💡 核心功能详解

### 配置管理

```csharp
// 强类型配置
public class DatabaseConfig
{
    public string ConnectionString { get; set; }
    public int Timeout { get; set; }
}

var dbConfig = "Database".Config<DatabaseConfig>();

// 配置监听（热更新）
var options = "Database".Options<DatabaseConfig>();
// options.Value 会随配置文件变化自动更新
```

### 对象映射

```csharp
// 基础映射
var dto = Mapper.Map<UserDto>(user);
```

### 主键生成

```csharp
// 雪花算法ID生成
long id = KeyGen.Id();
Key newKey = KeyGen.New();

// 自定义机房和机器号
SingletonPools.TryAdd(new KeyOptions(workerId: 1, datacenterId: 1));
```

### 命名规范转换

基于 [`NamingType`](src/Inkslab/NamingType.cs) 枚举的统一命名处理：

```csharp
public enum NamingType
{
    Normal = 0,      // 原样输出
    CamelCase = 1,   // 驼峰：userName
    SnakeCase = 2,   // 蛇形：user_name  
    PascalCase = 3,  // 帕斯卡：UserName
    KebabCase = 4    // 短横线：user-name
}

// 使用示例
string result = "UserName".ToNamingCase(NamingType.SnakeCase);
```

## 🎓 高级应用指南

### 1. 自定义启动器

实现 [`IStartup`](src/Inkslab/IStartup.cs) 接口创建自定义启动器：

```csharp
public class CustomStartup : IStartup
{
    public int Code => 999;      // 启动代码（用于排序）
    public int Weight => 1;      // 权重

    public void Startup()
    {
        // 自定义初始化逻辑
        SingletonPools.TryAdd<ICustomService, CustomServiceImpl>();
        
        // 注册语法糖
        RegisterCustomSyntaxSugar();
        
        // 配置全局设置
        ConfigureGlobalSettings();
    }
}
```

### 2. 扩展方法最佳实践

```csharp
#pragma warning disable IDE0130 // 命名空间与文件夹结构不匹配
namespace System // 扩展到系统命名空间，全局可用
#pragma warning restore IDE0130

{
    public static class CustomExtensions
    {
        /// <summary>
        /// 安全的字符串截取
        /// </summary>
        public static string SafeSubstring(this string source, int startIndex, int length)
        {
            if (string.IsNullOrEmpty(source) || startIndex >= source.Length)
                return string.Empty;
                
            return source.Substring(startIndex, Math.Min(length, source.Length - startIndex));
        }
        
        /// <summary>
        /// 条件执行扩展
        /// </summary>
        public static T If<T>(this T source, bool condition, Func<T, T> action)
        {
            return condition ? action(source) : source;
        }
    }
}
```

### 3. 高性能序列化配置

```csharp
public class HighPerformanceJsonHelper : IJsonHelper
{
    private static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
    {
        // 性能优化配置
        DateTimeZoneHandling = DateTimeZoneHandling.Utc,
        NullValueHandling = NullValueHandling.Ignore,
        DefaultValueHandling = DefaultValueHandling.Ignore,
        
        // 类型处理
        TypeNameHandling = TypeNameHandling.None,
        
        // 错误处理
        Error = (sender, args) => 
        {
            // 记录序列化错误但不中断处理
            args.ErrorContext.Handled = true;
        }
    };
    
    public string ToJson<T>(T jsonObj, NamingType namingType = NamingType.Normal, bool indented = false)
    {
        var settings = Settings.Clone();
        settings.ContractResolver = GetContractResolver(namingType);
        settings.Formatting = indented ? Formatting.Indented : Formatting.None;
        
        return JsonConvert.SerializeObject(jsonObj, settings);
    }
    
    private static IContractResolver GetContractResolver(NamingType namingType)
    {
        // 缓存 ContractResolver 实例以提升性能
        return namingType switch
        {
            NamingType.CamelCase => CachedResolvers.CamelCase,
            NamingType.SnakeCase => CachedResolvers.SnakeCase,
            NamingType.PascalCase => CachedResolvers.PascalCase,
            NamingType.KebabCase => CachedResolvers.KebabCase,
            _ => CachedResolvers.Default
        };
    }
    
    private static class CachedResolvers
    {
        public static readonly IContractResolver Default = new DefaultContractResolver();
        public static readonly IContractResolver CamelCase = new CamelCasePropertyNamesContractResolver();
        public static readonly IContractResolver SnakeCase = new DefaultContractResolver 
        { 
            NamingStrategy = new SnakeCaseNamingStrategy() 
        };
        // ... 其他缓存的解析器
    }
}
```

### 4. 模块化架构设计

```csharp
// 模块接口定义
public interface IModule
{
    string Name { get; }
    Version Version { get; }
    void Initialize();
    void Dispose();
}

// 模块管理器
public class ModuleManager
{
    private readonly List<IModule> _modules = new();
    
    public void LoadModule<T>() where T : IModule, new()
    {
        var module = new T();
        module.Initialize();
        _modules.Add(module);
    }
    
    public void LoadModules(Assembly assembly)
    {
        var moduleTypes = assembly.GetTypes()
            .Where(t => typeof(IModule).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);
            
        foreach (var type in moduleTypes)
        {
            var module = (IModule)Activator.CreateInstance(type);
            module.Initialize();
            _modules.Add(module);
        }
    }
}

// 在启动器中使用
public class ModularStartup : IStartup
{
    public int Code => 100;
    public int Weight => 1;
    
    public void Startup()
    {
        var moduleManager = new ModuleManager();
        
        // 加载核心模块
        moduleManager.LoadModule<ConfigModule>();
        moduleManager.LoadModule<JsonModule>();
        moduleManager.LoadModule<MappingModule>();
        
        // 自动发现并加载模块
        var assemblies = AssemblyFinder.FindAssemblies("*.Module.dll");
        foreach (var assembly in assemblies)
        {
            moduleManager.LoadModules(assembly);
        }
        
        SingletonPools.TryAdd<ModuleManager>(moduleManager);
    }
}
```

### 5. 性能监控和诊断

```csharp
public static class PerformanceExtensions
{
    public static T WithTiming<T>(this Func<T> func, Action<TimeSpan> onCompleted)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            return func();
        }
        finally
        {
            stopwatch.Stop();
            onCompleted(stopwatch.Elapsed);
        }
    }
    
    public static async Task<T> WithTimingAsync<T>(this Func<Task<T>> func, Action<TimeSpan> onCompleted)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            return await func();
        }
        finally
        {
            stopwatch.Stop();
            onCompleted(stopwatch.Elapsed);
        }
    }
}

// 使用示例
var result = (() => ExpensiveOperation()).WithTiming(elapsed => 
{
    if (elapsed.TotalMilliseconds > 1000)
    {
        Logger.LogWarning($"Slow operation detected: {elapsed.TotalMilliseconds}ms");
    }
});
```

## 🔧 测试和调试

框架提供了完整的单元测试，位于 [`tests/`](tests/) 目录：

- [`Inkslab.Tests`](tests/Inkslab.Tests/) - 核心功能测试
- [`Inkslab.Json.Tests`](tests/Inkslab.Json.Tests/) - JSON序列化测试  
- [`Inkslab.Config.Tests`](tests/Inkslab.Config.Tests/) - 配置读取测试
- [`Inkslab.Map.Tests`](tests/Inkslab.Map.Tests/) - 对象映射测试

### 示例测试用例

参考 [`StringExtensionsTests`](tests/Inkslab.Tests/StringExtensionsTests.cs) 了解如何编写测试：

```csharp
[Theory]
[InlineData("namingCase", NamingType.Normal, "namingCase")]
[InlineData("NamingCase", NamingType.CamelCase, "namingCase")]  
[InlineData("naming_case", NamingType.PascalCase, "NamingCase")]
[InlineData("UserName", NamingType.SnakeCase, "user_name")]
public void NamingConversionTest(string input, NamingType namingType, string expected)
{
    var result = input.ToNamingCase(namingType);
    Assert.Equal(expected, result);
}
```

## 📈 性能建议

1. **合理使用单例池** - 避免频繁创建重型对象
2. **缓存序列化配置** - JsonSerializerSettings 等配置对象应该缓存
3. **批量操作** - 使用框架提供的批量扩展方法
4. **异步优先** - 在 I/O 密集型操作中优先使用异步方法
5. **监控内存使用** - 定期检查大对象和集合的内存占用

## 🤝 贡献指南

1. Fork 项目
2. 创建特性分支 (`git checkout -b feature/AmazingFeature`)
3. 提交更改 (`git commit -m 'Add some AmazingFeature'`)
4. 推送到分支 (`git push origin feature/AmazingFeature`)
5. 打开 Pull Request

## 📝 许可证

本项目基于 MIT 许可证 - 查看 [LICENSE](LICENSE) 文件了解详情。

---

[![Stargazers over time](https://starchart.cc/tinylit/inkslab.svg)](https://starchart.cc/tinylit/inkslab)