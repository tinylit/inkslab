![Inkslab](inkslab.jpg 'Logo')

<!-- AI-META
Package: Inkslab.Map
Version: 1.2.23
TargetFrameworks: net461; netstandard2.1; net6.0
Namespace: Inkslab, Inkslab.Map
Dependencies: Inkslab
EntryContract: IMapper (src/Inkslab/Map/IMapper.cs)
Facade: Mapper (src/Inkslab/Map/Mapper.cs)
DefaultImplementation: MapperInstance (src/Inkslab.Map/MapperInstance.cs)
StartupType: MStartup (src/Inkslab.Map/MStartup.cs)
ProfileBase: Profile (src/Inkslab.Map/Profile.cs)
Keywords: object mapping, mapper, DTO, profile, value resolver, deep copy, convention
-->

## Inkslab.Map 是什么？

**Inkslab.Map** 是 [`IMapper`](src/Inkslab/Map/IMapper.cs) 的默认实现——一个基于表达式树的约定优先对象映射器，适用于 DTO / 实体 / VM 之间的双向转换。

- **属性名忽略大小写**自动匹配。
- 支持**集合 / 数组 / Dictionary** 映射。
- 支持**构造函数映射**（`New`）。
- 支持**属性级自定义**：`Map`、`From`、`Constant`、`Ignore`。
- 支持**继承与包含映射**：`Include`、`Profile`。
- 基于表达式树编译，**安全映射**非空类型间关系，无须处理 `null`/`Nullable` 分支。

