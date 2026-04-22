![Inkslab](inkslab.jpg 'Logo')

<!-- AI-META
Package: Inkslab.Json
Version: 1.2.23
TargetFrameworks: net461; netstandard2.1; net6.0
Namespace: Inkslab, Inkslab.Serialize.Json
Dependencies: Inkslab; Newtonsoft.Json
EntryContract: IJsonHelper (src/Inkslab/Serialize/Json/IJsonHelper.cs)
Facade: JsonHelper (src/Inkslab/Serialize/Json/JsonHelper.cs)
DefaultImplementation: DefaultJsonHelper (src/Inkslab.Json/DefaultJsonHelper.cs)
StartupType: JStartup (src/Inkslab.Json/JStartup.cs)
Keywords: JSON, serialization, Newtonsoft, naming, camelCase, snake_case, anonymous types
-->

## Inkslab.Json 是什么？

**Inkslab.Json** 是 [`IJsonHelper`](src/Inkslab/Serialize/Json/IJsonHelper.cs) 基于 [Newtonsoft.Json](https://www.nuget.org/packages/Newtonsoft.Json) 的默认实现，提供：

- 统一的 `NamingType` 命名风格转换。
- 缩进格式化输出。
- 属性忽略（`[Ignore]`）。
- 匿名类型反序列化。
- 可替换的 `IJsonHelper` 契约（切换到 `System.Text.Json` 或其它实现零侵入）。

---

## 安装

```bash
dotnet add package Inkslab.Json
```

安装后由 [`JStartup`](src/Inkslab.Json/JStartup.cs) 自动注册 `IJsonHelper` 默认实现。

---

## 快速入门

### 1. 序列化

```csharp
var json = JsonHelper.ToJson(new { Id = 1, Name = "Tom" });
// => {"Id":1,"Name":"Tom"}

var camel = JsonHelper.ToJson(dto, NamingType.CamelCase);
var snake = JsonHelper.ToJson(dto, NamingType.SnakeCase, indented: true);
```

### 2. 反序列化

```csharp
public class A
{
    [Ignore]                       // 序列化/反序列化均忽略
    public int    A1 { get; set; } = 100;
    public int    A2 { get; set; }
    public string A3 { get; set; } = string.Empty;
    public DateTime A4 { get; set; }
}

var json = "{\"A2\":100,\"A3\":\"A3\",\"A4\":\"2022-12-03T14:17:55+08:00\"}";
var dto  = JsonHelper.Json<A>(json);
```

### 3. 匿名类型反序列化

```csharp
var anon = JsonHelper.Json(json, new { A2 = 0, A3 = "" }, NamingType.CamelCase);
// anon.A2 / anon.A3 强类型可用
```

---

## 核心契约

### `IJsonHelper` [src/Inkslab/Serialize/Json/IJsonHelper.cs](src/Inkslab/Serialize/Json/IJsonHelper.cs)

```csharp
public interface IJsonHelper
{
    string ToJson<T>(T    jsonObj,                     NamingType namingType = NamingType.Normal, bool indented = false);
    string ToJson   (object jsonObj, Type type,         NamingType namingType = NamingType.Normal, bool indented = false);

    T      Json<T> (string json,                        NamingType namingType = NamingType.Normal);
    object Json    (string json, Type type,             NamingType namingType = NamingType.Normal);
}
```

### 静态门面 `JsonHelper` [src/Inkslab/Serialize/Json/JsonHelper.cs](src/Inkslab/Serialize/Json/JsonHelper.cs)

```csharp
JsonHelper.ToJson(obj, NamingType.CamelCase);
JsonHelper.Json<T>(json);
JsonHelper.Json(json, new { Id = 0, Name = "" });   // 匿名类型
```

### 默认实现 `DefaultJsonHelper` [src/Inkslab.Json/DefaultJsonHelper.cs](src/Inkslab.Json/DefaultJsonHelper.cs)

```csharp
public DefaultJsonHelper();
public DefaultJsonHelper(JsonSerializerSettings settings);   // 可注入自定义 Newtonsoft 设置
```

---

## 命名风格

配合 [`NamingType`](src/Inkslab/NamingType.cs)：

| 值 | 效果 |
| --- | --- |
| `Normal` | 原样 |
| `CamelCase` | `userName` |
| `SnakeCase` | `user_name` |
| `PascalCase` | `UserName` |
| `KebabCase` | `user-name` |

```csharp
var json = JsonHelper.ToJson(dto, NamingType.SnakeCase);
var dto2 = JsonHelper.Json<UserDto>(json, NamingType.SnakeCase);
```

---

## 自定义实现

### 1. 实现 `IJsonHelper`

```csharp
public class MyJsonHelper : IJsonHelper
{
    public string ToJson<T>(T jsonObj, NamingType namingType = NamingType.Normal, bool indented = false)
        => /* 自定义序列化 */;

    public string ToJson(object jsonObj, Type type, NamingType namingType = NamingType.Normal, bool indented = false)
        => /* ... */;

    public T Json<T>(string json, NamingType namingType = NamingType.Normal)
        => /* 自定义反序列化 */;

    public object Json(string json, Type type, NamingType namingType = NamingType.Normal)
        => /* ... */;
}
```

### 2. 启动前注册

```csharp
SingletonPools.TryAdd<IJsonHelper, MyJsonHelper>();

using var startup = new XStartup();
startup.DoStartup();
```

---

## 属性忽略

```csharp
public class User
{
    public int     Id   { get; set; }
    public string  Name { get; set; }

    [Ignore]                          // 来自 Inkslab.Annotations
    public string  Password { get; set; }
}
```

---

## 单元测试

参见 [tests/Inkslab.Json.Tests/UnitTests.cs](tests/Inkslab.Json.Tests/UnitTests.cs)。

---

## 说明

- 基于 `Newtonsoft.Json` 13.x。
- 命名策略通过 `ContractResolver` 实现，内部已缓存常用策略。
- 需要 `System.Text.Json` 时，自行实现 `IJsonHelper` 并通过 `SingletonPools` 替换即可。
