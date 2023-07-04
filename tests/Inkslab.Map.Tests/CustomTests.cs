using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Xunit;

namespace Inkslab.Map.Tests
{
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

    /// <summary>
    /// 无约束。
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class G1<T>
    {
        public G1(T value)
        {
            Value = value;
        }

        public T Value { get; }

        public int Version { get; set; }

        public DateTime P3 { get; set; }

        public DateTime Time { get; set; }
    }

    /// <summary>
    /// 分页的列表。
    /// </summary>
    /// <typeparam name="T">元素类型。</typeparam>
    public sealed class PagedList<T> : IEnumerable<T>, IEnumerable
    {
        private readonly IEnumerable<T> datas;

        /// <summary>
        /// 空集合。
        /// </summary>
        public static readonly PagedList<T> Empty = new PagedList<T>();

        /// <summary>
        /// 空集合。
        /// </summary>
        public PagedList() => datas = Enumerable.Empty<T>();

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="queryable">查询能力。</param>
        /// <param name="pageIndex">页码（索引从0开始）。</param>
        /// <param name="pageSize">分页条数。</param>
        public PagedList(IQueryable<T> queryable, int pageIndex, int pageSize)
        {
            if (pageIndex < 0)
            {
                throw new IndexOutOfRangeException("页码不能小于0。");
            }
            if (pageSize < 1)
            {
                throw new IndexOutOfRangeException("分页条目不能小于1。");
            }

            var results = queryable.Skip(pageIndex * pageSize)
                .Take(pageSize)
                .ToList();

            if (pageIndex == 0 && pageSize > results.Count)
            {
                Count = results.Count;
            }
            else
            {
                Count = queryable.Count();
            }

            datas = results;

            PageIndex = pageIndex;

            PageSize = pageSize;
        }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="datas">数据。</param>
        /// <param name="pageIndex">页码（索引从0开始）。</param>
        /// <param name="pageSize">分页条数。</param>
        /// <param name="totalCount">总数。</param>
        public PagedList(ICollection<T> datas, int pageIndex, int pageSize, int totalCount)
        {
            this.datas = datas ?? throw new ArgumentNullException(nameof(datas));

            if (pageIndex < 0)
            {
                throw new IndexOutOfRangeException("页码不能小于0。");
            }

            if (pageSize < 1)
            {
                throw new IndexOutOfRangeException("分页条目不能小于1。");
            }

            if (totalCount < 0)
            {
                throw new IndexOutOfRangeException("总数不能小于0。");
            }

            if (datas.Count > pageSize)
            {
                throw new IndexOutOfRangeException("集合元素总数不能大于分页条数。");
            }

            if (datas.Count > totalCount)
            {
                throw new IndexOutOfRangeException("集合元素总数不能大于总条数。");
            }

            PageIndex = pageIndex;

            PageSize = pageSize;

            Count = totalCount;
        }

        /// <summary>
        /// 当前页码（索引从0开始）。
        /// </summary>
        public int PageIndex { get; }

        /// <summary>
        /// 分页条数。
        /// </summary>
        public int PageSize { get; }

        /// <summary>
        /// 总数。
        /// </summary>
        public int Count { get; }

        /// <summary>
        /// 获取迭代器。
        /// </summary>
        /// <returns></returns>
        public IEnumerator<T> GetEnumerator() => datas.GetEnumerator();

