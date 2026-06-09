# AssignHeader 跳过 .NET 头验证重载实现计划

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 为 `Inkslab.Net` 的 `AssignHeader` 新增重载 `AssignHeader(string header, string value, bool skipValidation)`，当 `skipValidation=true` 时使用 `HttpHeaders.TryAddWithoutValidation()` 绕过 .NET 的请求头格式验证。

**Architecture:** 在现有 `_headers` 字典旁维护一个 `HashSet<string> _skipValidationHeaders`，记录需跳过验证的头名。通过 `RequestOptions.SkipValidationHeaders` 将此集合传入 `SendAsync`，在发送时按头名决定走 `Add` 还是 `TryAddWithoutValidation`。重试路径（`RequestableBase.SendAsync`）在合并 `_headers` 的同时合并 `_skipValidationHeaders`。

**Tech Stack:** C# 9.0 / .NET (net461; netstandard2.1; net6.0)，`System.Net.Http.HttpHeaders`，xUnit

---

## 文件变更清单

| 文件 | 操作 |
|------|------|
| `src/Inkslab.Net/IRequestableBase.cs` | 修改：泛型接口新增重载声明 |
| `src/Inkslab.Net/Options/RequestOptions.cs` | 修改：新增 `SkipValidationHeaders` 属性，构造函数增加可选参数 |
| `src/Inkslab.Net/RequestFactory.cs` | 修改：`Requestable` + `RequestableBase` 各加字段/重载/GetOptions；`SendAsync` 区分调用 |
| `tests/Inkslab.Net.Tests/UnitTest1.cs` | 修改：新增三个测试方法 |

---

## Task 1：写失败测试 + 接口声明

**Files:**
- Modify: `tests/Inkslab.Net.Tests/UnitTest1.cs`
- Modify: `src/Inkslab.Net/IRequestableBase.cs`

- [ ] **Step 1：向测试文件追加失败测试**

在 `tests/Inkslab.Net.Tests/UnitTest1.cs` 的类末尾（最后一个 `}` 之前）追加：

```csharp
/// <summary>
/// 跳过验证：发送格式不合规的 Date 头不抛出异常。
/// </summary>
[Fact]
public async Task AssignHeader_SkipValidation_AllowsInvalidHeaderValue()
{
    // Date 头值不合规时，HttpHeaders.Add() 会抛出 FormatException。
    // skipValidation=true 改用 TryAddWithoutValidation，不应抛出。
    await RequestFactory.Create("http://www.baidu.com/")
        .AssignHeader("Date", "not-a-valid-date", true)
        .GetAsync();
}
```

- [ ] **Step 2：运行，确认编译失败（红灯）**

```bash
dotnet build tests/Inkslab.Net.Tests
```

预期输出（含）：
```
error CS1503: Argument 3: cannot convert from 'bool' to ...
```
或
```
error CS7036: There is no argument given that corresponds to...
```

- [ ] **Step 3：在 `IRequestableBase<out TRequestable>` 中声明重载**

在 `src/Inkslab.Net/IRequestableBase.cs` 第 25 行 `AssignHeader(string header, string value)` 声明之后、`AssignHeaders` 之前插入：

```csharp
/// <summary>
/// 指定包含与请求或响应相关联的协议头。
/// </summary>
/// <param name="header">协议头。</param>
/// <param name="value">内容。</param>
/// <param name="skipValidation">是否跳过 .NET HttpHeaders 格式验证（使用 TryAddWithoutValidation）。</param>
/// <returns>请求能力。</returns>
TRequestable AssignHeader(string header, string value, bool skipValidation);
```

- [ ] **Step 4：构建接口项目，确认接口编译成功（实现类尚未添加，此步预期报缺失实现）**

```bash
dotnet build src/Inkslab.Net
```

预期：报 `Requestable` / `RequestableBase` 未实现新接口成员的错误（下一步修复）。

- [ ] **Step 5：提交**

```bash
git add tests/Inkslab.Net.Tests/UnitTest1.cs src/Inkslab.Net/IRequestableBase.cs
git commit -m "test+feat: 添加 AssignHeader skipValidation 失败测试；接口声明新重载"
```

---

## Task 2：`RequestOptions` 新增 `SkipValidationHeaders`

**Files:**
- Modify: `src/Inkslab.Net/Options/RequestOptions.cs`

- [ ] **Step 1：更新构造函数并添加属性**

将 `src/Inkslab.Net/Options/RequestOptions.cs` 中的构造函数替换为：

```csharp
/// <summary>
/// 请求配置。
/// </summary>
/// <param name="requestUri">请求地址。</param>
/// <param name="headers">请求头。</param>
/// <param name="skipValidationHeaders">跳过 .NET HttpHeaders 格式验证的请求头名集合。</param>
public RequestOptions(string requestUri, Dictionary<string, string> headers, HashSet<string> skipValidationHeaders = null)
{
    RequestUri = requestUri;
    Headers = headers ?? new Dictionary<string, string>();
    SkipValidationHeaders = skipValidationHeaders ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase);
}
```

