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

以下 6 个 `src/` 子目录各自对应一个独立的 NuGet 包，是唯一可被引用的交付物：

```
inkslab/
└─ src/
   ├─ Inkslab/          # 核心基础包（所有场景均需引用）
   ├─ Inkslab.Config/   # 配置读取           → 文档: Inkslab.Config.md
   ├─ Inkslab.Json/     # JSON 序列化         → 文档: Inkslab.Json.md
   ├─ Inkslab.Map/      # 对象映射            → 文档: Inkslab.Map.md
   ├─ Inkslab.DI/       # 依赖注入自动装配    → 文档: Inkslab.DI.md
   └─ Inkslab.Net/      # HTTP 请求客户端     → 文档: Inkslab.Net.md
```

### 模块选择指南

> **AI 使用提示**：根据下表"适用场景"一列匹配需求，点击"详细文档"链接获取完整 API 参考。`Inkslab`（核心包）是所有模块的共同依赖，始终需要引用。下表仅列出可通过 NuGet 引用的 6 个包，源码仓库中的 `tests/` 不在其中。

| 包名 | 适用场景 | 核心 API | 详细文档 |
|------|---------|---------|---------|
| `Inkslab` | 单例池注册/获取、框架启动协调、雪花算法主键生成、LRU/LFU 有界缓存、异步锁、字符串/集合/日期/加密扩展方法、模板格式化（StringSugar）、XML 序列化、正则常量 | `SingletonPools`、`XStartup`、`KeyGen`、`Lru<K,V>`、`Lfu<K,V>`、`AsynchronousLock` | 本文 |
| `Inkslab.Config` | 读取配置文件（`appsettings.json` / `app.config`）；强类型对象绑定；热更新监听 | `"key".Config<T>()`、`IConfigHelper`、`ConfigHelper` | [Inkslab.Config.md](./Inkslab.Config.md) |
| `Inkslab.Json` | JSON 序列化与反序列化；命名风格转换（camelCase / snake_case）；匿名类型反序列化；属性忽略（`[Ignore]`）；键名覆盖（`[JsonProperty]`） | `JsonHelper.ToJson()`、`JsonHelper.Json<T>()` | [Inkslab.Json.md](./Inkslab.Json.md) |
| `Inkslab.Map` | 不同类型对象互转（Entity ↔ DTO）；集合/字典映射；深拷贝；构造函数映射；自定义字段改名/忽略/常量 | `Mapper.Map<T>()`、`MapperInstance`、`Profile` | [Inkslab.Map.md](./Inkslab.Map.md) |
| `Inkslab.DI` | 向 `IServiceCollection` 自动注册服务（按 `[Singleton]`/`[Scoped]`/`[Transient]` 特性或 `IConfigureServices` 约定），取代手动 `AddSingleton`/`AddScoped` 等调用 | `.DependencyInjection(options).SeekAssemblies().ConfigureByDefined().ConfigureByAuto()` | [Inkslab.DI.md](./Inkslab.DI.md) |
| `Inkslab.Net` | 发送 HTTP/REST 请求；链式设置请求头、Query 参数、请求体（JSON/XML/Form/multipart）；响应反序列化；条件重试；数据验证；流下载 | `IRequestFactory.CreateRequestable(url)`、`.JsonCast<T>()`、`.When().ThenAsync()` | [Inkslab.Net.md](./Inkslab.Net.md) |

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

### `Singleton<T>` 泛型访问器

`SingletonPools.Singleton<T>()` 的类型安全封装，适合在静态字段或字段初始化器中使用，内部采用嵌套类延迟初始化，性能与直接调用等价：

```csharp
private static readonly IJsonHelper _json = Singleton<IJsonHelper>.Instance;
private static readonly DefaultSettings _settings = Singleton<DefaultSettings>.Instance;
```

### `Regexs` 预编译正则

位于 `Inkslab.Regexs`，提供预编译的常用正则（字段以 `Is` 开头表示完整匹配，否则表示"包含"）：

| 成员 | 类型 | 说明 |
| --- | --- | --- |
| `IsMail` | `Regex` | 邮箱地址完整匹配 |
| `IsNumber` | `Regex` | 数字（含正负号与小数点） |
| `Whitespaces` | `Regex` | 含空白符（包括 `\t\r\n`） |
| `ChineseCharacters` | `Regex` | 含中文字符 |
| `DoubleByteCharacters` | `Regex` | 含双字节字符（含汉字） |

