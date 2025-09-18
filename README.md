![Inkslab](inkslab.jpg 'Logo')

![GitHub](https://img.shields.io/github/license/tinylit/inkslab.svg)
![language](https://img.shields.io/github/languages/top/tinylit/inkslab.svg)
![codeSize](https://img.shields.io/github/languages/code-size/tinylit/inkslab.svg)
![AppVeyor](https://img.shields.io/appveyor/build/tinylit/inkslab)
![AppVeyor tests](https://img.shields.io/appveyor/tests/tinylit/inkslab)
[![GitHub issues](https://img.shields.io/github/issues-raw/tinylit/inkslab)](../../issues)

## "Inkslab"是什么？

Inkslab 是一套简单、高效的轻量级框架，专注于现代化 C# 开发体验。框架采用模块化设计，提供统一的 API 接口，涵盖了对象映射、配置读取、序列化、依赖注入等核心功能。

### 🎯 核心特性

- **统一API设计** - 所有模块遵循一致的设计原则和API风格
- **语法糖扩展** - 基于扩展方法的语法糖，提升开发效率
- **模块化架构** - 按需引用，最小化依赖
- **自动启动机制** - [`XStartup`](src/Inkslab/XStartup.cs) 自动发现和注册组件
- **多框架支持** - 支持 .NET Framework 4.6.1+、.NET Standard 2.1、.NET 6.0+

## 🚀 快速入门

### 安装

```bash
PM> Install-Package Inkslab
```

### 基础配置

```csharp
using Inkslab;

// 框架自动启动（推荐）
using (var startup = new XStartup())
{
    startup.DoStartup();
}
```

## 📦 NuGet 包

| Package | NuGet | Downloads | 描述 |
| ------- | ----- | --------- | ---- |
| Inkslab | [![Inkslab](https://img.shields.io/nuget/v/inkslab.svg)](https://www.nuget.org/packages/inkslab/) | ![Nuget](https://img.shields.io/nuget/dt/Inkslab) | 核心框架 |
| Inkslab.Config | [![Inkslab.Config](https://img.shields.io/nuget/v/inkslab.config.svg)](https://www.nuget.org/packages/inkslab.config/) | ![Nuget](https://img.shields.io/nuget/dt/Inkslab.Config) | [配置文件读取](./Inkslab.Config.md) |
| Inkslab.Json | [![Inkslab.Json](https://img.shields.io/nuget/v/inkslab.json.svg)](https://www.nuget.org/packages/inkslab.json/) | ![Nuget](https://img.shields.io/nuget/dt/Inkslab.Json) | [JSON 序列化](./Inkslab.Json.md) |
| Inkslab.Map | [![Inkslab.Map](https://img.shields.io/nuget/v/inkslab.map.svg)](https://www.nuget.org/packages/inkslab.map/) | ![Nuget](https://img.shields.io/nuget/dt/Inkslab.Map) | [对象映射](./Inkslab.Map.md) |
| Inkslab.DI | [![Inkslab.DI](https://img.shields.io/nuget/v/inkslab.di.svg)](https://www.nuget.org/packages/inkslab.di/) | ![Nuget](https://img.shields.io/nuget/dt/Inkslab.DI) | 依赖注入扩展 |
| Inkslab.Net | [![Inkslab.Net](https://img.shields.io/nuget/v/inkslab.net.svg)](https://www.nuget.org/packages/inkslab.net/) | ![Nuget](https://img.shields.io/nuget/dt/Inkslab.Net) | [HTTP 请求组件](./Inkslab.Net.md) |

## 🏗️ 核心技术架构

### 1. 扩展方法体系

Inkslab 基于 C# 扩展方法构建了一套完整的语法糖体系，位于 [`src/Inkslab/Extentions/`](src/Inkslab/Extentions/) 目录：

#### 字符串扩展 ([`StringExtensions`](src/Inkslab/Extentions/StringExtensions.cs))

```csharp
// 命名规范转换
string camelCase = "UserName".ToCamelCase();        // → "userName"
string snakeCase = "UserName".ToSnakeCase();        // → "user_name"  
string pascalCase = "user_name".ToPascalCase();     // → "UserName"
string kebabCase = "UserName".ToKebabCase();        // → "user-name"

// 统一命名转换API
string result = "UserName".ToNamingCase(NamingType.SnakeCase); // → "user_name"

// 配置读取语法糖
string dbConnection = "ConnectionStrings:Default".Config<string>();
var appSettings = "AppSettings".Config<AppConfig>();
```

#### 集合扩展 ([`IEnumerableExtentions`](src/Inkslab/Extentions/IEnumerableExtentions.cs))

```csharp
/// <summary>
/// 内容对齐。
/// </summary>
public void AlignTest()
{
    var array1 = new List<int> { 1, 2, 3, 4, 5, 6, 7 };
    var array2 = new List<int> { 4, 5, 1, 2, 3, 6, 7 };

    //? 将 array2 按照 array1 的集合排序。
    var array3 = array2
        .AlignOverall(array1)
        .ToList();

    //? 比较两个集合相同下标位，值是否相同。
    array3.ZipEach(array1, Assert.Equal);
}

/// <summary>
/// 内容遍历。
/// </summary>
public void JoinEachTest()
{
    var array1 = new List<int> { 1, 2, 3, 4, 5, 6, 7 };
    var array2 = new List<DistinctA>();

    var r = new Random();

    for (int i = 0, len = 50; i < len; i++)
    {
        array2.Add(new DistinctA
        {
            Id = r.Next(len),
            Name = i.ToString(),
            Date = DateTime.Now
        });
    }
    //? 与 Join 逻辑相同，但不需要返回新的集合。
    array1.JoinEach(array2, x => x, y => y.Id, (x, y) =>
    {
        Assert.Equal(x, y.Id);
    });
}
```

#### 加密扩展 ([`CryptoExtensions`](src/Inkslab/Extentions/CryptoExtensions.cs))

```csharp
// 常用哈希算法
string md5 = "password".Md5();
string encrypt = "data".Encrypt("Test@*$!", CryptoKind.DES); // 加密
string decrypt = "data".Decrypt("Test@*$!", CryptoKind.DES); // 解密
```

#### 日期时间扩展 ([`DateTimeExtensions`](src/Inkslab/Extentions/DateTimeExtensions.cs))
##### 自动根据提供时间是**Utc** / **Local** 自动处理一周的第一天和最后一天。
* **Utc**：周日为一周的第一天；周六为一周的最后一天。
* **Local**：周一为一周的第一天；周日为一周的最后一天。

### 2. 序列化框架

#### JSON 序列化 (基于 Newtonsoft.Json)

核心实现：[`DefaultJsonHelper`](src/Inkslab.Json/DefaultJsonHelper.cs)

```csharp
// 基础用法
string json = JsonHelper.ToJson(obj);
T result = JsonHelper.Json<T>(json);

// 命名规范支持
string json = JsonHelper.ToJson(obj, NamingType.CamelCase);
var result = JsonHelper.Json<User>(json, NamingType.SnakeCase);

// 格式化输出
string prettyJson = JsonHelper.ToJson(obj, indented: true);
```

#### 自定义JSON序列化器

```csharp
public class CustomJsonHelper : IJsonHelper
{
    public string ToJson<T>(T jsonObj, NamingType namingType = NamingType.Normal, bool indented = false)
    {
        // 自定义序列化逻辑
        var settings = new JsonSerializerSettings();
        
        // 根据命名规范配置
        switch (namingType)
        {
            case NamingType.CamelCase:
                settings.ContractResolver = new CamelCasePropertyNamesContractResolver();
                break;
            case NamingType.SnakeCase:
                settings.ContractResolver = new DefaultContractResolver 
                { 
                    NamingStrategy = new SnakeCaseNamingStrategy() 
                };
                break;
        }
        
        return JsonConvert.SerializeObject(jsonObj, indented ? Formatting.Indented : Formatting.None, settings);
    }
    
    public T Json<T>(string json, NamingType namingType = NamingType.Normal)
    {
        // 自定义反序列化逻辑
    }
}

// 注册自定义实现
SingletonPools.TryAdd<IJsonHelper, CustomJsonHelper>();
```

### 3. 语法糖适配器 ([`AdapterSugar`](src/Inkslab/Sugars/AdapterSugar.cs))

Inkslab 框架通过 **AdapterSugar<T>** 实现了自动化的语法糖适配机制，极大提升了扩展性和运行时性能。其核心流程如下：

#### **1. 类型与成员信息初始化**

- 自动获取泛型参数 **T** 及正则相关类型（如 **Match**、**Group**、**Capture** 等）的反射信息。
- 构建表达式树所需的参数表达式。

#### **2. 方法发现与遍历**

- 自动发现 **T** 类型下所有公开实例方法。
- 仅处理参数数量大于0且返回类型为 **string** 的方法。

#### **3. 参数分析与表达式构建**

- 支持参数类型：**Match**、**GroupCollection**、**Group**、**CaptureCollection**、**Capture**、**string**、**bool**。
- 根据参数类型，自动生成变量声明、赋值、条件判断及参数列表。
- 不支持类型将抛出异常，确保类型安全。

#### **4. 条件与特性处理**

- 支持 **MatchAttribute** 指定正则分组名。
- 支持 **MismatchAttribute** 补充不匹配条件。
- 汇总所有条件表达式，生成最终的匹配条件。

#### **5. 表达式树编译**

- 条件表达式编译为 **Func<Match, bool>**，用于判断当前正则匹配是否适用该方法。
- 方法调用表达式编译为 **Func<T, Match, string>**，用于执行实际转换逻辑。

#### **6. 适配器缓存**

- 每个方法生成一个适配器（包含条件判断与转换逻辑），自动缓存到静态列表，供后续格式化调用时高效匹配和执行。

```csharp
public abstract class AdapterSugar<T> : ISugar where T : AdapterSugar<T>, ISugar
{
    // 适配器模式实现
    private class Adapter
    {
        public Func<Match, bool> CanConvert { get; set; }
        public Func<T, Match, string> Convert { get; set; }
    }
    
    // 撤销操作
    public bool Undo { get; private set; }
    
    // 格式化方法
    public string Format(Match match);
}
```

#### 自定义语法糖示例
```csharp
  public class StringSugar : AdapterSugar<StringSugar>
  {
      private readonly object _source;
      private readonly DefaultSettings _settings;
      private readonly SyntaxPool _syntaxPool;

      public StringSugar(object source, DefaultSettings settings, SyntaxPool syntaxPool)
      {
          _source = source ?? throw new ArgumentNullException(nameof(source));
          _settings = settings ?? throw new ArgumentNullException(nameof(settings));
          _syntaxPool = syntaxPool ?? throw new ArgumentNullException(nameof(syntaxPool));
      }

      [Mismatch("token")] //? 不匹配 token。
      public string Single(string name, string format) => _syntaxPool.GetValue(this, _source, _settings, name, format);

      [Mismatch("token")] //? 不匹配 token。
      public string Single(string name) => _syntaxPool.GetValue(this, _source, _settings, name);

      public string Combination(string pre, string token, string name, string format) => _syntaxPool.GetValue(this, _source, _settings, pre, token, name, format);

      public string Combination(string pre, string token, string name) => _syntaxPool.GetValue(this, _source, _settings, pre, token, name);
  }
```

### 4. 单例池管理 ([`SingletonPools`](src/Inkslab/))

```csharp
// 注册单例
SingletonPools.TryAdd<IService, ServiceImpl>();
SingletonPools.TryAdd(new ServiceInstance());

// 获取单例
var service = SingletonPools.Singleton<IService>();
```

## 💡 核心功能详解

### 配置管理

```csharp
// 强类型配置
public class DatabaseConfig
{
    public string ConnectionString { get; set; }
    public int Timeout { get; set; }
}

var dbConfig = "Database".Config<DatabaseConfig>();

// 配置监听（热更新）
var options = "Database".Options<DatabaseConfig>();
// options.Value 会随配置文件变化自动更新
```

### 对象映射

```csharp
// 基础映射
var dto = Mapper.Map<UserDto>(user);
```

### 主键生成

```csharp
// 雪花算法ID生成
long id = KeyGen.Id();
Key newKey = KeyGen.New();

// 自定义机房和机器号
SingletonPools.TryAdd(new KeyOptions(workerId: 1, datacenterId: 1));
```

### 命名规范转换

基于 [`NamingType`](src/Inkslab/NamingType.cs) 枚举的统一命名处理：

```csharp
public enum NamingType
{
    Normal = 0,      // 原样输出
    CamelCase = 1,   // 驼峰：userName
    SnakeCase = 2,   // 蛇形：user_name  
    PascalCase = 3,  // 帕斯卡：UserName
    KebabCase = 4    // 短横线：user-name
}

// 使用示例
string result = "UserName".ToNamingCase(NamingType.SnakeCase);
```

## 🎓 高级应用指南

### 1. 自定义启动器

实现 [`IStartup`](src/Inkslab/IStartup.cs) 接口创建自定义启动器：

```csharp
public class CustomStartup : IStartup
{
    public int Code => 999;      // 启动代码（用于排序）
    public int Weight => 1;      // 权重

    public void Startup()
    {
        // 自定义初始化逻辑
        SingletonPools.TryAdd<ICustomService, CustomServiceImpl>();
        
        // 注册语法糖
        RegisterCustomSyntaxSugar();
        
        // 配置全局设置
        ConfigureGlobalSettings();
    }
}
```

### 2. 扩展方法最佳实践

```csharp
#pragma warning disable IDE0130 // 命名空间与文件夹结构不匹配
namespace System // 扩展到系统命名空间，全局可用
#pragma warning restore IDE0130

{
    public static class CustomExtensions
    {
        /// <summary>
        /// 安全的字符串截取
        /// </summary>
        public static string SafeSubstring(this string source, int startIndex, int length)
        {
            if (string.IsNullOrEmpty(source) || startIndex >= source.Length)
                return string.Empty;
                
            return source.Substring(startIndex, Math.Min(length, source.Length - startIndex));
        }
        
        /// <summary>
        /// 条件执行扩展
        /// </summary>
        public static T If<T>(this T source, bool condition, Func<T, T> action)
        {
            return condition ? action(source) : source;
        }
    }
}
```

### 3. 高性能序列化配置

```csharp
public class HighPerformanceJsonHelper : IJsonHelper
{
    private static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
    {
        // 性能优化配置
        DateTimeZoneHandling = DateTimeZoneHandling.Utc,
        NullValueHandling = NullValueHandling.Ignore,
        DefaultValueHandling = DefaultValueHandling.Ignore,
        
        // 类型处理
        TypeNameHandling = TypeNameHandling.None,
        
        // 错误处理
        Error = (sender, args) => 
        {
            // 记录序列化错误但不中断处理
            args.ErrorContext.Handled = true;
        }
    };
    
    public string ToJson<T>(T jsonObj, NamingType namingType = NamingType.Normal, bool indented = false)
    {
        var settings = Settings.Clone();
        settings.ContractResolver = GetContractResolver(namingType);
        settings.Formatting = indented ? Formatting.Indented : Formatting.None;
        
        return JsonConvert.SerializeObject(jsonObj, settings);
    }
    
    private static IContractResolver GetContractResolver(NamingType namingType)
    {
        // 缓存 ContractResolver 实例以提升性能
        return namingType switch
        {
            NamingType.CamelCase => CachedResolvers.CamelCase,
            NamingType.SnakeCase => CachedResolvers.SnakeCase,
            NamingType.PascalCase => CachedResolvers.PascalCase,
            NamingType.KebabCase => CachedResolvers.KebabCase,
            _ => CachedResolvers.Default
        };
    }
    
    private static class CachedResolvers
    {
        public static readonly IContractResolver Default = new DefaultContractResolver();
        public static readonly IContractResolver CamelCase = new CamelCasePropertyNamesContractResolver();
        public static readonly IContractResolver SnakeCase = new DefaultContractResolver 
        { 
            NamingStrategy = new SnakeCaseNamingStrategy() 
        };
        // ... 其他缓存的解析器
    }
}
```

### 4. 模块化架构设计

```csharp
// 模块接口定义
public interface IModule
{
    string Name { get; }
    Version Version { get; }
    void Initialize();
    void Dispose();
}

// 模块管理器
public class ModuleManager
{
    private readonly List<IModule> _modules = new();
    
    public void LoadModule<T>() where T : IModule, new()
    {
        var module = new T();
        module.Initialize();
        _modules.Add(module);
    }
    
    public void LoadModules(Assembly assembly)
    {
        var moduleTypes = assembly.GetTypes()
            .Where(t => typeof(IModule).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);
            
        foreach (var type in moduleTypes)
        {
            var module = (IModule)Activator.CreateInstance(type);
            module.Initialize();
            _modules.Add(module);
        }
    }
}

// 在启动器中使用
public class ModularStartup : IStartup
{
    public int Code => 100;
    public int Weight => 1;
    
    public void Startup()
    {
        var moduleManager = new ModuleManager();
        
        // 加载核心模块
        moduleManager.LoadModule<ConfigModule>();
        moduleManager.LoadModule<JsonModule>();
        moduleManager.LoadModule<MappingModule>();
        
        // 自动发现并加载模块
        var assemblies = AssemblyFinder.FindAssemblies("*.Module.dll");
        foreach (var assembly in assemblies)
        {
            moduleManager.LoadModules(assembly);
        }
        
        SingletonPools.TryAdd<ModuleManager>(moduleManager);
    }
}
```

### 5. 性能监控和诊断

```csharp
public static class PerformanceExtensions
{
    public static T WithTiming<T>(this Func<T> func, Action<TimeSpan> onCompleted)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            return func();
        }
        finally
        {
            stopwatch.Stop();
            onCompleted(stopwatch.Elapsed);
        }
    }
    
    public static async Task<T> WithTimingAsync<T>(this Func<Task<T>> func, Action<TimeSpan> onCompleted)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            return await func();
        }
        finally
        {
            stopwatch.Stop();
            onCompleted(stopwatch.Elapsed);
        }
    }
}

// 使用示例
var result = (() => ExpensiveOperation()).WithTiming(elapsed => 
{
    if (elapsed.TotalMilliseconds > 1000)
    {
        Logger.LogWarning($"Slow operation detected: {elapsed.TotalMilliseconds}ms");
    }
});
```

## 🔧 测试和调试

框架提供了完整的单元测试，位于 [`tests/`](tests/) 目录：

- [`Inkslab.Tests`](tests/Inkslab.Tests/) - 核心功能测试
- [`Inkslab.Json.Tests`](tests/Inkslab.Json.Tests/) - JSON序列化测试  
- [`Inkslab.Config.Tests`](tests/Inkslab.Config.Tests/) - 配置读取测试
- [`Inkslab.Map.Tests`](tests/Inkslab.Map.Tests/) - 对象映射测试

### 示例测试用例

参考 [`StringExtensionsTests`](tests/Inkslab.Tests/StringExtensionsTests.cs) 了解如何编写测试：

```csharp
[Theory]
[InlineData("namingCase", NamingType.Normal, "namingCase")]
[InlineData("NamingCase", NamingType.CamelCase, "namingCase")]  
[InlineData("naming_case", NamingType.PascalCase, "NamingCase")]
[InlineData("UserName", NamingType.SnakeCase, "user_name")]
public void NamingConversionTest(string input, NamingType namingType, string expected)
{
    var result = input.ToNamingCase(namingType);
    Assert.Equal(expected, result);
}
```

## 📈 性能建议

1. **合理使用单例池** - 避免频繁创建重型对象
2. **缓存序列化配置** - JsonSerializerSettings 等配置对象应该缓存
3. **批量操作** - 使用框架提供的批量扩展方法
4. **异步优先** - 在 I/O 密集型操作中优先使用异步方法
5. **监控内存使用** - 定期检查大对象和集合的内存占用

## 🤝 贡献指南

1. Fork 项目
2. 创建特性分支 (`git checkout -b feature/AmazingFeature`)
3. 提交更改 (`git commit -m 'Add some AmazingFeature'`)
4. 推送到分支 (`git push origin feature/AmazingFeature`)
5. 打开 Pull Request

## 📝 许可证

本项目基于 MIT 许可证 - 查看 [LICENSE](LICENSE) 文件了解详情。

---

[![Stargazers over time](https://starchart.cc/tinylit/inkslab.svg)](https://starchart.cc/tinylit/inkslab)