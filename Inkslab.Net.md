![Inkslab](inkslab.jpg 'Logo')

<!-- AI-META
Package: Inkslab.Net
Version: 2.0.0
TargetFrameworks: net461; netstandard2.1; net6.0; net8.0; net10.0
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
- **原生流上传/下载**：`Stream(Stream, ...)`、`Content(HttpContent)`、`DownloadAsync`。
- **实体属性校验**：核心包的 `[ValidateInput]` / `[ValidateOutput]` 分别启用请求与响应自动校验；原生 `.Validation()` 优先配置最终响应校验。
- **请求初始化钩子**：`IRequestInitialize`（全局头、鉴权注入等）。

---

## 原生 Stream 支持与所有权

`Content(HttpContent)` 和 `Stream(Stream, string, bool)` 直接定义在 `IRequestableEncoding`，`DownloadAsync` 定义在 `IStreamRequestable`。这些都是接口实例方法，不提供流或内容工厂委托重载。

```csharp
using var source = File.OpenRead(path);
var result = await RequestFactory.Create(url)
    .AssignHeader("Authorization", token)
    .Stream(source, "application/octet-stream", leaveOpen: true)
    .PostAsync(30000, cancellationToken);

using var content = new StreamContent(File.OpenRead(path));
content.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
await RequestFactory.Create(url).Content(content).PostAsync(30000, cancellationToken);

using var destination = File.Create(destinationPath);
using var download = await RequestFactory.Create(url).DownloadAsync(120000, cancellationToken);
await download.CopyToAsync(destination, 81920, cancellationToken);
```

直接流/HttpContent/原始 Form 内容实例只消费一次。`leaveOpen=true` 保持调用方源流打开，不能使请求可重试；默认请求结束后关闭源流。未发送前的实例仍由调用方拥有，使用 using 覆盖构建失败或放弃发送的路径。再次发送需要新实例和新请求。When 命中一次性内容时，在执行 Then 前抛 InvalidOperationException。

原有字符串正文和全部字段可重建的 Form（如 FileInfo）仍能重试，FileInfo 每次按流打开；含直接 Stream 的表单不可重放。字段中的 Stream 现在可单独触发 multipart，无需同时传入 FileInfo。请先设置头部/Query，再设置正文，之后添加 When/Then 或反序列化。

DownloadAsync 收到成功响应头后返回，正文逐块读取；调用方必须用 using 释放返回流，读到 EOF 不会自动释放。Dispose 会关闭响应及传输资源；支持异步释放的目标框架可用 await using，同时等待尚未结束的上传退出。复制使用 Stream.CopyToAsync，目标流的生命周期由调用方管理，失败可能留下部分数据。

DownloadAsync 的 timeout 按每次 HTTP 尝试计，并持续约束该次响应的正文读取，不包含 Then 回调和目标流写入的总预算。需要约束整个下载和复制时，由调用方创建带超时的 CancellationTokenSource，并将其 Token 同时传给 DownloadAsync 与 CopyToAsync；中断仍依赖自定义流配合取消。Then 回调的等待响应调用方取消，但其签名不接收 token，框架不能强制终止回调本身。

RequestFactory 使用框架共享的静态 HttpClient，保留 `RequestFactory(IRequestInitialize)` 构造方式。原 protected SendAsync 扩展点和 RequestOptions.Content 读取继续支持，每次发送按内容来源创建或领取内容。

上传的内部 HttpContent 直接复制源流并根据 leaveOpen 决定是否关闭它，不再使用 NonDisposingStream。下载时只有原始响应内容拥有底层流；返回的 ResponseOwnedStream 负责释放响应及整个传输作用域。

RequestAttemptScope 是内部对象，调用方不需要获取或手动释放它。普通发送和原始响应回调在 finally 中释放响应，并结束该次上传；构造或发送失败同样清理作用域；重试先结束旧尝试，再创建新尝试。只有 DownloadAsync 将这项所有权转交给返回流。

## 原生实体属性校验

```csharp
using Inkslab.Net.Validation;
using Inkslab.Annotations;

[ValidateInput]
public sealed class CreateRequest
{
    [System.ComponentModel.DataAnnotations.Required]
    public string Name { get; init; }
}

[ValidateOutput]
public sealed class CreateResult
{
    [System.ComponentModel.DataAnnotations.Required]
    public string Id { get; init; }
}
```

