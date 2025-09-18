
![Inkslab](inkslab.jpg 'Logo')

## "Inkslab.Config"是什么？

**Inkslab.Config** 是一个高性能、易扩展的项目配置文件读取器，支持强类型读取、配置热更新、环境适配等特性，适用于多种 .NET 应用场景。

---

## 🚀 快速入门

### 1. 强类型配置读取
```csharp
var value = "config-key".Config<string>(); // 返回结果字符串
```

### 2. 获取对象
```csharp
var value = "config-key".Config<Options>(); // 返回 Options 配置
```

---

## 🏗️ 接口契约


```csharp
public interface IConfigHelper
{
  /// <summary>
  /// 配置文件读取。
  /// </summary>
  /// <typeparam name="T">读取数据类型。</typeparam>
  /// <param name="key">键。</param>
  /// <param name="defaultValue">默认值。</param>
  /// <returns>如果找到 key 对应的值，则返回键值；否则，返回默认值。</returns>
  T Get<T>(string key, T defaultValue = default(T));
}
```

---


## 🔌 自定义扩展

### 1. 实现接口契约
```csharp
public class CustomConfigHelper : IConfigHelper
{
  public T Get<T>(string key, T defaultValue = default)
  {
    // TODO: 获取配置逻辑
  }
}
```

### 2. 注入实现
```csharp
SingletonPools.TryAdd<IConfigHelper, CustomConfigHelper>();
```

### 3. 正常使用


---

## 📖 说明

**.NET Framework**
- 支持 Web、Form、Service 等运行环境，默认使用 Web
- 层级分隔符：`/`
- 默认读取 `appStrings` 下的键值
- 读取数据库连接：`connectionStrings/key`
- 读取数据库连接字符串：`connectionStrings/key/connectionString`
- 读取自定义 `ConfigurationSectionGroup` 时需提供准确类型，否则返回默认值

**.NET Standard**
- 层级分隔符：`:`
- 读取规则与 `Microsoft.Extensions.Configuration` 保持一致

---