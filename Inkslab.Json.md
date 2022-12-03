![Inkslab](inkslab.jpg 'Logo')

### "Inkslab.Json"是什么？

Inkslab.Json 是实体JSON序列化和反序列化工具。

#### 使用方式：

* 序列化。

  ```c#
  string json = JsonHelper.ToJson(new { Id = Guid.NewGuid(), Timestamp = DateTime.Now });
  ```

* 反序列化。

  ```c#
    /// <summary>
    /// 序列化类型。
    /// </summary>
    public class A
    {
        /// <summary>
        /// 不序列化这个属性。
        /// </summary>
        [Ignore]
        public int A1 { get; set; } = 100;

        /// <summary>
        /// <see cref="A2"/>
        /// </summary>
        public int A2 { get; set; }

        /// <summary>
        /// <see cref="A3"/>
        /// </summary>
        public string A3 { get; set; } = string.Empty;

        /// <summary>
        /// <see cref="A4"/>
        /// </summary>
        public DateTime A4 { get; set; }
    }

    string json = "{\"A2\":100,\"A3\":\"A3\",\"A4\":\"2022-12-03 14:17:55.7425309+08:00\"}"; // JSON 字符串。
    A a = JsonHelper.Json<A>(json); // 转换实体。
  ```

#### 接口契约：

```c#
/// <summary>
/// JSON序列化。
/// </summary>
[Ignore] // 不自动注入，详见"Inkslab.DI"文档。
public interface IJsonHelper
{
    /// <summary> Json序列化。 </summary>
    /// <typeparam name="T">数据类型。</typeparam>
    /// <param name="jsonObj">对象。</param>
    /// <param name="namingType">命名规则。</param>
    /// <param name="indented">是否缩进。</param>
    /// <returns></returns>
    string ToJson<T>(T jsonObj, NamingType namingType = NamingType.Normal, bool indented = false);

    /// <summary> Json反序列化。 </summary>
    /// <typeparam name="T">结果类型。</typeparam>
    /// <param name="json">JSON字符串。</param>
    /// <param name="namingType">命名规则。</param>
    /// <returns></returns>
    T Json<T>(string json, NamingType namingType = NamingType.Normal);

    /// <summary> 匿名对象反序列化。 </summary>
    /// <typeparam name="T">结果类型。</typeparam>
    /// <param name="json">JSON字符串。</param>
    /// <param name="anonymousTypeObject">匿名对象。</param>
    /// <param name="namingType">命名规则。</param>
    /// <returns></returns>
    T Json<T>(string json, T anonymousTypeObject, NamingType namingType = NamingType.Normal);
}
```
### 自定义。
 * 实现接口契约：
```c#
/// <summary>
/// 自定义序列化和反序列化助手。
/// </summary>
public class CustomJsonHelper : IJsonHelper
{
    /// <summary> Json序列化。 </summary>
    /// <typeparam name="T">数据类型。</typeparam>
    /// <param name="jsonObj">对象。</param>
    /// <param name="namingType">命名规则。</param>
    /// <param name="indented">是否缩进。</param>
    /// <returns></returns>
    public string ToJson<T>(T jsonObj, NamingType namingType = NamingType.Normal, bool indented = false){
        //TODO: 序列化逻辑。
    }

    /// <summary> Json反序列化。 </summary>
    /// <typeparam name="T">结果类型。</typeparam>
    /// <param name="json">JSON字符串。</param>
    /// <param name="namingType">命名规则。</param>
    /// <returns></returns>
    public T Json<T>(string json, NamingType namingType = NamingType.Normal){
        //TODO: 反序列化逻辑。
    }
}
```
 * 注入实现。
```c#
SingletonPools.TryAdd<IJsonHelper, CustomJsonHelper>();
```
 * 正常使用。


##### 说明：
框架基于 [Newtonsoft.Json](https://www.nuget.org/packages/Newtonsoft.Json) 封装。