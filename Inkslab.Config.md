![Inkslab](inkslab.jpg 'Logo')

<!-- AI-META
Package: Inkslab.Config
Version: 1.2.23
TargetFrameworks: net461; netstandard2.1; net6.0
Namespace: Inkslab, Inkslab.Config, Inkslab.Config.Settings
Dependencies: Inkslab; Microsoft.Extensions.Configuration (非 NET_Traditional)
EntryContract: IConfigHelper (src/Inkslab/Config/IConfigHelper.cs)
DefaultImplementation: DefaultConfigHelper (src/Inkslab.Config/DefaultConfigHelper.cs)
StartupType: CStartup (src/Inkslab.Config/CStartup.cs)
Keywords: configuration, appsettings, strong-typed config, hot-reload, options
-->

## Inkslab.Config 是什么？

**Inkslab.Config** 是 [`IConfigHelper`](src/Inkslab/Config/IConfigHelper.cs) 的默认实现，提供强类型配置读取与热更新能力，跨 `.NET Framework` / `.NET Standard` / `.NET Core` 一致使用。

- 在 **.NET Framework**：基于 `System.Configuration`（`ConfigurationManager`）。
- 在 **.NET Standard / .NET Core**：基于 `Microsoft.Extensions.Configuration`（默认包含 `appsettings.json` 及 `appsettings.{Environment}.json`）。

---

## 安装

```bash
dotnet add package Inkslab.Config
```

`Inkslab.Config` 会通过 [`CStartup`](src/Inkslab.Config/CStartup.cs) 自动向 `SingletonPools` 注册 `IConfigHelper`，无需显式代码。

---

## 快速入门

### 1. 读取字符串

```csharp
var conn = "ConnectionStrings:Default".Config<string>();
```

### 2. 读取强类型对象

```csharp
public class DatabaseOptions
{
    public string ConnectionString { get; set; }
    public int    Timeout          { get; set; }
}

var db = "Database".Config<DatabaseOptions>();
```

### 3. 指定默认值

```csharp
int pageSize = "UI:PageSize".Config<int>(defaultValue: 20);
```

### 4. 监听变化（.NET Standard / .NET Core）

```csharp
var helper = SingletonPools.Singleton<IConfigHelper>();
helper.OnConfigChanged += sender =>
{
    // 热更新逻辑
};
```

---

## 核心契约

### `IConfigHelper` [src/Inkslab/Config/IConfigHelper.cs](src/Inkslab/Config/IConfigHelper.cs)

```csharp
public interface IConfigHelper
{
#if !NET_Traditional
    /// <summary>配置变更事件（仅非 .NET Framework）。</summary>
    event Action<object> OnConfigChanged;
#endif

    /// <summary>按 key 读取，未找到时返回默认值。</summary>
    T Get<T>(string key, T defaultValue = default);
}
```

### 静态门面 `ConfigHelper` [src/Inkslab/Config/ConfigHelper.cs](src/Inkslab/Config/ConfigHelper.cs)

```csharp
ConfigHelper.Get<string>("ConnectionStrings:Default");
```

### 扩展方法 `Config<T>` [src/Inkslab/Extentions/StringExtensions.cs](src/Inkslab/Extentions/StringExtensions.cs)

```csharp
"Section:Key".Config<int>(defaultValue: 0);
```

---

## 键路径规则

| 运行时 | 分隔符 | 示例 | 说明 |
| --- | --- | --- | --- |
| **.NET Framework** | `/` | `appSettings/key` | 默认读取 `appSettings`；可通过命名段读取 `connectionStrings`、自定义 `ConfigurationSectionGroup` |
| **.NET Standard / .NET Core** | `:` | `Database:ConnectionString` | 与 `Microsoft.Extensions.Configuration` 一致 |

### .NET Framework 细则

- 默认读取 `appSettings` 下的键值。
- 读取连接串：`connectionStrings/{name}`（返回字符串名称）或 `connectionStrings/{name}/connectionString`（返回连接串值）。
- 读取自定义 `ConfigurationSectionGroup` 时，目标类型必须与节点实际类型匹配，否则返回默认值。
- 支持指定运行环境：`new DefaultConfigHelper(RuntimeEnvironment.Service)`，枚举值 `Web` / `Form` / `Service`。

### .NET Standard / .NET Core 细则

- 默认加载 `appsettings.json` 与 `appsettings.{ASPNETCORE_ENVIRONMENT}.json`。
- 支持热更新（文件变更时触发 `OnConfigChanged`）。

---

## 进阶用法

### 自定义 JSON 配置源

实现 [`IJsonConfigSettings`](src/Inkslab.Config/IJsonConfigSettings.cs) 并注入：

```csharp
public class MyJsonConfigSettings : IJsonConfigSettings
{
    public void Config(IConfigurationBuilder builder)
    {
        builder.AddJsonFile("custom.json", optional: true, reloadOnChange: true);
    }
}

// 必须在 XStartup.DoStartup() 之前
SingletonPools.TryAdd<IJsonConfigSettings, MyJsonConfigSettings>();
```

### 内置便捷实现

| 类型 | 位置 | 作用 |
| --- | --- | --- |
| `JsonConfigSettings` | [src/Inkslab.Config/Settings/JsonConfigSettings.cs](src/Inkslab.Config/Settings/JsonConfigSettings.cs) | 以 `Action<IConfigurationBuilder>` 方式快速配置 |
| `JsonPathConfigSettings` | [src/Inkslab.Config/Settings/JsonPathConfigSettings.cs](src/Inkslab.Config/Settings/JsonPathConfigSettings.cs) | 按文件路径追加 JSON 配置 |

### 替换默认实现

```csharp
public class MyConfigHelper : IConfigHelper
{
    public event Action<object> OnConfigChanged;
    public T Get<T>(string key, T defaultValue = default) { /* ... */ }
}

SingletonPools.TryAdd<IConfigHelper, MyConfigHelper>();
```

---

## 单元测试

参见 [tests/Inkslab.Config.Tests/UnitTests.cs](tests/Inkslab.Config.Tests/UnitTests.cs)。

---

## 常见问题

- **配置未生效**：确认在 `XStartup.DoStartup()` 之前完成 `SingletonPools.TryAdd`。
- **.NET Framework 下读取为空**：检查目标类型与配置节类型是否匹配。
- **热更新未触发**：仅 .NET Standard / .NET Core 的 JSON 源默认支持 `reloadOnChange`。
