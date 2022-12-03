![Inkslab](inkslab.jpg 'Logo')

### "Inkslab.Map"是什么？

    Inkslab.Map 一个基于约定的对象-对象映射器。

#### 使用方式：
* 通用。
   ```c#
    FooDto fooDto = Mapper.Map<FooDto>(foo);
    BarDto barDto = Mapper.Map<BarDto>(bar);
   ```
* 自定义匹配规则。
  ```c#
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
  ```
  - 常规。
   ```c#
    var constant = DateTimeKind.Utc;

    using var instance = new MapperInstance();

    instance.Map<C1, C2>()
        .Map(x => x.R1, y => y.From(z => z.P1)) //? 指定映射。
                                                //.Map(x => x.P2, y => y.From(z => z.P2)) // 名称相同可不指定，按照通用映射处理。
        .Map(x => x.T3, y => y.From(z => z.P3.ToString())) //? 指定映射规则。
        .Map(x => x.D4, y => y.Constant(constant)) //? 指定目标值为常量。
        .Map(x => x.I5, y => y.Ignore()); //? 忽略属性映射。

    var sourceC1 = new C1
    {
        P1 = 1,
        P2 = "Test",
        P3 = DateTime.Now,
        I5 = 10000
    };

    var destinationC2 = instance.Map<C2>(sourceC1);
   ```
  - 关系继承和重写（源类型及源的祖祖辈辈类型指定的关系，按照从子到祖的顺序优先被使用）。
   ```c#
    var constant = DateTimeKind.Utc;

    using var instance = new MapperInstance();

    instance.Map<C1, C2>()
        .Map(x => x.R1, y => y.From(z => z.P1)) //? 指定映射。
                                                //.Map(x => x.P2, y => y.From(z => z.P2)) // 名称相同可不指定，按照通用映射处理。
        .Map(x => x.T3, y => y.From(z => z.P3.ToString())) //? 指定映射规则。
        .Map(x => x.D4, y => y.Constant(constant)) //? 指定目标值为常量。
        .Map(x => x.I5, y => y.Ignore()); //? 忽略属性映射。

    instance.Map<C3, C2>()
        .Map(x => x.D4, y => y.From(y => y.D4)) //? 重写D4的规则，替换掉子类的常量关系。
        .Map(x => x.I5, y => y.From(z => z.I5)); //? 重写I5的规则，替换掉子类的忽略关系。

    var sourceC3 = new C3
    {
        P1 = 1,
        P2 = "Test",
        P3 = DateTime.Now,
        D4 = DateTimeKind.Local,
        I5 = 10000
    };

    var destinationC2 = instance.Map<C2>(sourceC3);
   ```
  - 包含。
  ```c#
    using var instance = new MapperInstance();

    instance.Map<C2, C1>()
        .Include<C3>() //? 指定的规则将同时对 C2->C1 和 C2->C3 都生效。
        .Map(x => x.P1, y => y.From(z => z.R1))
        .Map(x => x.P3, y => y.From(z => Convert.ToDateTime(z.T3)));

    var sourceC2 = new C2
    {
        R1 = 1,
        P2 = "Test",
        T3 = DateTime.Now.ToString(),
        D4 = DateTimeKind.Local,
        I5 = 10000
    };

    var destinationC3 = instance.Map<C3>(sourceC2);
  ```
  - 指定源对象到目标对象实例化。
  ```c#
    var constant = DateTimeKind.Utc;

    using var instance = new MapperInstance();

    instance.New<C1, C4>(x => new C4(x.P1)) //? 指定构造函数创建对象。
                                            //.Map(x => x.P2, y => y.From(z => z.P2)) // 名称相同可不指定，按照通用映射处理。
        .Map(x => x.T3, y => y.From(z => z.P3.ToString())) //? 指定映射规则。
        .Map(x => x.D4, y => y.Constant(constant)) //? 指定目标值为常量。
        .Map(x => x.I5, y => y.Ignore()); //? 忽略属性映射。

    var sourceC1 = new C1
    {
        P1 = 1,
        P2 = "Test",
        P3 = DateTime.Now,
        I5 = 10000
    };

    var destinationC4 = instance.Map<C4>(sourceC1);
  ```
  - 指定（自定义）迭代器到另一（自定义）迭代器的转换，详见单元测试。

#### 接口契约：

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

#### 自定义。
 * 实现接口契约：
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
 * 注入实现。
```c#
SingletonPools.TryAdd<IMapper, CustomMapper>();
```
 * 正常使用（通用方式）。

##### 说明：

* 映射是类似但不同于 [AutoMapper](https://www.nuget.org/packages/AutoMapper) 的高性能框架。
  - 框架着重与映射**安全对象**，而AutoMapper着重于转换对象。若想使用类似与AutoMapper的功能，请配置不深拷贝。
  - 框架致力于优先自定义，而不是完全自定义。框架映射完全不相关的对象时，以属性名称忽略大小写匹配；而AutoMapper必须指定映射关系。
  - 框架解决自定义集合到集合的映射，AutoMapper暂不支持。
* 基于表达式的分析和组合实现映射。
* 致力于解决非空类型之间的映射（自定义映射规则无需解决null判断和可空类型的转换）。