        /// <summary>
        /// 获取迭代器。
        /// </summary>
        /// <returns></returns>
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    /// <summary>
    /// 瀑布流。
    /// </summary>
    /// <typeparam name="T">类型。</typeparam>
    public class LazyLoading<T>
    {
        /// <summary>
        /// 空集合。
        /// </summary>
        public LazyLoading()
        {
            Datas = new List<T>(0);
        }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="queryable">查询能力。</param>
        /// <param name="skipSize">跳过条数。</param>
        /// <param name="takeSize">获取条数。</param>
        public LazyLoading(IQueryable<T> queryable, int skipSize, int takeSize)
        {
            if (skipSize < 0)
            {
                throw new IndexOutOfRangeException("跳过数量不能小于0。");
            }
            if (takeSize < 1)
            {
                throw new IndexOutOfRangeException("获取条数不能小于1。");
            }

            Datas = queryable.Skip(skipSize)
                .Take(takeSize)
                .ToList();

            Offset = skipSize + Datas.Count;

            if (Datas.Count == takeSize)
            {
                HasNext = queryable
                    .Skip(Offset)
                    .Any();
            }
        }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="datas">数据。</param>
        /// <param name="offset">下一页偏移量。</param>
        /// <param name="hasNext">是否有下一条数据。</param>
        public LazyLoading(IEnumerable<T> datas, int offset, bool hasNext)
        {
            if (datas is null)
            {
                throw new ArgumentNullException(nameof(datas));
            }

            if (offset < 0)
            {
                throw new IndexOutOfRangeException("偏移量不能小于0。");
            }

            Datas = datas as IReadOnlyCollection<T> ?? new List<T>(datas);

            HasNext = hasNext;

            Offset = offset;
        }

        /// <summary>
        /// 下一页的偏移量。
        /// </summary>
        public int Offset { get; }

        /// <summary>
        /// 是否有下一个。
        /// </summary>
        public bool HasNext { get; set; }

        /// <summary>
        /// 数据。
        /// </summary>
        public IReadOnlyCollection<T> Datas { get; }
    }

    /// <summary>
    /// 自定义。
    /// </summary>
    public class CustomTests
    {
        /// <summary>
        /// 简单映射。
        /// </summary>
        [Fact]
        public void SimpleMapTest()
        {
            var constant = DateTimeKind.Utc;

            var x = typeof(int).GetMethod("op_Addition", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly, null, new[] { typeof(int), typeof(int) }, null);

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

            Assert.True(sourceC1.P1 == destinationC2.R1); //? 指定映射。
            Assert.True(sourceC1.P2 == destinationC2.P2); //? 默认映射。
            Assert.True(sourceC1.P3.ToString() == destinationC2.T3); //? 指定映射规则。
            Assert.True(destinationC2.D4 == constant); //? 常量。
            Assert.True(sourceC1.I5 == 10000 && destinationC2.I5 == long.MaxValue); //! 忽略映射。
        }

        /// <summary>
        /// 关系继承与重写（源类型及源的祖祖辈辈类型指定的关系，按照从子到祖的顺序优先被使用）。
        /// </summary>
        [Fact]
        public void ExtendsOrOverwriteTest()
        {
            var constant = DateTimeKind.Utc;

            using var instance = new MapperInstance();

            instance.Map<C1, C2>()
                .Map(x => x.R1, y => y.From(z => z.P1)) //? 指定映射。
                                                        //.Map(x => x.P2, y => y.From(z => z.P2)) // 名称相同可不指定，按照通用映射处理。
                .Map(x => x.T3, y => y.From(z => z.P3.ToString())) //? 指定映射规则。
                .Map(x => x.D4, y => y.Constant(constant)) //? 指定目标值为常量。
                .Map(x => x.I5, y => y.Ignore()); //? 忽略属性映射。

            instance.Map<C3, C2>()
                .Map(x => x.D4, y => y.From(y => y.D4))
                .Map(x => x.I5, y => y.From(z => z.I5));

            var sourceC3 = new C3
            {
                P1 = 1,
                P2 = "Test",
                P3 = DateTime.Now,
                D4 = DateTimeKind.Local,
                I5 = 10000
            };

            var destinationC2 = instance.Map<C2>(sourceC3);

            Assert.True(sourceC3.P1 == destinationC2.R1); //? 继承 C1 的映射关系。
            Assert.True(sourceC3.P2 == destinationC2.P2); //? 继承 C1 的映射关系。
            Assert.True(sourceC3.P3.ToString() == destinationC2.T3); //? 继承 C1 的映射关系。
            Assert.True(destinationC2.D4 == sourceC3.D4); //? 关系重写。
            Assert.True(sourceC3.I5 == destinationC2.I5); //! 关系重写。
        }

