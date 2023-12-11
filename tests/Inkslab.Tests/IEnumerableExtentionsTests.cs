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
    }

    /// <summary>
    /// <see cref="System.Collections.EnumerableExtensions"/> 和 <see cref="System.Collections.Generic.EnumerableExtensions"/> 测试。
    /// </summary>
    public class EnumerableExtensionsTests
    {
        /// <summary>
        /// 合并。
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

            var e = "1,2,xzy";
            var r = list.Join(","); //? 自动忽略 null 值。

            Assert.Equal(e, r);
        }

        /// <summary>
        /// 去重。
        /// </summary>
        [Fact]
        public void DistinctTest()
        {
            var list = new List<DistinctA>();

            var r = new Random();

            for (int i = 0, len = 50; i < len; i++)
            {
                list.Add(new DistinctA
                {
                    Id = r.Next(len),
                    Name = i.ToString(),
                    Date = DateTime.Now
                });
            }

            var hashSet = new HashSet<int>();

            foreach (var item in list.Distinct(x => x.Id))
            {
                if (hashSet.Add(item.Id))
                {
                    continue;
                }

                Assert.False(true);
            }
        }

        /// <summary>
        /// 内容对齐。
        /// </summary>
        [Fact]
        public void AlignTest()
        {
            var array1 = new List<int> { 1, 2, 3, 4, 5, 6, 7 };
            var array2 = new List<int> { 4, 5, 1, 2, 3, 6, 7 };

            var array3 = array2
                .AlignOverall(array1)
                .ToList();

            array3.ZipEach(array1, (x, y) =>
            {
                Assert.Equal(x, y);
            });
        }
    }
}