```csharp
// 标记开启自动校验，不需要显式 Validation。
var created = await RequestFactory.Create(url)
    .Json(new CreateRequest { Name = "example" })
    .JsonCast<CreateResult>().PostAsync(30000, cancellationToken);

var response = await RequestFactory.Create(url)
    .Json(dto) // 只有标记 ValidateInput 的实体才执行入参校验。
    .JsonCast<ResponseDto>()
    .DataVerify(result => result.Success)
    .Success(result => result.Data)
    .Fail<InvalidOperationException>(result => new InvalidOperationException(result.Message))
    .Validation(new ValidationOptions { AllowNull = false })
    .PostAsync(30000, cancellationToken);
```

两个标记位于核心 `Inkslab` 包的 `Inkslab.Annotations`，只依赖 System.Attribute。支持类和结构体，不重复、不继承；派生 DTO 需要自行标记。已有 Required/Range/IValidatableObject 只定义规则，不会单独开启请求校验。

现有 `Json<T>`、`Xml<T>`、`Form(object,...)` 和实体形式的 `AppendQueryString` 仅对标记 ValidateInput 的 DTO 在序列化/属性展开前校验，失败不发送 HTTP。非空实例按运行时具体类型判断；泛型 null 按声明类型判断，标记实体的 null 拒绝，未标记则沿原序列化路径。Form(object) 的 null 没有类型信息，仍忽略；未标记 Query null 同样忽略。原始字符串、Stream、HttpContent 保持原始内容行为。

显式 `.Validation()` 跳过非空的普通标量、字典、数组和其它集合。自动输入/输出校验则以标记为准：容器子类自身有对应标记时，也会校验该实例的属性和类型规则，但不会遍历元素；子属性的标记不会自动启用外层，也不递归验证。有显式输出配置时优先采用显式策略，因此带标记的容器也按显式规则跳过。实体标记逐次通过 Type.IsDefined 判断，不维护类型或标记缓存；没有启用校验的类型不创建 ValidationContext。

响应统一在最外层取得最终结果后选择一次策略：有显式 `.Validation(options)` 时优先采用显式选项；否则最终实体标记 ValidateOutput 时采用默认选项；两者都没有则不校验。顺序为“解析/Catch → DataVerify → Success/Fail → 输出校验 → 返回”，校验映射或兜底后的最终结果，业务异常直接抛出。没有业务判断时可直接 `.JsonCast<T>().Validation()`。

Success<TResult> 后按最终 TResult 实例判断标记，不提前校验被替换的响应外壳。最终值为 null 时使用最终声明类型判断，Nullable<T> 检查底层 T；显式 AllowNull 可覆盖自动校验的默认拒绝规则。HTTP 重试只对最终输出进行一次校验，每次新的顶层调用独立判断。

Validation 返回 `IRequestable<T>`，之后只提供发送方法，不再追加业务处理；`.Validation().Validation()` 在编译期失败。包装对象不实现 IRequestableValidation<T>，显式配置不会再次套用。响应 `ValidationOptions` 是 `readonly struct`，AllowNull、ServiceProvider、Items 均使用 `init`。调用 `.Validation()` 或传入 ValidationOptions.Default 仍算显式开启，无需创建选项对象，默认拒绝 null。仅非空 Items 在配置请求链时浅复制，后续修改原字典不影响该请求；其中引用类型的值和服务提供者本身不做深复制。实体校验上下文逐次创建，支持标准属性、类型规则、IValidatableObject，默认不递归并遵循标准 Validator 的阶段短路规则。

```csharp
var options = new ValidationOptions { AllowNull = true };
var entity = await RequestFactory.Create(url).JsonCast<ResponseDto>()
    .Validation(options).GetAsync();
```

此类型由 class 改为 struct，不保持二进制兼容，需要重新编译消费者；原先 `.Validation(null)` 改为 `.Validation()` 或 `.Validation(default)`，初始化后的属性赋值改用对象初始化器。显式校验的 null 策略保留，标量 nullable 结果为空时同样默认拒绝，可通过 AllowNull 放行。

校验失败抛 HttpEntityValidationException，包含 Request/Response 阶段和 ValidationResults；不自动重试、不附加完整正文。未标记的请求实体恢复原有处理，不因旧 DTO 预先声明的属性规则被新增拦截。

### 接口实现方迁移

