# 设计文档：AssignHeader 跳过 .NET 头验证重载

**日期**：2026-06-08  
**模块**：`Inkslab.Net`

## 背景

.NET `HttpHeaders.Add()` 对头名/值格式执行 RFC 验证，不合规时抛出 `FormatException`。框架需提供一种方式，允许调用方对指定请求头使用 `TryAddWithoutValidation()` 绕过此验证。

## 目标

在现有 `AssignHeader(string header, string value)` 基础上，新增重载 `AssignHeader(string header, string value, bool skipValidation)`，通过 `skipValidation` 标记指定头在发送时走 `TryAddWithoutValidation`。

## 设计

### 接口（`IRequestableBase.cs`）

泛型接口 `IRequestableBase<out TRequestable>` 和非泛型接口 `IRequestableBase` 各新增一个重载：

```csharp
TRequestable AssignHeader(string header, string value, bool skipValidation);
```

### 存储（`RequestFactory.cs` 内部类）

`Requestable` 和 `ThenRequestable.RequestableBase` 各自新增字段：

```csharp
private readonly HashSet<string> _skipValidationHeaders
    = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
```

头值的存储与覆盖逻辑维持原 `_headers` 字典不变。

### 重载实现

```csharp
public IRequestable AssignHeader(string header, string value, bool skipValidation)
{
    if (header is null) throw new ArgumentNullException(nameof(header));
    _headers[header] = value;
    if (skipValidation)
        _skipValidationHeaders.Add(header);
    else
        _skipValidationHeaders.Remove(header);
    return this;
}
```

- `skipValidation=true`：将头名加入 HashSet。
- `skipValidation=false`：从 HashSet 移除（清理之前可能设置的跳过标记）。

### `RequestOptions`（`Options/RequestOptions.cs`）

新增只读属性：

```csharp
public HashSet<string> SkipValidationHeaders { get; }
```

构造函数签名扩展为：

```csharp
public RequestOptions(string requestUri, Dictionary<string, string> headers,
    HashSet<string> skipValidationHeaders)
```

`Requestable.GetOptions()` 传入 `_skipValidationHeaders`。

### 发送逻辑（`RequestFactory.SendAsync`）

```csharp
foreach (var kv in options.Headers)
{
    if (options.SkipValidationHeaders.Contains(kv.Key))
        httpMsg.Headers.TryAddWithoutValidation(kv.Key, kv.Value);
    else
        httpMsg.Headers.Add(kv.Key, kv.Value);
}
```

## 影响范围

| 文件 | 改动 |
|------|------|
| `src/Inkslab.Net/IRequestableBase.cs` | 泛型 + 非泛型接口各加一个重载声明 |
| `src/Inkslab.Net/Options/RequestOptions.cs` | 新增 `SkipValidationHeaders` 属性；构造函数增加参数 |
| `src/Inkslab.Net/RequestFactory.cs` | `Requestable`、`RequestableBase` 各加 HashSet 字段、重载实现、`GetOptions` 传参；`SendAsync` 区分 `Add`/`TryAddWithoutValidation` |

## 测试要点

- `skipValidation=true` 时，发送格式不合规的头不抛异常。
- `skipValidation=false`（或先 `true` 后 `false`）时，HashSet 中该头名被清除，发送时仍走 `Add`。
- 原有 `AssignHeader(header, value)` 行为不变。
