![Inkslab](inkslab.jpg 'Logo')

### "Inkslab.Net"是什么？

Inkslab.Net 是HTTP/HTTPS请求工具，涵盖了刷新认证、重试、序列化、反序列化、数据验证与重发，文件上传下载等功能。

#### 使用方式：

* 获得请求能力。

  - 注入 **IRequestFactory** 接口。
  - 使用 **IRequestFactory.CreateRequestable("{api}")** 获得请求能力;

* 根据业务需要，按照提示即可下发请求指令。

  - 普通请求。

    ```c#
    string result = await requestFactory.CreateRequestable("api")
        .AppendQueryString("?{params}")
        .GetAsync();
    ```

  - 认证信息刷新请求。

    ```c#
    string result = await requestFactory.CreateRequestable("api")
        .AppendQueryString("?{params}")
        .AssignHeader("Authorization", "Bearer 3506555d8a256b82211a62305b6dx317")
        .When(status => status == HttpStatusCode.Unauthorized)
        .ThenAsync((requestable, e) => { // 仅会执行一次，与其它重试机制无关。
            //TODO:刷新认证信息。
            return Task.CompletedTask;
        })
        .GetAsync();
    ```

  - 序列化、反序列化、验证与重发。

    + 结果实体。

      ```c#
      public class ServResult
      {
          /// <summary>
          /// 状态码。
          /// </summary>
          [XmlElement("code")]
          public int Code { get; set; }
      
          private bool? success = null;
      
          /// <summary>
          /// 是否成功。
          /// </summary>
          [XmlIgnore]
          public bool Success{
          {
              get => success ?? Code == StatusCodes.OK;
              set => success = new bool?(value);
          }
      
          /// <summary>
          /// 错误信息。
          /// </summary>
          [XmlElement("msg")]
          public string Msg { get; set; }
      
          /// <summary>
          /// Utc。
          /// </summary>
          [XmlElement("timestamp")]
          public DateTime Timestamp { get; set; }
      }

      public class ServResult<TData> : ServResult
      {
          /// <summary>
          /// 数据结果。
          /// </summary>
          [XmlElement("data")]
          public TData Data { get; set; }
      }
      ```

    + 序列化。

      ```c#
      string result = await requestFactory.CreateRequestable("api")
          .Json(new {
                    Date = DateTime.Now,
                    TemperatureC = 1,
                    Summary = 50
                })
          .PostAsync();
      ```

    + 反序列化。

      ```c#
      ServResult result = await requestFactory.CreateRequestable("api")
          .Json(new
                {
                    Date = DateTime.Now,
                    TemperatureC = 1,
                    Summary = 50
                })
          .JsonCast<ServResult>()
          .PostAsync();
      ```

    + 验证。

      ```c#
      int result = await requestFactory.CreateRequestable("api")
          .Json(new
                {
                    Date = DateTime.Now,
                    TemperatureC = 1,
                    Summary = 50
                })
          .JsonCast<ServResult<int>>()
          .DataVerify(r => r.Success) // 数据验证。
          .Success(r => r.Data) // 成功返回的数据。
          .Fail(r => new BusiException(r.Msg, r.Code)) // 验证失败抛出异常，也可以返回失败结果。
          .PostAsync();
      ```

##### 说明：

* 基础请求配置。
  - `AssignHeader`设置求取头。
  - `AppendQueryString`添加请求参数。
    - 正常情况：多次添加相同的参数名称，不会被覆盖（数组场景）。
    - 刷新认证：`TryThen`函数中，会覆盖相同名称的参数。
* 请求方式。
  - 显示支持：GET、DELETE、POST、PUT、HEAD、PATCH。
  - 隐式支持：使用 `SendAsync` 方法，第一个参数为请求方式。
  - 流处理：`DownloadAsync` 流下载。

* 数据传输。
  - Json：`content-type = "application/json"`。
  - Xml：`content-type = "application/xml"`。
  - Form：`content-type = "application/x-www-form-urlencoded"` / `multipart/form-data` ( *根据消息内容是否有[`FileInfo`]或[`IEnumerable<FileInfo>`]* 自动切换)。
  - Body：自己序列化数据和指定`content-type`。
* 数据接收。
  - XmlCast&lt;T&gt;：接收Xml格式数据，并自动反序列化为`T`类型。
  - JsonCast&lt;T&gt;：接收JSON格式数据，并自动反序列化为`T`类型，需要提供`IJsonHelper`接口支持，可以使用`Inkslab.Json`包。
  - String：接收任意格式结果。
* 刷新认证。
  - When：需要刷新认证的条件。
  - ThenAsync：请求异常刷新认证(每个设置，最多执行一次)。
* 数据验证。
  - DataVerify：数据验证（返回`true`代表数据符合预期）。
  - Fail：指定失败结果或失败抛出的异常。
  - Success：支持成功范围的结果。
* 其它。
  - XmlCatch&lt;T&gt;：捕获`XmlException`异常，并返回`T`结果，不抛异常。
  - JsonCatch&lt;T&gt;：捕获`JsonException`异常，并返回`T`结果，不抛异常。
  - UseEncoding：数据编码格式，默认：`UTF8`。