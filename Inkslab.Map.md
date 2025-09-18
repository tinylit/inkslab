
![Inkslab](inkslab.jpg 'Logo')

## "Inkslab.Map"是什么？

**Inkslab.Map** 是一个高性能、约定优先的对象-对象映射器，支持属性自动匹配、集合映射、构造函数映射及自定义规则，适用于 DTO、实体、视图模型等多场景数据转换。

---

## 🚀 快速上手

### 1. 通用对象映射
```csharp
FooDto fooDto = Mapper.Map<FooDto>(foo);
BarDto barDto = Mapper.Map<BarDto>(bar);
```

### 2. 集合与数组映射
```csharp
var destArr = Mapper.Map<D[]>(sourceArr);
var dic = Mapper.Map<Dictionary<string, object>>(sourceObj);
```

### 3. 构造函数映射
```csharp
var dest = Mapper.Map<B>(sourceA); // B 有构造函数 B(int, string, DateTime)
```

---

## 🏗️ 进阶用法

### 1. 属性自定义匹配
```csharp
using var instance = new MapperInstance();

instance.Map<C1, C2>()
    .Map(x => x.R1, y => y.From(z => z.P1)) // 指定属性映射
    .Map(x => x.T3, y => y.From(z => z.P3.ToString())) // 映射并转换类型
    .Map(x => x.D4, y => y.Constant(DateTimeKind.Utc)) // 目标属性赋常量
    .Map(x => x.I5, y => y.Ignore()); // 忽略属性

var destC2 = instance.Map<C2>(sourceC1);
```

### 2. 包含与继承映射
```csharp
instance.Map<C2, C1>()
    .Include<C3>() // 规则同时应用于 C2->C1 和 C2->C3
    .Map(x => x.P1, y => y.From(z => z.R1))
    .Map(x => x.P3, y => y.From(z => Convert.ToDateTime(z.T3)));

var destC3 = instance.Map<C3>(sourceC2);
```

### 3. 构造函数自定义
```csharp
instance.New<C1, C4>(x => new C4(x.P1))
    .Map(x => x.T3, y => y.From(z => z.P3.ToString()))
    .Map(x => x.D4, y => y.Constant(DateTimeKind.Utc))
    .Map(x => x.I5, y => y.Ignore());

var destC4 = instance.Map<C4>(sourceC1);
```

### 4. 集合到集合自定义
```csharp
instance.Map<C1, C2>()
    .NewEnumerable<PagedList<C1>, PagedList<C2>>((x, y) => new PagedList<C2>(y, x.PageIndex, x.PageSize, x.Count));
```

---

## 🧑‍💻 单元测试参考

详见 [`tests/Inkslab.Map.Tests/DefaultTests.cs`](tests/Inkslab.Map.Tests/DefaultTests.cs) 和 [`tests/Inkslab.Map.Tests/CustomTests.cs`](tests/Inkslab.Map.Tests/CustomTests.cs)，覆盖了常规映射、集合映射、构造函数、继承、忽略、常量赋值等场景。

---

## 💡 常见问题与建议

- 属性名不区分大小写自动匹配
- 支持深拷贝和浅拷贝
- 支持多种集合类型转换
- 支持自定义映射规则和构造函数
- 递归关系默认不支持，避免循环引用
- 批量映射建议复用 MapperInstance 以提升性能
- 实现 IMapper 接口可扩展特殊业务场景