在 `Headers` 属性之后添加：

```csharp
/// <summary>
/// 跳过 .NET HttpHeaders 格式验证的请求头名集合（使用 TryAddWithoutValidation）。
/// </summary>
public HashSet<string> SkipValidationHeaders { get; }
```

在文件顶部 using 中补充（如果尚未存在）：

```csharp
using System.Collections.Generic;
```

（注意：`Dictionary<string,string>` 已用 `System.Collections.Generic`，`HashSet` 同命名空间，无需额外 using。）

- [ ] **Step 2：构建，确认编译成功**

```bash
dotnet build src/Inkslab.Net/Options
```

预期：零错误（构造函数第三参数为可选，现有调用者不受影响）。

- [ ] **Step 3：提交**

```bash
git add src/Inkslab.Net/Options/RequestOptions.cs
git commit -m "feat: RequestOptions 新增 SkipValidationHeaders 属性"
```

---

## Task 3：`Requestable` 类实现新重载

**Files:**
- Modify: `src/Inkslab.Net/RequestFactory.cs`（`Requestable` 私有嵌套类，约第 753–897 行）

- [ ] **Step 1：添加 `_skipValidationHeaders` 字段**

在 `private readonly Dictionary<string, string> _headers;`（约第 756 行）之后插入：

```csharp
private readonly HashSet<string> _skipValidationHeaders;
```

- [ ] **Step 2：在公开构造函数中初始化**

在公开构造函数（约第 760–766 行）`_headers = new Dictionary<string, string>();` 之后添加：

```csharp
_skipValidationHeaders = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
```

- [ ] **Step 3：更新私有复制构造函数（供 `UseEncoding` 使用）**

将私有构造函数（约第 768–774 行）替换为：

```csharp
private Requestable(RequestFactory factory, Encoding encoding, QueryString<Requestable> queryString, Dictionary<string, string> headers, HashSet<string> skipValidationHeaders) : base(encoding)
{
    _factory = factory;
    _headers = headers;
    _skipValidationHeaders = skipValidationHeaders;
    _queryString = queryString;
}
```

- [ ] **Step 4：更新 `UseEncoding` 调用复制构造函数处**

在 `UseEncoding` 方法（约第 823–831 行），将：

```csharp
return new Requestable(_factory, encoding, _queryString, _headers);
```

替换为：

```csharp
return new Requestable(_factory, encoding, _queryString, _headers, _skipValidationHeaders);
```

- [ ] **Step 5：添加新重载实现**

在现有 `AssignHeader(string header, string value)` 方法（约第 790–800 行）之后插入：

```csharp
public IRequestable AssignHeader(string header, string value, bool skipValidation)
{
    if (header is null)
    {
        throw new ArgumentNullException(nameof(header));
    }

    _headers[header] = value;

    if (skipValidation)
        _skipValidationHeaders.Add(header);
    else
        _skipValidationHeaders.Remove(header);

    return this;
}
```

- [ ] **Step 6：添加显式接口实现**

在现有 `IRequestableBase IRequestableBase<IRequestableBase>.AssignHeader(string header, string value)`（约第 833–838 行）之后插入：

```csharp
IRequestableBase IRequestableBase<IRequestableBase>.AssignHeader(string header, string value, bool skipValidation)
{
    AssignHeader(header, value, skipValidation);

    return this;
}
```

- [ ] **Step 7：更新 `GetOptions` 传入 `_skipValidationHeaders`**

将 `GetOptions`（约第 817–821 行）替换为：

```csharp
public override RequestOptions GetOptions(HttpMethod method, double timeout) => new RequestOptions(_queryString.ToString(), _headers, _skipValidationHeaders)
{
    Method = method,
    Timeout = timeout,
};
```

- [ ] **Step 8：构建**

```bash
dotnet build src/Inkslab.Net
```

预期：仅剩 `RequestableBase` 未实现新接口成员的错误（Task 4 修复）。

- [ ] **Step 9：提交**

```bash
git add src/Inkslab.Net/RequestFactory.cs
git commit -m "feat: Requestable 实现 AssignHeader skipValidation 重载"
```

---

## Task 4：`RequestableBase`（重试路径）实现新重载

**Files:**
- Modify: `src/Inkslab.Net/RequestFactory.cs`（`ThenRequestable.RequestableBase` 私有嵌套类，约第 962–1075 行）

- [ ] **Step 1：添加 `_skipValidationHeaders` 字段**

在 `private readonly Dictionary<string, string> _headers = new Dictionary<string, string>();`（约第 966 行）之后插入：

```csharp
private readonly HashSet<string> _skipValidationHeaders = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
```

- [ ] **Step 2：添加新重载实现**

在现有 `AssignHeader(string header, string value)` 方法（约第 989–998 行）之后插入：

```csharp
public IRequestableBase AssignHeader(string header, string value, bool skipValidation)
{
    if (header is null)
    {
        throw new ArgumentNullException(nameof(header));
    }

    _headers[header] = value;

    if (skipValidation)
        _skipValidationHeaders.Add(header);
    else
        _skipValidationHeaders.Remove(header);

    return this;
}
```

