using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection.Metadata;
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
    /// 无泛型的 <see cref="INewInstance{TSource, TSourceItem, TDestination, TDestinationItem}"/>.
    /// </summary>
    public class NewInstanceGenericOfNothings : INewInstance<List<C1>, C1, LinkedList<C2>, C2>
    {
        /// <summary>
        /// <inheritdoc/>.
        /// </summary>
        public LinkedList<C2> NewInstance(List<C1> source, List<C2> destinationItems) => new LinkedList<C2>(destinationItems);
    }

    /// <summary>
    /// 一个泛型的 <see cref="INewInstance{TSource, TSourceItem, TDestination, TDestinationItem}"/>.
    /// </summary>
    public class NewInstanceGenericOfSingle<TItem> : INewInstance<List<TItem>, TItem, ReadOnlyCollection<TItem>, TItem>
    {
        /// <summary>
        /// <inheritdoc/>.
        /// </summary>
        public ReadOnlyCollection<TItem> NewInstance(List<TItem> source, List<TItem> destinationItems) => new ReadOnlyCollection<TItem>(destinationItems);
    }

    /// <summary>
    /// 两个泛型的 <see cref="INewInstance{TSource, TSourceItem, TDestination, TDestinationItem}"/>.
    /// </summary>
    public class NewInstanceGenericOfDouble<TSourceItem, TDestinationItem> : INewInstance<PagedList<TSourceItem>, TSourceItem, PagedList<TDestinationItem>, TDestinationItem>
    {
        /// <summary>
        /// <inheritdoc/>.
        /// </summary>
        public PagedList<TDestinationItem> NewInstance(PagedList<TSourceItem> source, List<TDestinationItem> destinationItems) => new PagedList<TDestinationItem>(destinationItems, source.PageIndex, source.PageSize, source.Count);
    }

    /// <summary>
    /// 四个泛型的 <see cref="INewInstance{TSource, TSourceItem, TDestination, TDestinationItem}"/>.
    /// </summary>
    public class NewInstanceGenericOfFull<TSource, TSourceItem, TDestination, TDestinationItem> : INewInstance<TSource, TSourceItem, TDestination, TDestinationItem> where TSource : IEnumerable<TSourceItem> where TDestination : ICollection<TDestinationItem>, new()
    {
        /// <summary>
        /// <inheritdoc/>.
        /// </summary>
        public TDestination NewInstance(TSource source, List<TDestinationItem> destinationItems)
        {
            var destination = new TDestination();

            foreach (var destinationItem in destinationItems)
            {
                destination.Add(destinationItem);
            }

            return destination;
        }
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
        /// <see cref="INewInstance{TSource, TSourceItem, TDestination, TDestinationItem}"/> 无泛型的实现类。特定源类型和目标类型的转换。
        /// </summary>
        [Fact]
        public void NewInstanceGenericOfNothingsTest()
        {
            var constant = DateTimeKind.Utc;

            using var instance = new MapperInstance();

            instance.Map<C1, C2>()
                .Map(x => x.R1, y => y.From(z => z.P1)) //? 指定映射。
                                                        //.Map(x => x.P2, y => y.From(z => z.P2)) // 名称相同可不指定，按照通用映射处理。
                .Map(x => x.T3, y => y.From(z => z.P3.ToString())) //? 指定映射规则。
                .Map(x => x.D4, y => y.Constant(constant)) //? 指定目标值为常量。
                .Map(x => x.I5, y => y.Ignore()); //? 忽略属性映射。

            instance.New(typeof(NewInstanceGenericOfNothings));

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

            var destinationList = instance.Map<LinkedList<C2>>(sourceList);

            //? 集合元素个数。
            Assert.True(destinationList.Count == sourceList.Count);
        }

        /// <summary>
        /// <see cref="INewInstance{TSource, TSourceItem, TDestination, TDestinationItem}"/> 一个泛型的实现类。源集合类型的表示任意元素，到同元素的目标集合类型转换。
        /// </summary>
        [Fact]
        public void NewInstanceGenericOfSingleTest()
        {
            using var instance = new MapperInstance();

            instance.New(typeof(NewInstanceGenericOfSingle<>));

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

            var destinationReadOnlyCollection = instance.Map<ReadOnlyCollection<C1>>(sourceList);

            Assert.True(sourceList.Count == destinationReadOnlyCollection.Count);
        }

        /// <summary>
        /// <see cref="INewInstance{TSource, TSourceItem, TDestination, TDestinationItem}"/> 两个泛型的实现类。源集合类型的表示任意元素，到目标集合类型任意元素的转换。
        /// </summary>
        [Fact]
        public void NewInstanceGenericOfDoubleTest()
        {
            var items = new List<C1>
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

            var constant = DateTimeKind.Utc;

            using var instance = new MapperInstance();

            instance.Map<C1, C2>()
                .Map(x => x.R1, y => y.From(z => z.P1)) //? 指定映射。
                                                        //.Map(x => x.P2, y => y.From(z => z.P2)) // 名称相同可不指定，按照通用映射处理。
                .Map(x => x.T3, y => y.From(z => z.P3.ToString())) //? 指定映射规则。
                .Map(x => x.D4, y => y.Constant(constant)) //? 指定目标值为常量。
                .Map(x => x.I5, y => y.Ignore()); //? 忽略属性映射。

            instance.New(typeof(NewInstanceGenericOfDouble<,>));

            var sourceList = new PagedList<C1>(items, 5, 10, 53);

            var destinationList = instance.Map<PagedList<C2>>(sourceList);

            //? 集合元素个数。
            Assert.True(destinationList.Count() == sourceList.Count());
            //? 总数。
            Assert.True(destinationList.Count == sourceList.Count);
            Assert.True(destinationList.PageIndex == sourceList.PageIndex);
            Assert.True(destinationList.PageSize == sourceList.PageSize);
        }

        /// <summary>
        /// <see cref="INewInstance{TSource, TSourceItem, TDestination, TDestinationItem}"/> 四个泛型的实现类。任意满足泛型约束的类型转换。
        /// </summary>
        [Fact]
        public void NewInstanceGenericOfFullTest()
        {
            var items = new List<C1>
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

            var constant = DateTimeKind.Utc;

            using var instance = new MapperInstance();

            instance.Map<C1, C2>()
                .Map(x => x.R1, y => y.From(z => z.P1)) //? 指定映射。
                                                        //.Map(x => x.P2, y => y.From(z => z.P2)) // 名称相同可不指定，按照通用映射处理。
                .Map(x => x.T3, y => y.From(z => z.P3.ToString())) //? 指定映射规则。
                .Map(x => x.D4, y => y.Constant(constant)) //? 指定目标值为常量。
                .Map(x => x.I5, y => y.Ignore()); //? 忽略属性映射。

            instance.New(typeof(NewInstanceGenericOfFull<,,,>));

            var sourceList = new PagedList<C1>(items, 5, 10, 53);

            var destinationList = instance.Map<LinkedList<C2>>(sourceList);

            //? 集合元素个数。
            Assert.True(destinationList.Count == sourceList.Count());
        }
    }
}