---
    /// <summary>
    /// <inheritdoc/>.
    /// </summary>
    public class C1
    {
        /// <summary>
        /// <inheritdoc/>.
        /// </summary>
        public int P1 { get; set; }

        /// <summary>
        /// <inheritdoc/>.
        /// </summary>
        public string P2 { get; set; }

        /// <summary>
        /// <inheritdoc/>.
        /// </summary>
        public DateTime P3 { get; set; }

        /// <summary>
        /// <inheritdoc/>.
        /// </summary>
        public long I5 { get; set; }
    }

    /// <summary>
    /// <inheritdoc/>.
    /// </summary>
    public class C2
    {
        /// <summary>
        /// <inheritdoc/>.
        /// </summary>
        public int R1 { get; set; }

        /// <summary>
        /// <inheritdoc/>.
        /// </summary>
        public string P2 { get; set; }

        /// <summary>
        /// <inheritdoc/>.
        /// </summary>
        public string T3 { get; set; }

        /// <summary>
        /// <inheritdoc/>.
        /// </summary>
        public DateTimeKind D4 { get; set; }

        /// <summary>
        /// <inheritdoc/>.
        /// </summary>
        public long I5 { get; set; } = long.MaxValue;
    }

    /// <summary>
    /// <inheritdoc/>.
    /// </summary>
    public class C3 : C1
    {
        /// <summary>
        /// <inheritdoc/>.
        /// </summary>
        public DateTimeKind D4 { get; set; }
    }

    /// <summary>
    /// <inheritdoc/>.
    /// </summary>
    public class C4
    {
        /// <summary>
        /// <inheritdoc/>.
        /// </summary>
        public C4(int p1) => P1 = p1;

        /// <summary>
        /// <inheritdoc/>.
        /// </summary>
        public int P1 { get; set; }

        /// <summary>
        /// <inheritdoc/>.
        /// </summary>
        public string P2 { get; set; }

        /// <summary>
        /// <inheritdoc/>.
        /// </summary>
        public string T3 { get; set; }

        /// <summary>
        /// <inheritdoc/>.
        /// </summary>
        public DateTimeKind D4 { get; set; }

        /// <summary>
        /// <inheritdoc/>.
        /// </summary>
        public long I5 { get; set; } = long.MaxValue;
    }

## 📑 接口契约

```c#
    /// <summary>
    /// 映射器。
    /// </summary>
    public interface IMapper
    {
        /// <summary>
        /// 对象映射。
        /// </summary>
        /// <param name="obj">数据源。</param>
        /// <typeparam name="T">目标类型。</typeparam>
        /// <returns>映射的对象。</returns>
        T Map<T>(object obj);

        /// <summary> 
        /// 对象映射。
        /// </summary>
        /// <param name="obj">数据源。</param>
        /// <param name="destinationType">目标类型。</param>
        /// <returns>映射的对象。</returns>
        object Map(object obj, Type destinationType);
    }
```

## 🔌 自定义扩展

### 1. 实现接口契约
```c#
    /// <summary>
    /// 自定义映射。
    /// </summary>
    public class CustomMapper : IMapper
    {
        /// <summary>
        /// 对象映射。
        /// </summary>
        /// <param name="obj">数据源。</param>
        /// <typeparam name="T">目标类型。</typeparam>
        /// <returns>映射的对象。</returns>
        public T Map<T>(object obj){
            //TODO: 对象转换逻辑。
        }

        /// <summary> 
        /// 对象映射。
        /// </summary>
        /// <param name="obj">数据源。</param>
        /// <param name="destinationType">目标类型。</param>
        /// <returns>映射的对象。</returns>
        public object Map(object obj, Type destinationType){
            //TODO: 对象转换逻辑。
        }
    }
```
### 2. 注入实现
```c#
SingletonPools.TryAdd<IMapper, CustomMapper>();
```

### 3. 正常使用（通用方式）


---

## 📖 说明

* 映射是类似但不同于 [AutoMapper](https://www.nuget.org/packages/AutoMapper) 的高性能框架。
    - 框架着重于映射**安全对象**，而 AutoMapper 着重于转换对象。若需类似 AutoMapper 的功能，请配置不深拷贝。
    - 框架优先支持自定义，完全不相关的对象以属性名称忽略大小写自动匹配；而 AutoMapper 必须指定映射关系。
    - 框架支持自定义集合到集合的映射，AutoMapper 暂不支持。
* 基于表达式的分析和组合实现映射。
* 致力于解决非空类型之间的映射（自定义映射规则无需处理 null 判断和可空类型转换）。

---