- [ ] **Step 3：更新 `SendAsync` 合并 `_skipValidationHeaders`**

将 `SendAsync`（约第 1061–1074 行）替换为：

```csharp
public Task<HttpResponseMessage> SendAsync(RequestOptions options, CancellationToken cancellationToken = default)
{
    options.RequestUri = RequestUriRef(options.RequestUri);

    if (_headers.Count > 0)
    {
        foreach (var header in _headers)
        {
            options.Headers[header.Key] = header.Value;
        }
    }

    if (_skipValidationHeaders.Count > 0)
    {
        foreach (var header in _skipValidationHeaders)
        {
            options.SkipValidationHeaders.Add(header);
        }
    }

    return _requestable.SendAsync(options, cancellationToken);
}
```

- [ ] **Step 4：构建，确认零错误**

```bash
dotnet build src/Inkslab.Net
```

预期：零错误。

- [ ] **Step 5：提交**

```bash
git add src/Inkslab.Net/RequestFactory.cs
git commit -m "feat: RequestableBase 实现 AssignHeader skipValidation 重载及重试路径 SkipValidationHeaders 合并"
```

---

## Task 5：`SendAsync` 按 HashSet 切换 Add / TryAddWithoutValidation

**Files:**
- Modify: `src/Inkslab.Net/RequestFactory.cs`（`RequestFactory.SendAsync`，约第 1622–1652 行）

- [ ] **Step 1：替换头发送逻辑**

将（约第 1635–1641 行）：

```csharp
if (options.Headers.Count > 0)
{
    foreach (var kv in options.Headers)
    {
        httpMsg.Headers.Add(kv.Key, kv.Value);
    }
}
```

替换为：

```csharp
if (options.Headers.Count > 0)
{
    foreach (var kv in options.Headers)
    {
        if (options.SkipValidationHeaders.Contains(kv.Key))
            httpMsg.Headers.TryAddWithoutValidation(kv.Key, kv.Value);
        else
            httpMsg.Headers.Add(kv.Key, kv.Value);
    }
}
```

- [ ] **Step 2：构建整个解决方案**

```bash
dotnet build
```

预期：零错误，零警告。

- [ ] **Step 3：运行 Task 1 中的失败测试，确认通过（绿灯）**

```bash
dotnet test tests/Inkslab.Net.Tests --filter "FullyQualifiedName~AssignHeader_SkipValidation_AllowsInvalidHeaderValue"
```

预期：PASS

- [ ] **Step 4：提交**

```bash
git add src/Inkslab.Net/RequestFactory.cs
git commit -m "feat: SendAsync 按 SkipValidationHeaders 切换 Add/TryAddWithoutValidation"
```

---

## Task 6：补充测试

**Files:**
- Modify: `tests/Inkslab.Net.Tests/UnitTest1.cs`

- [ ] **Step 1：追加 skipValidation=false 清理 HashSet 的测试**

在 `AssignHeader_SkipValidation_AllowsInvalidHeaderValue` 之后追加：

```csharp
/// <summary>
/// skipValidation=false 时，若该头名曾以 true 设置，应从跳过集合中移除。
/// </summary>
[Fact]
public async Task AssignHeader_SkipValidationFalse_RemovesFromSkipSet()
{
    // 先以 skipValidation=true 设置，再以 false 覆盖同名头。
    // false 应将该头名从 SkipValidationHeaders 中移除。
    // 此后发送该头时走 Add() 路径，对合法值不抛异常。
    await RequestFactory.Create("http://www.baidu.com/")
        .AssignHeader("X-Test", "skip-value", true)
        .AssignHeader("X-Test", "normal-value", false)
        .GetAsync();
}
```

- [ ] **Step 2：追加重试路径（ThenAsync）中使用 skipValidation 的测试**

```csharp
/// <summary>
/// 重试路径（ThenAsync）中使用 skipValidation=true 不抛异常。
/// </summary>
[Fact]
public async Task AssignHeader_SkipValidation_WorksInRetryPath()
{
    await RequestFactory.Create("http://www.baidu.com/")
        .When(status => status == System.Net.HttpStatusCode.Unauthorized)
        .ThenAsync(r =>
        {
            r.AssignHeader("Date", "not-a-valid-date", true);

            return Task.CompletedTask;
        })
        .GetAsync();
}
```

- [ ] **Step 3：运行全部测试**

```bash
dotnet test tests/Inkslab.Net.Tests
```

预期：全部 PASS。

- [ ] **Step 4：提交**

```bash
git add tests/Inkslab.Net.Tests/UnitTest1.cs
git commit -m "test: 补充 AssignHeader skipValidation 场景测试"
```

---

## Task 7：全解决方案构建验证

- [ ] **Step 1：构建并运行所有测试**

```bash
dotnet build && dotnet test
```

预期：零构建错误，所有测试通过。