        /// <summary>
        /// 包含。
        /// </summary>
        [Fact]
        public void IncludeTest()
        {
            using var instance = new MapperInstance();

            instance.Map<C2, C1>()
                .Include<C3>()
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

            Assert.True(sourceC2.R1 == destinationC3.P1); //? 继承 C1 的映射关系。
            Assert.True(sourceC2.P2 == destinationC3.P2); //? 继承 C1 的映射关系。
            Assert.True(sourceC2.T3 == destinationC3.P3.ToString()); //? 继承 C1 的映射关系。
        }

        /// <summary>
        /// 泛型约束测试。
        /// </summary>
        [Fact]
        public void IncludeConstraintsSimpleToGTest()
        {
            using var instance = new MapperInstance();

            instance.New<C1, G1<C1>>(x => new G1<C1>(x)
            {
                Version = 1,
                Time = x.P3
            })
            .Map(x => x.P3, x => x.From(y => y.P3))
            .IncludeConstraints((x/* 源类型 */, y/* 原类型泛型参数，不是泛型时为空数组 */, z/* 目标类型泛型参数类型数组 */) => x == z[0] || x.IsSubclassOf(z[0])); //! 目标类型必须是泛型。

            var sourceC2 = new C3
            {
                P1 = 100,
                P2 = "Test",
                D4 = DateTimeKind.Local,
                I5 = 10000,
                P3 = DateTime.UtcNow
            };

            var destinationG1 = instance.Map<G1<C3>>(sourceC2);

            Assert.True(destinationG1.Version == 1);
            Assert.True(destinationG1.P3 == sourceC2.P3);
            Assert.True(destinationG1.Time == sourceC2.P3);

            var destinationC3 = destinationG1.Value;

            Assert.True(sourceC2.P1 == destinationC3.P1);
            Assert.True(sourceC2.P2 == destinationC3.P2);
            Assert.True(sourceC2.P3 == destinationC3.P3);
        }

        /// <summary>
        /// 构造函数重写。
        /// </summary>
        [Fact]
        public void NewTest()
        {
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

            Assert.True(sourceC1.P1 == destinationC4.P1); //? 指定映射。
            Assert.True(sourceC1.P2 == destinationC4.P2); //? 默认映射。
            Assert.True(sourceC1.P3.ToString() == destinationC4.T3); //? 指定映射规则。
            Assert.True(destinationC4.D4 == constant); //? 常量。
            Assert.True(sourceC1.I5 == 10000 && destinationC4.I5 == long.MaxValue); //! 忽略映射。
        }

        /// <summary>
        /// 构造函数重写。
        /// </summary>
        [Fact]
        public void NewLazyLoadingTest()
        {
            var constant = DateTimeKind.Utc;

            using var instance = new MapperInstance();

            instance.New<LazyLoading<object>, LazyLoading<object>>(x => new LazyLoading<object>(instance.Map<List<object>>(x.Datas), x.Offset, x.HasNext) { HasNext = x.HasNext })
                .IncludeConstraints((x, y, z) => true);

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

            var source = new LazyLoading<C1>(new List<C1> { sourceC1 }, 10, true);

            var destination = instance.Map<LazyLoading<C2>>(source);
        }

