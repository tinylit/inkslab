using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Inkslab.Tests
{
    /// <summary>
    /// 去重测试。
    /// </summary>
    public class DistinctA
    {
        /// <summary>
        /// Id。
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// 名称。
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Date。
        /// </summary>
        public DateTime Date { get; set; }

        /// <summary>
        /// 重写Equals用于测试
        /// </summary>
        public override bool Equals(object obj)
        {
            return obj is DistinctA other && Id == other.Id && Name == other.Name;
        }

        /// <summary>
        /// 重写GetHashCode用于测试
        /// </summary>
        public override int GetHashCode()
        {
            return HashCode.Combine(Id, Name);
        }
    }

    /// <summary>
    /// <see cref="System.Collections.EnumerableExtensions"/> 和 <see cref="System.Collections.Generic.EnumerableExtensions"/> 测试。
    /// </summary>
    public class EnumerableExtensionsTests
    {
        #region System.Collections.EnumerableExtensions 测试

        /// <summary>
        /// Join 方法基本功能测试。
        /// </summary>
        [Fact]
        public void JoinTest()
        {
            var list = new List<object>
            {
                1,
                2,
                null,
                "xzy"
            };

            var expected = "1,2,xzy";
            var result = list.Join(","); // 自动忽略 null 值

            Assert.Equal(expected, result);
        }

        /// <summary>
        /// Join 方法默认分隔符测试。
        /// </summary>
        [Fact]
        public void Join_DefaultSeparator_ShouldUseComma()
        {
            var list = new List<object> { 1, 2, 3 };
            var result = list.Join();

            Assert.Equal("1,2,3", result);
        }

        /// <summary>
        /// Join 方法空序列测试。
        /// </summary>
        [Fact]
        public void Join_EmptySequence_ShouldReturnEmptyString()
        {
            var list = new List<object>();
            var result = list.Join(",");

            Assert.Equal(string.Empty, result);
        }

        /// <summary>
        /// Join 方法全部为null的序列测试。
        /// </summary>
        [Fact]
        public void Join_AllNullSequence_ShouldReturnEmptyString()
        {
            var list = new List<object> { null, null, null };
            var result = list.Join(",");

            Assert.Equal(string.Empty, result);
        }

        /// <summary>
        /// Join 方法单个元素测试。
        /// </summary>
        [Fact]
        public void Join_SingleElement_ShouldReturnElementAsString()
        {
            var list = new List<object> { "hello" };
            var result = list.Join(",");

            Assert.Equal("hello", result);
        }

        /// <summary>
        /// Join 方法自定义分隔符测试。
        /// </summary>
        [Fact]
        public void Join_CustomSeparator_ShouldWork()
        {
            var list = new List<object> { 1, 2, 3 };
            var result = list.Join(" | ");

            Assert.Equal("1 | 2 | 3", result);
        }

        /// <summary>
        /// ForEach 方法（非泛型）基本功能测试。
        /// </summary>
        [Fact]
        public void ForEach_NonGeneric_ShouldExecuteActionForEachElement()
        {
            var list = new List<object> { 1, 2, 3 };
            var results = new List<int>();

            list.ForEach<int>(x => results.Add(x));

            Assert.Equal(new[] { 1, 2, 3 }, results);
        }

        /// <summary>
        /// ForEach 方法（非泛型）空Action测试。
        /// </summary>
        [Fact]
        public void ForEach_NonGeneric_NullAction_ShouldThrowArgumentNullException()
        {
            var list = new List<object> { 1, 2, 3 };

            Assert.Throws<ArgumentNullException>(() => list.ForEach<int>(null));
        }

        #endregion

        #region System.Collections.Generic.EnumerableExtensions 测试

        /// <summary>
        /// ForEach 方法（泛型）基本功能测试。
        /// </summary>
        [Fact]
        public void ForEach_Generic_ShouldExecuteActionForEachElement()
        {
            var list = new List<int> { 1, 2, 3, 4, 5 };
            var results = new List<int>();

            list.ForEach(x => results.Add(x * 2));

            Assert.Equal(new[] { 2, 4, 6, 8, 10 }, results);
        }

        /// <summary>
        /// ForEach 方法对List优化测试。
        /// </summary>
        [Fact]
        public void ForEach_Generic_List_ShouldUseListForEach()
        {
            var list = new List<string> { "a", "b", "c" };
            var results = new List<string>();

            list.ForEach(x => results.Add(x.ToUpper()));

            Assert.Equal(new[] { "A", "B", "C" }, results);
        }

        /// <summary>
        /// ForEach 方法对数组优化测试。
        /// </summary>
        [Fact]
        public void ForEach_Generic_Array_ShouldUseArrayForEach()
        {
            var array = new[] { 1, 2, 3 };
            var results = new List<int>();

            array.ForEach(x => results.Add(x + 10));

            Assert.Equal(new[] { 11, 12, 13 }, results);
        }

        /// <summary>
        /// ForEach 方法空Action测试。
        /// </summary>
        [Fact]
        public void ForEach_Generic_NullAction_ShouldThrowArgumentNullException()
        {
            var list = new List<int> { 1, 2, 3 };

            Assert.Throws<ArgumentNullException>(() => list.ForEach(null));
        }

        /// <summary>
        /// Distinct 方法基本功能测试。
        /// </summary>
        [Fact]
        public void DistinctTest()
        {
            var list = new List<DistinctA>();
            var random = new Random(42); // 使用固定种子确保可重现性

            for (int i = 0; i < 50; i++)
            {
                list.Add(new DistinctA
                {
                    Id = random.Next(10), // 限制ID范围确保有重复
                    Name = i.ToString(),
                    Date = DateTime.Now
                });
            }

            var distinctList = list.Distinct(x => x.Id).ToList();
            var hashSet = new HashSet<int>();

            foreach (var item in distinctList)
            {
                Assert.True(hashSet.Add(item.Id), $"发现重复的ID: {item.Id}");
            }

            // 验证去重后的数量不超过ID的可能值数量
            Assert.True(distinctList.Count <= 10);
        }

        /// <summary>
        /// Distinct 方法带比较器测试。
        /// </summary>
        [Fact]
        public void Distinct_WithComparer_ShouldWork()
        {
            var list = new List<string> { "Apple", "apple", "BANANA", "banana", "Cherry" };
            var result = list.Distinct(x => x, StringComparer.OrdinalIgnoreCase).ToList();

            Assert.Equal(3, result.Count);
            Assert.Contains("Apple", result);
            Assert.Contains("BANANA", result);
            Assert.Contains("Cherry", result);
        }

        /// <summary>
        /// Distinct 方法空源测试。
        /// </summary>
        [Fact]
        public void Distinct_NullSource_ShouldThrowArgumentNullException()
        {
            IEnumerable<string> list = null;

            Assert.Throws<ArgumentNullException>(() => list.Distinct(x => x).ToList());
        }

        /// <summary>
        /// Distinct 方法空键选择器测试。
        /// </summary>
        [Fact]
        public void Distinct_NullKeySelector_ShouldThrowArgumentNullException()
        {
            var list = new List<string> { "a", "b", "c" };

            Assert.Throws<ArgumentNullException>(() => list.Distinct<string, string>(null).ToList());
        }

        /// <summary>
        /// AlignOverall 方法基本功能测试。
        /// </summary>
        [Fact]
        public void AlignTest()
        {
            var array1 = new List<int> { 1, 2, 3, 4, 5, 6, 7 };
            var array2 = new List<int> { 4, 5, 1, 2, 3, 6, 7 };

            var array3 = array2.AlignOverall(array1).ToList();

            array3.ZipEach(array1, Assert.Equal);
        }

        /// <summary>
        /// AlignOverall 方法空序列测试。
        /// </summary>
        [Fact]
        public void AlignOverall_EmptyOuter_ShouldReturnEmpty()
        {
            var outer = new List<int>();
            var inner = new List<int> { 1, 2, 3 };

            var result = outer.AlignOverall(inner).ToList();

            Assert.Empty(result);
        }

        /// <summary>
        /// AlignOverall 方法带比较器测试。
        /// </summary>
        [Fact]
        public void AlignOverall_WithComparer_ShouldWork()
        {
            var outer = new List<string> { "apple", "BANANA", "cherry" };
            var inner = new List<string> { "APPLE", "banana", "CHERRY" };

            var result = outer.AlignOverall(inner, StringComparer.OrdinalIgnoreCase).ToList();

            Assert.Equal(3, result.Count);
            Assert.Equal("apple", result[0]);
            Assert.Equal("BANANA", result[1]);
            Assert.Equal("cherry", result[2]);
        }

        /// <summary>
        /// AlignOverall 方法空参数测试。
        /// </summary>
        [Theory]
        [InlineData(true, false)] // outer为null
        [InlineData(false, true)] // inner为null
        public void AlignOverall_NullParameters_ShouldThrowArgumentNullException(bool outerNull, bool innerNull)
        {
            var outer = outerNull ? null : new List<int> { 1, 2, 3 };
            var inner = innerNull ? null : new List<int> { 1, 2, 3 };

            Assert.Throws<ArgumentNullException>(() => outer.AlignOverall(inner).ToList());
        }

        /// <summary>
        /// Align 方法基本功能测试。
        /// </summary>
        [Fact]
        public void Align_WithKeySelector_ShouldWork()
        {
            var people = new List<DistinctA>
            {
                new DistinctA { Id = 1, Name = "Alice" },
                new DistinctA { Id = 2, Name = "Bob" },
                new DistinctA { Id = 3, Name = "Charlie" }
            };
            var order = new List<int> { 3, 1, 2 };

            var result = people.Align(order, p => p.Id).ToList();

            Assert.Equal(3, result.Count);
            Assert.Equal("Charlie", result[0].Name);
            Assert.Equal("Alice", result[1].Name);
            Assert.Equal("Bob", result[2].Name);
        }

        /// <summary>
        /// Align 方法带结果选择器测试。
        /// </summary>
        [Fact]
        public void Align_WithResultSelector_ShouldWork()
        {
            var people = new List<DistinctA>
            {
                new DistinctA { Id = 1, Name = "Alice" },
                new DistinctA { Id = 2, Name = "Bob" }
            };
            var order = new List<int> { 2, 1 };

            var result = people.Align(order, p => p.Id, p => p.Name).ToList();

            Assert.Equal(new[] { "Bob", "Alice" }, result);
        }

        /// <summary>
        /// ZipEach 方法基本功能测试。
        /// </summary>
        [Fact]
        public void ZipEach_ShouldExecuteActionForMatchingElements()
        {
            var first = new List<int> { 1, 2, 3 };
            var second = new List<string> { "one", "two", "three" };
            var results = new List<string>();

            first.ZipEach(second, (num, str) => results.Add($"{num}-{str}"));

            Assert.Equal(new[] { "1-one", "2-two", "3-three" }, results);
        }

        /// <summary>
        /// ZipEach 方法不同长度序列测试。
        /// </summary>
        [Fact]
        public void ZipEach_DifferentLengths_ShouldStopAtShorter()
        {
            var first = new List<int> { 1, 2, 3, 4, 5 };
            var second = new List<string> { "one", "two" };
            var results = new List<string>();

            first.ZipEach(second, (num, str) => results.Add($"{num}-{str}"));

            Assert.Equal(new[] { "1-one", "2-two" }, results);
        }

        /// <summary>
        /// AlignEach 方法基本功能测试。
        /// </summary>
        [Fact]
        public void AlignEach_ShouldExecuteActionForMatchingElements()
        {
            var people = new List<DistinctA>
            {
                new DistinctA { Id = 1, Name = "Alice" },
                new DistinctA { Id = 2, Name = "Bob" },
                new DistinctA { Id = 3, Name = "Charlie" }
            };
            var order = new List<int> { 3, 1 };
            var results = new List<string>();

            people.AlignEach(order, p => p.Id, p => results.Add(p.Name));

            Assert.Equal(new[] { "Charlie", "Alice" }, results);
        }

        /// <summary>
        /// JoinEach 方法基本功能测试。
        /// </summary>
        [Fact]
        public void JoinEachTest()
        {
            var array1 = new List<int> { 1, 2, 3 };
            var array2 = new List<DistinctA>
            {
                new DistinctA { Id = 1, Name = "One" },
                new DistinctA { Id = 2, Name = "Two" },
                new DistinctA { Id = 3, Name = "Three" },
                new DistinctA { Id = 4, Name = "Four" } // 这个不会被匹配
            };

            var results = new List<string>();

            array1.JoinEach(array2, x => x, y => y.Id, (x, y) =>
            {
                Assert.Equal(x, y.Id);
                results.Add($"{x}-{y.Name}");
            });

            Assert.Equal(new[] { "1-One", "2-Two", "3-Three" }, results);
        }

        /// <summary>
        /// JoinEach 方法空参数测试。
        /// </summary>
        [Fact]
        public void JoinEach_NullParameters_ShouldThrowArgumentNullException()
        {
            var outer = new List<int> { 1, 2, 3 };
            var inner = new List<string> { "a", "b", "c" };

            // 测试outer为null
            Assert.Throws<ArgumentNullException>(() => 
                ((IEnumerable<int>)null).JoinEach<int, string, int>(inner, x => x, y => y.Length, (x, y) => { }));

            // 测试inner为null
            Assert.Throws<ArgumentNullException>(() => 
                outer.JoinEach<int, string, int>(null, x => x, y => y.Length, (x, y) => { }));

            // 测试outerKeySelector为null
            Assert.Throws<ArgumentNullException>(() => 
                outer.JoinEach<int, string, int>(inner, null, y => y.Length, (x, y) => { }));

            // 测试innerKeySelector为null
            Assert.Throws<ArgumentNullException>(() => 
                outer.JoinEach<int, string, int>(inner, x => x, null, (x, y) => { }));

            // 测试eachIterator为null
            Assert.Throws<ArgumentNullException>(() => 
                outer.JoinEach<int, string, int>(inner, x => x, y => y.Length, null));
        }

        #endregion

        #region 边界情况和性能测试

        /// <summary>
        /// 大数据量性能测试。
        /// </summary>
        [Fact]
        public void Performance_LargeDataSet_ShouldComplete()
        {
            var largeList = Enumerable.Range(1, 10000).ToList();
            var order = Enumerable.Range(1, 10000).Reverse().ToList();

            var result = largeList.AlignOverall(order).ToList();

            Assert.Equal(10000, result.Count);
            Assert.Equal(order, result);
        }

        /// <summary>
        /// 复杂对象Distinct测试。
        /// </summary>
        [Fact]
        public void Distinct_ComplexObjects_ShouldWork()
        {
            var objects = new List<DistinctA>
            {
                new DistinctA { Id = 1, Name = "Alice", Date = DateTime.Today },
                new DistinctA { Id = 1, Name = "Alice", Date = DateTime.Today.AddDays(1) }, // 相同Id和Name，不同Date
                new DistinctA { Id = 2, Name = "Bob", Date = DateTime.Today },
                new DistinctA { Id = 1, Name = "Alice", Date = DateTime.Today.AddDays(2) } // 又一个相同Id和Name
            };

            var result = objects.Distinct(x => new { x.Id, x.Name }).ToList();

            Assert.Equal(2, result.Count);
            Assert.Contains(result, x => x.Id == 1 && x.Name == "Alice");
            Assert.Contains(result, x => x.Id == 2 && x.Name == "Bob");
        }

        /// <summary>
        /// 嵌套集合ForEach测试。
        /// </summary>
        [Fact]
        public void ForEach_NestedCollections_ShouldWork()
        {
            var nestedList = new List<List<int>>
            {
                new List<int> { 1, 2, 3 },
                new List<int> { 4, 5, 6 },
                new List<int> { 7, 8, 9 }
            };

            var allNumbers = new List<int>();

            nestedList.ForEach(innerList => 
                innerList.ForEach(num => allNumbers.Add(num)));

            Assert.Equal(new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 }, allNumbers);
        }

        /// <summary>
        /// Join方法特殊字符测试。
        /// </summary>
        [Fact]
        public void Join_SpecialCharacters_ShouldWork()
        {
            var list = new List<object> { "hello", "world", "!", "@#$%", 123 };
            var result = list.Join(" 🌟 ");

            Assert.Equal("hello 🌟 world 🌟 ! 🌟 @#$% 🌟 123", result);
        }

        /// <summary>
        /// 多次Align操作测试。
        /// </summary>
        [Fact]
        public void Align_MultipleOperations_ShouldWork()
        {
            var source = new List<DistinctA>
            {
                new DistinctA { Id = 1, Name = "A" },
                new DistinctA { Id = 2, Name = "B" },
                new DistinctA { Id = 3, Name = "C" }
            };

            var firstOrder = new List<int> { 3, 1, 2 };
            var secondOrder = new List<int> { 2, 3, 1 };

            var firstResult = source.Align(firstOrder, x => x.Id).ToList();
            var secondResult = firstResult.Align(secondOrder, x => x.Id).ToList();

            Assert.Equal("B", secondResult[0].Name);
            Assert.Equal("C", secondResult[1].Name);
            Assert.Equal("A", secondResult[2].Name);
        }

        #endregion
    }
}
