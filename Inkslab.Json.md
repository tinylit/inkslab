
![Inkslab](inkslab.jpg 'Logo')

## "Inkslab.Json"是什么？

**Inkslab.Json** 是一个高性能、易扩展的实体 JSON 序列化与反序列化工具，支持命名规范转换、缩进格式化、属性忽略等特性，适用于多种 .NET 应用场景。

---

## 🚀 快速入门

### 1. 序列化
```csharp
string json = JsonHelper.ToJson(new { Id = Guid.NewGuid(), Timestamp = DateTime.Now });
```

### 2. 反序列化
```csharp
public class A
{
    [Ignore] // 不序列化这个属性
    public int A1 { get; set; } = 100;
    public int A2 { get; set; }
    public string A3 { get; set; } = string.Empty;
    public DateTime A4 { get; set; }
}

string json = "{\"A2\":100,\"A3\":\"A3\",\"A4\":\"2022-12-03 14:17:55.7425309+08:00\"}";
A a = JsonHelper.Json<A>(json);
```

---

## 🏗️ 接口契约


```csharp
public interface IJsonHelper
{
  string ToJson<T>(T jsonObj, NamingType namingType = NamingType.Normal, bool indented = false);
  T Json<T>(string json, NamingType namingType = NamingType.Normal);
  T Json<T>(string json, T anonymousTypeObject, NamingType namingType = NamingType.Normal);
}
```

---

## 🔌 自定义扩展

### 1. 实现接口契约
```csharp
public class CustomJsonHelper : IJsonHelper
{
  public string ToJson<T>(T jsonObj, NamingType namingType = NamingType.Normal, bool indented = false)
  {
    // TODO: 序列化逻辑
  }
  public T Json<T>(string json, NamingType namingType = NamingType.Normal)
  {
    // TODO: 反序列化逻辑
  }
}
```

### 2. 注入实现
```csharp
SingletonPools.TryAdd<IJsonHelper, CustomJsonHelper>();
```

### 3. 正常使用



---

## 📖 说明

框架基于 [Newtonsoft.Json](https://www.nuget.org/packages/Newtonsoft.Json) 封装，支持多种命名规范、属性忽略、缩进格式化等高级特性。