        /// <summary>
        /// 元素对等的迭代器转换。
        /// </summary>
        [Fact]
        public void NewEnumerableSimTest()
        {
            var constant = DateTimeKind.Utc;

            using var instance = new MapperInstance();

            instance.Map<C1, C2>()
                .NewEnumerable<PagedList<C1>, PagedList<C2>>((x, y) => new PagedList<C2>(y, x.PageIndex, x.PageSize, x.Count))
                .Map(x => x.R1, y => y.From(z => z.P1)) //? 指定映射。
                                                        //.Map(x => x.P2, y => y.From(z => z.P2)) // 名称相同可不指定，按照通用映射处理。
                .Map(x => x.T3, y => y.From(z => z.P3.ToString())) //? 指定映射规则。
                .Map(x => x.D4, y => y.Constant(constant)) //? 指定目标值为常量。
                .Map(x => x.I5, y => y.Ignore()); //? 忽略属性映射。

            var sourceList = new List<C1>
            {
                new C1
                {
                    P1 = 1,
                    P2 = "Test",
                    P3 = DateTime.Now,
                    I5 = 10000
                },

                new C1
                {
                    P1 = 1,
                    P2 = "Test",
                    P3 = DateTime.Now,
                    I5 = 10000
                },

                new C1
                {
                    P1 = 1,
                    P2 = "Test",
                    P3 = DateTime.Now,
                    I5 = 10000
                }
            };

            var destinationList = instance.Map<PagedList<C2>>(new PagedList<C1>(sourceList, 0, sourceList.Count, sourceList.Count));

            //? 集合元素个数。
            Assert.True(destinationList.Count == sourceList.Count);
        }

        /// <summary>
        /// 元素有继承关系的迭代器转换。
        /// </summary>
        [Fact]
        public void NewEnumerableByItemInheritTest()
        {
            var constant = DateTimeKind.Utc;

            using var instance = new MapperInstance();

            instance.Map<C1, C2>()
                .NewEnumerable<PagedList<C1>, PagedList<C2>>((x, y) => new PagedList<C2>(y, x.PageIndex, x.PageSize, x.Count))
                .Map(x => x.R1, y => y.From(z => z.P1)) //? 指定映射。
                                                        //.Map(x => x.P2, y => y.From(z => z.P2)) // 名称相同可不指定，按照通用映射处理。
                .Map(x => x.T3, y => y.From(z => z.P3.ToString())) //? 指定映射规则。
                .Map(x => x.D4, y => y.Constant(constant)) //? 指定目标值为常量。
                .Map(x => x.I5, y => y.Ignore()); //? 忽略属性映射。

            var sourceList = new List<C3>
            {
                new C3
                {
                    P1 = 1,
                    P2 = "Test",
                    P3 = DateTime.Now,
                    I5 = 10000
                },

                new C3
                {
                    P1 = 1,
                    P2 = "Test",
                    P3 = DateTime.Now,
                    I5 = 10000
                },

                new C3
                {
                    P1 = 1,
                    P2 = "Test",
                    P3 = DateTime.Now,
                    I5 = 10000
                }
            };

            //? 声明 C1 ，使用 C3 进行映射。
            var destinationList = instance.Map<PagedList<C2>>(new PagedList<C3>(sourceList, 0, sourceList.Count, sourceList.Count));

            //? 集合元素个数。
            Assert.True(destinationList.Count == sourceList.Count);
        }

        /// <summary>
        /// 按照类型约束作为入参。
        /// </summary>
        [Fact]
        public void NewEnumerableByConstraintsTest()
        {
            using var instance = new MapperInstance();

            instance.Map<object, object>() //? 任意类型，都可以，仅对 NewEnumerable 有效。
                .NewEnumerable<List<object>, PagedList<object>>((x, y) => new PagedList<object>(y, 0, x.Count, x.Count));

            var sourceList = new List<C3>
            {
                new C3
                {
                    P1 = 1,
                    P2 = "Test",
                    P3 = DateTime.Now,
                    I5 = 10000
                },

                new C3
                {
                    P1 = 1,
                    P2 = "Test",
                    P3 = DateTime.Now,
                    I5 = 10000
                },

                new C3
                {
                    P1 = 1,
                    P2 = "Test",
                    P3 = DateTime.Now,
                    I5 = 10000
                }
            };

            //? 声明 C1 ，使用 C3 进行映射。
            var destinationList = instance.Map<PagedList<C2>>(sourceList);

            //? 集合元素个数。
            Assert.True(destinationList.Count == sourceList.Count);
        }
    }
}
