using System;
using Xunit;
using Inkslab.Map.Maps;
using System.Text;
using System.Collections.Generic;

namespace Inkslab.Map.Tests
{
    /// <summary>
    /// <inheritdoc/>.
    /// </summary>
    public class A
    {
        /// <summary>
        /// A1.
        /// </summary>
        public int A1 { get; set; }
        /// <summary>
        /// A2.
        /// </summary>
        public string A2 { get; set; }
        /// <summary>
        /// A3.
        /// </summary>
        public DateTime A3 { get; set; }
    }

    /// <summary>
    /// <inheritdoc/>.
    /// </summary>
    public class B
    {
        private readonly A a;

        /// <summary>
        /// <inheritdoc/>.
        /// </summary>
        /// <param name="a"><inheritdoc/>.</param>
        public B(A a)
        {
            this.a = a ?? throw new ArgumentNullException(nameof(a));
        }

        /// <summary>
        /// B1.
        /// </summary>
        public int B1 => a.A1;
        /// <summary>
        /// B2.
        /// </summary>
        public string B2 => a.A2;
        /// <summary>
        /// B3.
        /// </summary>
        public DateTime B3 => a.A3;
    }

    /// <summary>
    /// <inheritdoc/>.
    /// </summary>
    public class C : A, ICloneable
    {
        /// <summary>
        /// <inheritdoc/>.
        /// </summary>
        public object Clone() => new C
        {
            A1 = A1,
            A2 = A2,
            A3 = A3
        };
    }

    /// <summary>
    /// <inheritdoc/>.
    /// </summary>
    public class D
    {
        /// <summary>
        /// A1.
        /// </summary>
        public long A1 { get; set; }
        /// <summary>
        /// A2.
        /// </summary>
        public string A2 { get; set; }
        /// <summary>
        /// A3.
        /// </summary>
        public DateTime? A3 { get; set; }
    }

    /// <summary>
    /// 默认测试。
    /// </summary>
    public class DefaultTests
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        public DefaultTests()
        {
            //+ 引包即用：添加Nuget包或工程引用即可使用。
            using (var xstartup = new XStartup())
            {
                xstartup.DoStartup();
            }
        }

        /// <summary>
        /// 自动处理所有可空类型。
        /// </summary>
        [Fact]
        public void NullableTest()
        {
            var now = DateTime.Now;

            var date = Mapper.Map<DateTime?>(now);
            var date2 = (DateTime?)Mapper.Map(now.ToString("yyyy-MM-dd"), typeof(DateTime?));

            Assert.True(date.HasValue && date2.HasValue && date?.Date == date2);
        }

        /// <summary>
        /// <see cref="ConvertMap"/>。
        /// </summary>
        [Fact]
        public void ConvertTest()
        {
            var now = DateTime.Now;

            var date = Mapper.Map<DateTime>(now.ToString("yyyy-MM-dd"));

            Assert.True(now.Year == date.Year && now.Month == date.Month && now.Day == date.Day);
        }

        /// <summary>
        /// <see cref="ToStringMap"/>。
        /// </summary>
        [Fact]
        public void ToStringTest()
        {
            var newId = Guid.NewGuid().ToString("N");

            var sb = new StringBuilder();

            sb.Append(newId);

            var mapId = Mapper.Map<string>(newId);

            Assert.Equal(newId, mapId);
        }

        /// <summary>
        /// <see cref="ParseStringMap"/>.
        /// </summary>
        [Fact]
        public void ParseStringTest()
        {
            var newId = Guid.NewGuid();

            var mapId = Mapper.Map<Guid>(newId.ToString("N"));

            Assert.Equal(newId, mapId);
        }

        /// <summary>
        /// <see cref="EnumUnderlyingTypeMap"/>.
        /// </summary>
        [Fact]
        public void EnumUnderlyingTypeTest()
        {
            var timeKind = DateTimeKind.Local;

            var mapTimeKind = Mapper.Map<DateTimeKind>(timeKind.ToInt32());

            Assert.Equal(timeKind, mapTimeKind);
        }