> 与 [AutoMapper](https://automapper.org/) 定位不同：本框架优先"安全对象深拷贝 + 自动匹配"，AutoMapper 优先"值转换 + 显式映射"。需要类似 AutoMapper 的表现，可禁用深拷贝配置。

---

## 安装

```bash
dotnet add package Inkslab.Map
```

安装后由 [`MStartup`](src/Inkslab.Map/MStartup.cs) 自动注册 `IMapper` 默认实现。

---

## 核心契约

### `IMapper` [src/Inkslab/Map/IMapper.cs](src/Inkslab/Map/IMapper.cs)

```csharp
public interface IMapper
{
    T      Map<T>(object obj);
    object Map   (object obj, Type destinationType);
}
```

### 静态门面 `Mapper` [src/Inkslab/Map/Mapper.cs](src/Inkslab/Map/Mapper.cs)

```csharp
Mapper.Map<UserDto>(user);
Mapper.Map(user, typeof(UserDto));
```

### 默认实现 `MapperInstance` [src/Inkslab.Map/MapperInstance.cs](src/Inkslab.Map/MapperInstance.cs)

继承 `Profile`，实例化后可在**当前范围**内配置定制规则（`using` 确保释放）。

```csharp
using var instance = new MapperInstance();
instance.Map<From, To>()
        .Map(...);

var dto = instance.Map<To>(source);
```

---

## 快速入门

### 1. 自动映射

```csharp
FooDto foo = Mapper.Map<FooDto>(fooEntity);
BarDto bar = Mapper.Map<BarDto>(barEntity);
```

### 2. 集合 / 数组 / 字典

```csharp
var arr  = Mapper.Map<UserDto[]>(userArr);
var list = Mapper.Map<List<UserDto>>(users);
var dict = Mapper.Map<Dictionary<string, object>>(user);
```

### 3. 构造函数映射

目标类型具备匹配构造函数时自动选用：

```csharp
public record B(int Id, string Name, DateTime CreatedAt);

var b = Mapper.Map<B>(a);
```

---

## 进阶用法

### 1. 属性级自定义

```csharp
using var instance = new MapperInstance();

instance.Map<C1, C2>()
    .Map(x => x.R1, y => y.From(z => z.P1))                    // 字段改名
    .Map(x => x.T3, y => y.From(z => z.P3.ToString()))         // 类型转换
    .Map(x => x.D4, y => y.Constant(DateTimeKind.Utc))         // 常量
    .Map(x => x.I5, y => y.Ignore());                          // 忽略属性

var dst = instance.Map<C2>(src);
```

### 2. 继承与包含

```csharp
instance.Map<C2, C1>()
        .Include<C3>()                                         // 规则同时作用于 C2→C1 与 C2→C3
        .Map(x => x.P1, y => y.From(z => z.R1))
        .Map(x => x.P3, y => y.From(z => Convert.ToDateTime(z.T3)));

var c3 = instance.Map<C3>(src);
```

### 3. 构造函数自定义

```csharp
instance.New<C1, C4>(x => new C4(x.P1))
        .Map(x => x.T3, y => y.From(z => z.P3.ToString()))
        .Map(x => x.D4, y => y.Constant(DateTimeKind.Utc))
        .Map(x => x.I5, y => y.Ignore());
```

### 4. 集合到集合的自定义构造

```csharp
instance.Map<C1, C2>()
        .NewEnumerable<PagedList<C1>, PagedList<C2>>(
            (src, mapped) => new PagedList<C2>(mapped, src.PageIndex, src.PageSize, src.Total));
```

### 5. 使用 `Profile` 预定义规则

```csharp
public class OrderProfile : Profile
{
    public OrderProfile()
    {
        Map<OrderEntity, OrderDto>()
            .Map(d => d.PayTime, o => o.From(s => s.PaidAt));
    }
}

// 在启动阶段添加到全局配置：
// instance.AddProfile<OrderProfile>();
```

---

## 内置映射规则

默认实现内置多种常用规则（位于 [src/Inkslab.Map/Maps/](src/Inkslab.Map/Maps/)），按优先级匹配：

| 规则 | 场景 |
| --- | --- |
| `CloneableMap` | 源类型实现 `ICloneable` 时的克隆 |
| `ConvertMap` | `System.Convert` 覆盖的基础类型 |
| `EnumUnderlyingTypeMap` | 枚举 ↔ 其底层数值类型 |
| `StringToEnumMap` | 字符串 → 枚举 |
| `ParseStringMap` | 字符串 → `Guid` / `Version` / `TimeSpan` / `DateTimeOffset` |
| `ParseStringToBooleanMap` | 字符串 → `bool` |
| `ToStringMap` | 任意对象 → `string` |
| `KeyValueMap` | `KeyValuePair<,>` ↔ `KeyValuePair<,>` |
| `FromKeyIsStringValueIsAnyMap` | `IDictionary<string,*>` → 对象 |
| `ToKeyIsStringValueIsObjectMap` | 对象 → `IDictionary<string, object>` |
| `EnumerableMap` | 集合 ↔ 集合 |
| `ConstructorMap` | 构造函数单参注入 |
| `DefaultMap` | 兜底：属性名忽略大小写匹配 |

---

## 自定义实现

```csharp
public class MyMapper : IMapper
{
    public T      Map<T>(object obj)                        => /* ... */;
    public object Map   (object obj, Type destinationType)  => /* ... */;
}

// 启动前替换
SingletonPools.TryAdd<IMapper, MyMapper>();
```

---

## 单元测试

- [tests/Inkslab.Map.Tests/DefaultTests.cs](tests/Inkslab.Map.Tests/DefaultTests.cs)：默认规则。
- [tests/Inkslab.Map.Tests/CustomTests.cs](tests/Inkslab.Map.Tests/CustomTests.cs)：自定义规则。
- [tests/Inkslab.Map.Tests/ProfileConcurrencyTests.cs](tests/Inkslab.Map.Tests/ProfileConcurrencyTests.cs)：`Profile` 并发测试。
- [tests/Inkslab.Map.Tests/ApplyTest.cs](tests/Inkslab.Map.Tests/ApplyTest.cs)：应用示例。

---

## 性能与注意事项

- **默认深拷贝**：源引用不会被共享，避免后续修改误伤。
- **不支持循环引用**：递归关系会抛出或导致栈溢出；请显式 `Ignore` 自引用。
- **复用 `MapperInstance`**：批量映射时复用同一实例以命中表达式编译缓存。
- **线程安全**：`MapperInstance` 的配置阶段**非线程安全**；应用（`Map` 调用）线程安全。

---

## 说明

- 与 `AutoMapper` 的核心差异：
  - 本框架无需强制声明映射关系——未声明的属性按名称忽略大小写自动匹配。
  - 本框架支持**集合到集合**的自定义构造。
  - 本框架默认深拷贝，避免数据源被意外共享。
- 基于**表达式树分析 + 组合**实现映射，运行时零反射开销。
