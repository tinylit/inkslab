![Inkslab](inkslab.jpg 'Logo')

<!-- AI-META
Package: Inkslab.Net
Version: 1.2.23
TargetFrameworks: net461; netstandard2.1; net6.0
Namespace: Inkslab.Net, Inkslab.Net.Options
Dependencies: Inkslab
EntryContract: IRequestFactory (src/Inkslab.Net/IRequestFactory.cs)
DefaultImplementation: RequestFactory (src/Inkslab.Net/RequestFactory.cs)
InitializeHook: IRequestInitialize (src/Inkslab.Net/IRequestInitialize.cs)
Keywords: HTTP, HttpClient, REST, JSON, XML, multipart, download, retry, auth refresh, DataVerify
-->

## Inkslab.Net 是什么？

**Inkslab.Net** 是一个可链式调用的 HTTP 请求客户端，基于 `HttpClient` 实现，提供：

- **流式 API**：URL → 参数 → 头 → 内容 → 转换 → 校验 → 发送。
- **统一序列化**：`Json` / `Xml` / `Form` / `Body`，配合 `JsonCast<T>` / `XmlCast<T>` / `CustomCast<T>` 反序列化。
- **失败重试与认证刷新**：`When(...).ThenAsync(...)` 条件重试链。
- **数据验证**：`DataVerify(...).Success(...).Fail(...)` 语义化结果处理。
- **流下载**：`DownloadAsync`。
- **请求初始化钩子**：`IRequestInitialize`（全局头、鉴权注入等）。

---

## 安装

```bash
dotnet add package Inkslab.Net
```

Inkslab.Net 通过 DI 暴露 `IRequestFactory`，推荐与 `Inkslab.DI` 或 ASP.NET Core 自带的 `IServiceCollection` 一起使用。

---

## 核心契约

### `IRequestFactory` [src/Inkslab.Net/IRequestFactory.cs](src/Inkslab.Net/IRequestFactory.cs)

```csharp
public interface IRequestFactory
{
    IRequestable CreateRequestable(string requestUri);
}
```

### 链式接口族

| 接口 | 作用 | 源文件 |
| --- | --- | --- |
| `IRequestableBase<T>` | 请求头、Query 参数 | [IRequestableBase.cs](src/Inkslab.Net/IRequestableBase.cs) |
| `IRequestable` | 基础请求器（编码、内容设置） | [IRequestable.cs](src/Inkslab.Net/IRequestable.cs) |
| `IRequestableEncoding` | Body 编码（JSON/XML/Form/Body） | [IRequestableContent.cs](src/Inkslab.Net/IRequestableContent.cs) |
| `IRequestableContent` | 已设置内容后的请求器 | [IRequestableContent.cs](src/Inkslab.Net/IRequestableContent.cs) |
| `IDeserializeRequestable` | `JsonCast` / `XmlCast` / `CustomCast` | [IDeserializeRequestable.cs](src/Inkslab.Net/IDeserializeRequestable.cs) |
| `IWhenRequestable` / `IThenRequestable` | 条件重试链 | [IWhenRequestable.cs](src/Inkslab.Net/IWhenRequestable.cs) |
| `IRequestableDataVerify<T>` | 结果校验 → `Success` / `Fail` | [IRequestableDataVerify.cs](src/Inkslab.Net/IRequestableDataVerify.cs) |
| `IStreamRequestable` | `DownloadAsync` 流下载 | [IStreamRequestable.cs](src/Inkslab.Net/IStreamRequestable.cs) |
| `IRequestInitialize` | 全局初始化钩子 | [IRequestInitialize.cs](src/Inkslab.Net/IRequestInitialize.cs) |

---

## 快速入门

### 1. 获取请求器

```csharp
public class MyService
{
    private readonly IRequestFactory _factory;
    public MyService(IRequestFactory factory) => _factory = factory;

    public Task<string> GetAsync() =>
        _factory.CreateRequestable("https://api.example.com/users")
                .AppendQueryString("page", 1)
                .AppendQueryString("size", 20)
                .GetAsync();
}
```

### 2. 查询参数

```csharp
.AppendQueryString("?keyword=test&page=1")
.AppendQueryString("name", "tom")
.AppendQueryString("time", DateTime.UtcNow, "yyyy-MM-ddTHH:mm:ssZ")
.AppendQueryString(new { PageIndex = 1, PageSize = 20 }, NamingType.KebabCase)
```

> **说明**：多次调用同一参数名 → 追加为数组；仅在命中 `ThenAsync` 认证重试时覆盖。