```csharp
Regexs.IsMail.IsMatch("user@example.com");     // true
Regexs.IsNumber.IsMatch("-3.14");               // true
Regexs.ChineseCharacters.IsMatch("Hello你好");  // true
```

### `XmlHelper` XML 序列化

位于 `Inkslab.Serialize.Xml`，提供静态 XML 序列化/反序列化，与 `Inkslab.Net` 的 `.XmlCast<T>()` 内部保持一致：

```csharp
using Inkslab.Serialize.Xml;

// 序列化（失败时 obj 为 null 返回 null）
string xml = XmlHelper.XmlSerialize(dto);
string xml = XmlHelper.XmlSerialize(dto, Encoding.UTF8, indented: true);

// 反序列化（失败静默返回 null / default，不抛出异常）
var dto = XmlHelper.XmlDeserialize<MyDto>(xml);
var obj = XmlHelper.XmlDeserialize(xml, typeof(MyDto));
```

---

## 注解（`Inkslab.Annotations`）

跨模块共享的元数据注解，无额外包依赖（位于核心 `Inkslab` 包）：

| 注解 | 作用目标 | 用途 |
| --- | --- | --- |
| `[Export]` | 类 | DI 标记基类；`[Singleton]` / `[Scoped]` / `[Transient]` 均继承自此 |
| `[Import]` | 构造器 / 属性 / 字段 | 标记 DI 注入点 |
| `[Ignore]` | 属性 / 字段 | Map 映射与 JSON 序列化时跳过该成员 |
| `[JsonProperty("name")]` | 属性 / 字段 / 参数 | 覆盖 JSON 序列化 / 反序列化时使用的键名 |
| `[Match("groupName")]` | 方法参数 | `AdapterSugar` 中将参数绑定到指定命名正则组 |
| `[Mismatch("groupName")]` | 方法（可多次叠加） | `AdapterSugar` 中：当指定命名正则组匹配成功时排除此方法 |

```csharp
public class User
{
    public int    Id   { get; set; }

    [JsonProperty("user_name")]     // JSON 输出 key 为 user_name
    public string Name { get; set; }

    [Ignore]                        // Map 映射与 JSON 均跳过
    public string Password { get; set; }
}
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

### `StringSugar` 属性模板格式化

通过扩展方法 `string.StringSugar(source)` 将模板字符串中的 `${PropertyName}` 占位符替换为对象属性值，底层正则规则由 `IStringSugar` 决定（可通过 `SingletonPools.TryAdd<IStringSugar, MyStringSugar>()` 替换）。

默认实现（`DefaultStringSugar`）支持以下语法：

| 语法 | 说明 | 示例 |
| --- | --- | --- |
| `${Name}` | 属性值 | `${UserName}` |
| `${Name:format}` | 带格式化，调用 `.ToString(format)` | `${CreateAt:yyyy-MM}`、`${Status:D}` |
| `${Name:#}` | 字符串属性的字符数（Length） | `${Title:#}` |
| `${Name:0..5}` | 字符串属性的子串（Substring） | `${Title:0..5}` |
| `${A?B}` / `${A??B}` | 空值合并：A 为 null 则返回 B，否则返回 A | `${Nickname?UserName}` |
| `${A+B}` | 值合并（数值相加 / 字符串拼接） | `${First+Last}` |
| `${A?+B}` | 空试探合并：A 为 null 则返回 null，否则返回 A+B | `${Prefix?+Name}` |

```csharp
var user = new { Name = "张三", Age = 30, Birthday = new DateTime(1994, 6, 1) };
"姓名：${Name}，年龄：${Age}".StringSugar(user);
// => "姓名：张三，年龄：30"

"生日：${Birthday:yyyy-MM-dd}".StringSugar(user);
// => "生日：1994-06-01"

var order = new { Code = (string)null, BackupCode = "B001" };
"编号：${Code?BackupCode}".StringSugar(order);
// => "编号：B001"
```

通过 `DefaultSettings` 控制格式化行为：

```csharp
// DefaultSettings（默认）：字符串直接输出，null 输出 ""
"姓名=${Name}".StringSugar(user, new DefaultSettings());

// JsonSettings：字符串类型加双引号，null 输出 "null"
"姓名=${Name}".StringSugar(user, new JsonSettings());
// => 姓名="张三"
```

**扩展 `AdapterSugar<T>`**：继承此抽象类可自定义模板解析规则。在子类方法参数上使用 `[Match("groupName")]` 将参数绑定到指定命名正则组，在方法上使用 `[Mismatch("groupName")]` 声明"该命名组匹配时不触发此方法"。

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