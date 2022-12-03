![Inkslab](inkslab.jpg 'Logo')

### "Inkslab.Config"是什么？

    Inkslab.Config 是项目配置文件读取器。

#### 使用方式：

* 普通类型。

  ```c#
  var value = "config-key".Config<string>(); // 返回结果字符串。
  ```  

* 监听类型。

  ```c#  
  var options = "config-key".Options<string>(); // 返回IOptions&lt;string&gt;配置。
  var value = options.Value; // 配置文件发生变化时，“options.Value”会被更新。
  ```

#### 接口契约：

```c#
    /// <summary>
    /// 配置文件帮助类。
    /// </summary>
    public interface IConfigHelper
    {
        /// <summary>
        /// 配置文件读取。
        /// </summary>
        /// <typeparam name="T">读取数据类型。</typeparam>
        /// <param name="key">健。</param>
        /// <param name="defaultValue">默认值。</param>
        /// <returns>如果找到 <paramref name="key"/> 对应的值，则返回键值；否则，返回默认值 <paramref name="defaultValue"/> 。</returns>
        T Get<T>(string key, T defaultValue = default(T));
    }
```

#### 自定义。
 * 实现接口契约：
```c#
    /// <summary>
    /// 自定义读取配置助手。
    /// </summary>
    public class CustomConfigHelper : IConfigHelper
    {
        /// <summary>
        /// 配置文件读取。
        /// </summary>
        /// <typeparam name="T">读取数据类型。</typeparam>
        /// <param name="key">健。</param>
        /// <param name="defaultValue">默认值。</param>
        /// <returns>如果找到 <paramref name="key"/> 对应的值，则返回键值；否则，返回默认值 <paramref name="defaultValue"/> 。</returns>
        public T Get<T>(string key, T defaultValue = default){
            //TODO: 获取配置逻辑。
        }
    }
```
 * 注入实现。
```c#
     SingletonPools.TryAdd<IConfigHelper, CustomConfigHelper>();
```
 * 正常使用。

##### 说明：

* .NET Framework。
  - 运行环境包括：Web、Form、Service。
  - 运行环境默认使用`Web`。
  - 层级分隔符：`/`。
  - 默认读取`appStrings`下的键值。
  - 读取数据库连接:`connectionStrings/key`。
  - 读取数据库连接的连接字符：`connectionStrings/key/connectionString`。
  - 读取自定义`ConfigurationSectionGroup`请提供准确的类型，否则强转失败，返回默认值。
* .NET Standard：
  - 层级分隔符：`:`。
  - 读取规则与`Microsoft.Extensions.Configuration`保持一致。