        /// <summary>
        /// <see cref="StringToEnumMap"/>.
        /// </summary>
        [Fact]
        public void StringToEnumTest()
        {
            var timeKind = DateTimeKind.Local;

            var mapTimeKind = Mapper.Map<DateTimeKind>(timeKind.ToString());

            Assert.Equal(timeKind, mapTimeKind);
        }

        /// <summary>
        /// <see cref="KeyValueMap"/>.
        /// </summary>
        [Fact]
        public void KeyValueTest()
        {
            var sourceKv = new KeyValuePair<string, object>("key", DateTime.Now);

            var destinationKv = Mapper.Map<KeyValuePair<string, DateTime>>(sourceKv);

            Assert.True(sourceKv.Key == destinationKv.Key && Equals(sourceKv.Value, destinationKv.Value));
        }

        /// <summary>
        /// <see cref="ConstructorMap"/>.
        /// </summary>
        [Fact]
        public void ConstructorTest()
        {
            var sourceA = new A
            {
                A1 = 100,
                A2 = "Test",
                A3 = DateTime.Now
            };

            var destinationB = Mapper.Map<B>(sourceA);

            Assert.True(sourceA.A1 == destinationB.B1 && sourceA.A2 == destinationB.B2 && sourceA.A3 == destinationB.B3);
        }

        /// <summary>
        /// <see cref="FromKeyIsStringValueIsObjectMap"/>.
        /// </summary>
        [Fact]
        public void FromKeyIsStringValueIsObjectTest()
        {
            var now = DateTime.Now;

            var sourceDic = new Dictionary<string, object>
            {
                { "a1", DateTimeKind.Utc },
                { "A2", "Test" },
                { "a3", now }
            };

            var destinationA = Mapper.Map<A>(sourceDic);

            Assert.True(destinationA.A1 == DateTimeKind.Utc.ToInt32());
            Assert.True(destinationA.A2 == "Test");
            Assert.True(destinationA.A3 == now);
        }

        /// <summary>
        /// <see cref="ToKeyIsStringValueIsObjectMap"/>.
        /// </summary>
        [Fact]
        public void ToKeyIsStringValueIsObjectTest()
        {
            var sourceA = new A
            {
                A1 = 100,
                A2 = "Test",
                A3 = DateTime.Now
            };

            var destinationDic = Mapper.Map<IDictionary<string, object>>(sourceA);

            Assert.True(Equals(destinationDic["A1"], sourceA.A1));
            Assert.True(Equals(destinationDic["A2"], sourceA.A2));
            Assert.True(Equals(destinationDic["A3"], sourceA.A3));
        }

        /// <summary>
        /// <see cref="CloneableMap"/>.
        /// </summary>
        [Fact]
        public void CloneableTest()
        {
            var sourceC = new C
            {
                A1 = 100,
                A2 = "Test",
                A3 = DateTime.Now
            };

            var destinationA = Mapper.Map<A>(sourceC);

            Assert.True(destinationA.A1 == sourceC.A1);
            Assert.True(destinationA.A2 == sourceC.A2);
            Assert.True(destinationA.A3 == sourceC.A3);
        }

        /// <summary>
        /// <see cref="DefaultMap"/>.
        /// </summary>
        [Fact]
        public void DefaultTest()
        {
            var sourceC = new C
            {
                A1 = 100,
                A2 = "Test",
                A3 = DateTime.Now
            };

            var destinationD = Mapper.Map<D>(sourceC);

            Assert.True(destinationD.A1 == sourceC.A1);
            Assert.True(destinationD.A2 == sourceC.A2);
            Assert.True(destinationD.A3 == sourceC.A3);
        }

        /// <summary>
        /// <see cref="EnumerableMap"/>.
        /// </summary>
        [Fact]
        public void EnumerableTest()
        {
            var sourceList = new List<C>
            {
                new C
                {
                    A1 = 100,
                    A2 = "Test",
                    A3 = DateTime.Now
                },

                new C
                {
                    A1 = 100,
                    A2 = "Test",
                    A3 = DateTime.Now
                },
                new C
                {
                    A1 = 100,
                    A2 = "Test",
                    A3 = DateTime.Now
                }
            };

            var destinationHashSet = Mapper.Map<HashSet<C>>(sourceList);

            Assert.True(destinationHashSet.Count == sourceList.Count);
        }
    }
}