自定义实现 IRequestableEncoding、IStreamRequestable、IRequestableExtend 及 DataVerify 结果接口的代码需要补齐新增原生成员并重新编译。本次支持常规 .NET 6 / .NET 8 / .NET 10，不承诺完整 trimming/NativeAOT；JSON 默认仍为 Newtonsoft。

## 安装命令

```bash
dotnet add package Inkslab.Net
```

Inkslab.Net 通过 DI 暴露 `IRequestFactory`，推荐与 `Inkslab.DI` 或 ASP.NET Core 自带的 `IServiceCollection` 一起使用。

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

## 发送请求

每个发送方法默认 `timeout = 1000ms`；流下载默认 `10000ms`。所有方法均接受可选的 `CancellationToken`：

```csharp
using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

await r.GetAsync(cancellationToken: cts.Token);
await r.PostAsync(3000, cts.Token);            // timeout=3000ms，带取消令牌
await r.DeleteAsync();
await r.PutAsync();
await r.PatchAsync();
await r.HeadAsync();
await r.SendAsync("CONNECT");                  // 自定义 HTTP 方法

using var stream = await r.DownloadAsync(30000, cts.Token);  // 流下载，默认 10000ms
```

---

## 请求体

| 方法 | Content-Type | 说明 |
| --- | --- | --- |
| `.Json(obj)` / `.Json<T>(obj, naming)` | `application/json` | 自动调用 `IJsonHelper` |
| `.Xml(obj)` / `.Xml<T>(obj)` | `application/xml` | 基于 `XmlHelper` |
| `.Form(dict)` | `application/x-www-form-urlencoded` | 纯键值对（`IEnumerable<KV<string,string>>`） |
| `.Form(multipart)` | `multipart/form-data` | 传入 `MultipartFormDataContent` |
| `.Form(body, NamingType)` | 自动检测 | **智能路由**：值含 `FileInfo` → `multipart`；否则 → `form-urlencoded` |
| `.Body(str, contentType)` | 自定义 | 原始字符串内容 |

**`Form` 智能路由示例**（最常用的对象/字典重载）：

```csharp
// IEnumerable<KeyValuePair<string, object>>：值含 FileInfo 时自动 multipart
var fields = new Dictionary<string, object>
{
    ["title"] = "报告",
    ["file"]  = new FileInfo("report.pdf")   // 触发 multipart/form-data
};
await factory.CreateRequestable(url).Form(fields).PostAsync();

// 对象重载：同上，属性含 FileInfo → multipart
await factory.CreateRequestable(url)
    .Form(new { title = "报告", file = new FileInfo("report.pdf") }, NamingType.SnakeCase)
    .PostAsync();
```

---

## 响应反序列化

```csharp
// JSON（泛型）
var dto = await r.Json(req)
                 .JsonCast<ServResult<User>>()
                 .PostAsync();

// JSON（匿名类型）
var anon = await r.JsonCast(new { Code = 0, Data = default(User) })
                  .GetAsync();

// XML（泛型）
var dto = await r.XmlCast<ServResult>()
                 .GetAsync();

// XML（匿名类型）
var anon = await r.XmlCast(new { Code = 0 })
                  .GetAsync();

// 自定义：字符串 → T（框架自动检测 HTTP 状态，非 2xx 时工厂不执行）
var dto = await r.CustomCast(body => Parse(body))
                 .GetAsync();

// 自定义：原始响应 → T（不检测 HTTP 状态，完全由工厂决定如何处理）
var dto = await r.CustomCast(async (response, ct) =>
                 {
                     var bytes = await response.Content.ReadAsByteArrayAsync();
                     return Decode(bytes);
                 })
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

**多条件 OR 重试**：在同一 `ThenAsync` 前叠加多个 `When`/`.Or()`：

```csharp
var data = await factory.CreateRequestable(url)
    .When(s => s == HttpStatusCode.Unauthorized)
    .Or(s => s == HttpStatusCode.Forbidden)          // 任一条件满足即触发重试
    .ThenAsync(async (req, _) =>
    {
        req.AssignHeader("Authorization", $"Bearer {await RefreshTokenAsync()}");
    })
    .JsonCast<ServResult<UserInfo>>()
    .GetAsync();
```

> **注意**：每一组 `When/Or → ThenAsync` 在每次顶层发送中最多重试**一次**，避免死循环；复用可重放请求链时，每次发送独立记录。多个独立重试机制可链式追加（`ThenAsync` 后可再接 `When`）。一次性流或内容命中重试条件时，在执行 Then 回调前拒绝重放。

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
