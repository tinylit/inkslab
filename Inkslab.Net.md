
![Inkslab](inkslab.jpg 'Logo')

## "Inkslab.Net"是什么？

**Inkslab.Net** 是一个高性能、易扩展的 HTTP/HTTPS 请求工具，支持认证刷新、重试、序列化/反序列化、数据验证、文件上传下载等功能，适用于多种 .NET 应用场景。

---


## 🚀 快速入门

### 1. 获得请求能力
```csharp
// 注入 IRequestFactory 接口
// 使用 IRequestFactory.CreateRequestable("{api}") 获得请求能力
```

### 2. 普通请求
```csharp
string result = await requestFactory.CreateRequestable("api")
  .AppendQueryString("?{params}")
  .GetAsync();
```

### 3. 认证信息刷新请求
```csharp
string result = await requestFactory.CreateRequestable("api")
  .AppendQueryString("?{params}")
  .AssignHeader("Authorization", "Bearer 3506555d8a256b82211a62305b6dx317")
  .When(status => status == HttpStatusCode.Unauthorized)
  .ThenAsync((requestable, e) => {
    // 刷新认证信息
    return Task.CompletedTask;
  })
  .GetAsync();
```

### 4. 序列化、反序列化、验证与重发

#### 结果实体
```csharp
public class ServResult
{
  [XmlElement("code")]
  public int Code { get; set; }
  private bool? success = null;
  [XmlIgnore]
  public bool Success {
    get => success ?? Code == StatusCodes.OK;
    set => success = value;
  }
  [XmlElement("msg")]
  public string Msg { get; set; }
  [XmlElement("timestamp")]
  public DateTime Timestamp { get; set; }
}

public class ServResult<TData> : ServResult
{
  [XmlElement("data")]
  public TData Data { get; set; }
}
```

#### 序列化
```csharp
string result = await requestFactory.CreateRequestable("api")
  .Json(new {
    Date = DateTime.Now,
    TemperatureC = 1,
    Summary = 50
  })
  .PostAsync();
```

#### 反序列化
```csharp
ServResult result = await requestFactory.CreateRequestable("api")
  .Json(new {
    Date = DateTime.Now,
    TemperatureC = 1,
    Summary = 50
  })
  .JsonCast<ServResult>()
  .PostAsync();
```

#### 验证
```csharp
int result = await requestFactory.CreateRequestable("api")
  .Json(new {
    Date = DateTime.Now,
    TemperatureC = 1,
    Summary = 50
  })
  .JsonCast<ServResult<int>>()
  .DataVerify(r => r.Success)
  .Success(r => r.Data)
  .Fail(r => new BusiException(r.Msg, r.Code))
  .PostAsync();
```

---


## 📖 说明

**基础请求配置**
- `AssignHeader` 设置请求头
- `AppendQueryString` 添加请求参数（多次添加同名参数不会覆盖，数组场景；认证刷新时重设会覆盖）

**请求方式**
- 显式支持：GET、DELETE、POST、PUT、HEAD、PATCH
- 隐式支持：`SendAsync` 方法，第一个参数为请求方式
- 流处理：`DownloadAsync` 流下载

**数据传输**
- Json：`content-type = "application/json"`
- Xml：`content-type = "application/xml"`
- Form：`content-type = "application/x-www-form-urlencoded"` / `multipart/form-data`（根据消息内容自动切换）
- Body：自定义序列化和 `content-type`

**数据接收**
- `XmlCast<T>`：接收 Xml 格式数据并自动反序列化为 T 类型
- `JsonCast<T>`：接收 JSON 格式数据并自动反序列化为 T 类型（需 IJsonHelper 支持，可用 Inkslab.Json 包）
- `String`：接收任意格式结果

**认证刷新**
- `When`：设置认证刷新条件
- `ThenAsync`：请求异常时刷新认证（每个设置最多执行一次）

**数据验证**
- `DataVerify`：数据验证（返回 true 代表数据符合预期）
- `Fail`：指定失败结果或抛出异常
- `Success`：成功时返回的数据

**其它**
- `XmlCatch<T>`：捕获 XmlException 并返回 T 结果，不抛异常
- `JsonCatch<T>`：捕获 JsonException 并返回 T 结果，不抛异常
- `UseEncoding`：数据编码格式，默认 UTF8