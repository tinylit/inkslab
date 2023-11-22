![Inkslab](inkslab.jpg 'Logo')

![GitHub](https://img.shields.io/github/license/tinylit/inkslab.svg)
![language](https://img.shields.io/github/languages/top/tinylit/inkslab.svg)
![codeSize](https://img.shields.io/github/languages/code-size/tinylit/inkslab.svg)
![AppVeyor](https://img.shields.io/appveyor/build/tinylit/inkslab)
![AppVeyor tests](https://img.shields.io/appveyor/tests/tinylit/inkslab)
[![GitHub issues](https://img.shields.io/github/issues-raw/tinylit/inkslab)](../../issues)

### “Inkslab”是什么？

Inkslab 是一套简单、高效的轻量级框架（涵盖了对象映射、配置读取、Xml/Json序列化和反序列化、以及自动/定制化依赖注入）。

### 如何安装？
First, [install NuGet](http://docs.nuget.org/docs/start-here/installing-nuget). Then, install [Inkslab](https://www.nuget.org/packages/inkslab/) from the package manager console: 

```
PM> Install-Package Inkslab
```
### 引包即用？

* 引包即用是指，安装 `NuGet` 包后，自动注入配置信息。
* 在启动方法中添加如下代码即可：
``` csharp
using (var startup = new XStartup())
{
    startup.DoStartup();
}
```

NuGet 包
--------

| Package | NuGet | Downloads | Jane Says <kbd>Markdown</kbd> |
| ------- | ----- | --------- | --------- |
| Inkslab | [![Inkslab](https://img.shields.io/nuget/v/inkslab.svg)](https://www.nuget.org/packages/inkslab/) | ![Nuget](https://img.shields.io/nuget/dt/Inkslab) | Core universal design. |
| Inkslab.Map | [![Inkslab.Map](https://img.shields.io/nuget/v/inkslab.map.svg)](https://www.nuget.org/packages/inkslab.map/) | ![Nuget](https://img.shields.io/nuget/dt/Inkslab.Map) | [Type conversion, cloning, mapping.](./Inkslab.Map.md) |
| Inkslab.Config | [![Inkslab.Config](https://img.shields.io/nuget/v/inkslab.config.svg)](https://www.nuget.org/packages/inkslab.config/) | ![Nuget](https://img.shields.io/nuget/dt/Inkslab.Config) | [Read configuration file.](./Inkslab.Config.md) |
| Inkslab.Json | [![Inkslab.Json](https://img.shields.io/nuget/v/inkslab.json.svg)](https://www.nuget.org/packages/inkslab.json/) | ![Nuget](https://img.shields.io/nuget/dt/Inkslab.Json) | [JSON read and write processing.](./Inkslab.Json.md) |
| Inkslab.Net | [![Inkslab.Net](https://img.shields.io/nuget/v/inkslab.net.svg)](https://www.nuget.org/packages/inkslab.net/) | ![Nuget](https://img.shields.io/nuget/dt/Inkslab.Net) | [Request component of HTTP/HTTPS.](./Inkslab.Net.md) |

### 内置功能。
  * Xml 系列化和反序列化助手。
    - 使用方式。
    ```c#
    /// <summary>
    /// 序列化实体。
    /// </summary>
    [XmlRoot("xml")]
    public class XmlA
    {
        /// <summary>
        /// 忽略字段。
        /// </summary>
        [Ignore] //? 忽略字段。
        public int A1 { get; set; } = 100;

        /// <summary>
        /// 生成 <![CDATA[{value}]]>
        /// </summary>
        public CData A2 { get; set; }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public string A3 { get; set; }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public DateTime A4 { get; set; }
    }
    /// <summary>
    /// 反序列化实体。
    /// </summary>
    [XmlRoot("xml")]
    public class XmlB
    {
        /// <summary>
        /// 忽略字段。
        /// </summary>
        [Ignore] //? 忽略字段。
        public int A1 { get; set; } = 100;
        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public string A2 { get; set; }
        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public string A3 { get; set; }
        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public DateTime A4 { get; set; }
    }
    ```
     * 序列化。
     ```c#
      XmlA x = new XmlA
      {
          A1 = 200,
          A2 = "测试CData节点",
          A3 = "普通节点",
          A4 = DateTime.Now
      };

      string xml = XmlHelper.XmlSerialize(x);
     ```
    * 反序列化。
    ```c#
     string xml ="<?xml version=\"1.0\" encoding=\"utf-8\"?><xml xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\"><A2><![CDATA[测试CData节点]]></A2><A3>普通节点</A3><A4>2022-12-03T14:53:29.1218173+08:00</A4></xml>";

     XmlB y = XmlHelper.XmlDeserialize<XmlB>(xml);
    ```
  * 自定义程序集查找器。
    - 自定义【IDirectory】。
        ```c#
            public class CustomDirectory : IDirectory
            {
                public string[] GetFiles(string path, string searchPattern) => Directory.GetFiles(path, searchPattern);
            }
        ```
    - 注入到单列池中。
        ```c#
            SingletonPools.TryAdd<IDirectory, CustomDirectory>();
        ```

  * 配置文件助手。
    - 使用方式：

      * 普通类型。

        ```c#
        var value = "config-key".Config<string>(); // 返回结果字符串。
        ```  

      * 监听类型。

        ```c#  
        var options = "config-key".Options<string>(); // 返回IOptions&lt;string&gt;配置。
        var value = options.Value; // 配置文件发生变化时，“options.Value”会被更新。
        ```
    - 默认实现，
      ```
      PM> Install-Package Inkslab.Config
      ```
    - 如需自定义，请参考 [Inkslab.Config](Inkslab.Config.md 'Logo') 文稿。
  * Json 序列化和反序列化助手。
    - 使用方式：

      * 序列化。

        ```c#
        string json = JsonHelper.ToJson(new { Id = KeyGen.Id(), Timestamp = DateTime.Now });
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
          A a = JsonHelper.Json<A>(json); // 转换实体、匿名类型请参考重载方法。
        ```
    - 默认实现，
      ```
      PM> Install-Package Inkslab.Json
      ```
    - 如需自定义，请参考 [Inkslab.Json](Inkslab.Json.md 'Logo') 文稿。

  * 对象映射。
    - 使用方式。
    ```c#
     FooDto fooDto = Mapper.Map<FooDto>(foo);
     BarDto barDto = Mapper.Map<BarDto>(bar);
    ```
    - 默认实现，
       ```
       PM> Install-Package Inkslab.Map
       ```
    - 如需自定义或了解**高级语法**，请参考 [Inkslab.Map](Inkslab.Map.md 'Logo') 文稿。
  * 主键生成器。
    - 使用方式。
    ```c#
    long id = KeyGen.Id();
    ```
    - 默认实现：雪花算法。 
    - 设置机房和机号。
    ```c#
    SingletonPools.TryAdd(new KeyOptions(workerId, datacenterId));
    ```
    - 自定义主键生成器。
      - 实现接口。
      ```c#
      /// <summary>
      /// KeyGen 创建器。
      /// </summary>
      public interface IKeyGenFactory
      {
          /// <summary>
          /// 创建。
          /// </summary>
          /// <returns></returns>
          IKeyGen Create();
      }
      ```
      - 注入实现。
      ```c#
      SingletonPools.TryAdd<IMapper, CustomMapper>();
      ```
      - 正常使用。

### 单例。

* 作为单例基类。

  ```c#
  public class ASingleton : Singleton<ASingleton> {
      private ASingleton(){ }
  }
  
  ASingleton singleton = ASingleton.Instance;
  ```

* 作为单例使用。

  ```c#
  public class BSingleton {   
  }
  
  BSingleton singleton = Singleton<BSingleton>.Instance
  ```

* 绝对单例。

  ```c#
  public class CSingleton : Singleton<CSingleton> {
      private CSingleton(){ }
  }
  
  CSingleton singleton1 = CSingleton.Instance;
  CSingleton singleton2 = Singleton<CSingleton>.Instance; // 与“singleton1”是同一实例。
  ```

### 单例池。

* TryAdd：添加单例实现。

* Singleton：获取单例。

* 单例实现：

  - 单例实现（一）。

    - 添加默认支持的单例实现。

    ```c#
    SingletonPools.TryAdd<A,B>(); //=> true.
    ```

    - 在未使用A的实现前，可以刷新单例实现支持。

    ```c#
    SingletonPools.TryAdd<A,C>(); //=> true;
    SingletonPools.TryAdd<A>(new C()); //=> true;
    ```

  - 单例实现（二）。

    - 添加实例或工厂支持的单例实现。

    ```c#
    SingletonPools.TryAdd<A>(new B()); //=> true.
    ```

    - 在未使用A的实现前，可以被实例或工厂刷新单例实现支持，默认支持方式不被生效。

    ```c#
    SingletonPools.TryAdd<A,C>(); //=> false;
    SingletonPools.TryAdd<A>(new C()); //=> true;
    ```

* 单例使用：

  - 单例使用（一）。

    ```c#
    A a = SingletonPools.Singleton<A>();
    ```

    未提前注入单例实现，会直接抛出`NotImplementedException`异常。

  - 单例使用（二）。

    ```c#
    A a = SingletonPools.Singleton<A,B>();
    ```

    未提前注入单例实现，会尝试创建`B`实例。

- 说明：

  - TryAdd&lt;T&gt;：使用实例时，使用【公共/非公共】无参构造函数创建实例。

  - TryAdd&lt;T1,T2&gt;：使用实例时，尽量使用参数更多且被支持的公共构造函数创建实例。

    ```c#
    public class A {
    }
    public class B {
        private readonly A a;
        public B() : this(new A()){ }
        Public B(A a){ this.a = a ?? throw new ArgumentNullException(nameof(a)); }
    }
    ```

    使用单例时，未注入A的单例实现，使用无参构造函数生成实例。

    使用单例时，已注入A的单例实现，使用参数`A`的造函数生成实例。

### 命名规范。

* 命名方式。

  ```c#
  /// <summary> 命名规范。 </summary>
  public enum NamingType
  {
      /// <summary> 默认命名(原样/业务自定义)。 </summary>
      Normal = 0,
  
      /// <summary> 驼峰命名,如：userName。 </summary>
      CamelCase = 1,
  
      /// <summary> url命名,如：user_name，注：反序列化时也需要指明。 </summary>
      UrlCase = 2,
  
      /// <summary> 帕斯卡命名,如：UserName。 </summary>
      PascalCase = 3
  }
  ```

### 命名转换（三种命名可以相互转换）。

* 指定命名方式。

  ```c#
  string named = "name".ToNamingCase(NamingType.CamelCase);
  ```

* 特定命名方式。

  - 帕斯卡命名（又称大驼峰）。

    ```c#
    string named = "user_name".ToPascalCase(); // UserName
    ```

  - 驼峰命名。

    ```c#
    string named = "user_name".ToCamelCase(); // userName
    ```

  - 蛇形命名。

    ```c#
    string named = "user_name".ToSnakeCase(); // user_name
    ```
  - 短横线命名
    ```c#
    string named = "user_name".ToKebabCase(); // user-name
    ```

### 字符串语法糖。

```c#
string value = "${a + b}".PropSugar(new { A = 1, B = 2 }); //=> value = "3"。
```

* 语法说明：

  - 空运算符：A ? B、A ?? B

    当A为`null`时，返回B，否则返回A。

  - 合并运算符：A + B

    当A和B可以参与运算时，返回运算结果。否则转成字符串拼接。

  - 试空合并运算符：A ?+ B

    当A为`null`时，返回`null`，否则按照【合并运算符】计算A+B的结果。
  
  -  属性后代：A.B 或 A.B.C ...

标星历程图。

[![Stargazers over time](https://starchart.cc/tinylit/inkslab.svg)](https://starchart.cc/tinylit/inkslab)