### 3. 请求头

```csharp
.AssignHeader("Authorization", "Bearer <token>")
.AssignHeaders(new Dictionary<string, string>
{
    ["X-Trace-Id"] = traceId,
    ["X-Tenant"]   = tenantId
});
```

---

## 发送请求

每个发送方法默认 `timeout = 1000ms`；流下载默认 `10000ms`。

```csharp
await r.GetAsync();
await r.DeleteAsync();
await r.PostAsync();
await r.PutAsync();
await r.PatchAsync();
await r.HeadAsync();
await r.SendAsync("CONNECT");              // 自定义 HTTP 方法

var stream = await r.DownloadAsync();      // 流下载
```

---

## 请求体

| 方法 | Content-Type | 说明 |
| --- | --- | --- |
| `.Json(obj)` / `.Json<T>(obj, naming)` | `application/json` | 自动调用 `IJsonHelper` |
| `.Xml(obj)` / `.Xml<T>(obj)` | `application/xml` | 基于 `XmlHelper` |
| `.Form(dict)` | `application/x-www-form-urlencoded` | 纯键值对 |
| `.Form(multipart)` | `multipart/form-data` | 文件上传 |
| `.Body(str, contentType)` | 自定义 | 原始字节 |

---

## 响应反序列化

```csharp
// JSON
var dto = await r.Json(req)
                 .JsonCast<ServResult<User>>()
                 .PostAsync();

// XML
var dto = await r.XmlCast<ServResult>()
                 .GetAsync();

// 自定义
var dto = await r.CustomCast(body => Parse(body))
                 .GetAsync();

// 匿名类型
var anon = await r.JsonCast(new { Code = 0, Data = default(User) })
                  .GetAsync();
```

---

## 条件重试 / 认证刷新

```csharp
var data = await factory.CreateRequestable("https://api.example.com/me")
    .AssignHeader("Authorization", $"Bearer {token}")
    .When(status => status == HttpStatusCode.Unauthorized)
    .ThenAsync(async (req, _) =>
    {
        token = await RefreshTokenAsync();
        req.AssignHeader("Authorization", $"Bearer {token}");   // 会覆盖同名头
    })
    .JsonCast<ServResult<UserInfo>>()
    .GetAsync();
```

> **注意**：每一组 `When → ThenAsync` 仅执行**一次**，避免死循环。

---

## 数据验证

```csharp
public class ServResult
{
    public int    Code      { get; set; }
    public bool   Success   { get => Code == 0; set { } }
    public string Msg       { get; set; }
    public DateTime Timestamp { get; set; }
}
public class ServResult<T> : ServResult { public T Data { get; set; } }

int userId = await factory.CreateRequestable("https://api.example.com/user")
    .Json(payload)
    .JsonCast<ServResult<int>>()
    .DataVerify(r => r.Success)
    .Success (r => r.Data)
    .Fail    (r => new BusiException(r.Msg, r.Code))
    .PostAsync();
```

---

## 全局初始化钩子 `IRequestInitialize`

```csharp
public class AuthInitializer : IRequestInitialize
{
    public void Initialize(IRequestableBase req)
        => req.AssignHeader("X-Tenant", TenantContext.Current);
}

// 注册（启动前）
SingletonPools.TryAdd<IRequestInitialize, AuthInitializer>();
```

---

## 编码与异常容忍

```csharp
.UseEncoding(Encoding.UTF8)
.JsonCatch<MyResult>(ex => MyResult.Empty)   // 反序列化失败兜底
.XmlCatch<MyResult>(ex => MyResult.Empty)
```

---

## 单元测试

参见 [tests/Inkslab.Net.Tests/UnitTest1.cs](tests/Inkslab.Net.Tests/UnitTest1.cs)。

---

## 说明要点

- **请求方式**：显式支持 `GET` / `DELETE` / `POST` / `PUT` / `HEAD` / `PATCH`；任意方法使用 `SendAsync(method)`；流场景使用 `DownloadAsync`。
- **JsonCast 依赖**：需有 `IJsonHelper` 实现（推荐 `Inkslab.Json`）。
- **XML 反序列化**：使用 `System.Xml.Serialization`，请配合 `[XmlElement]` / `[XmlIgnore]` 标注。
- **超时**：以毫秒为单位，所有发送方法第一参数均为 `double timeout`。
- **并发**：`IRequestFactory` 与 `HttpClient` 一致，推荐**单例注入**复用连接池。
