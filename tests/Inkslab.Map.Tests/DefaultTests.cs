using Inkslab.Map.Maps;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

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
    /// <inheritdoc/>.
    /// </summary>
    public class E
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

        /// <summary>
        /// A4
        /// </summary>
        public D A4 { get; set; }
    }

    /// <summary>
    /// <inheritdoc/>.
    /// </summary>
    public class F
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

        /// <summary>
        /// A4
        /// </summary>
        public A A4 { get; set; } = new A();
    }

    /// <summary>
    /// ҵ����
    /// </summary>
    public class LineOfBusinessOutDto
    {
        /// <summary>
        /// Id
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// ParentId
        /// </summary>
        public long ParentId { get; set; }

        /// <summary>
        /// Code
        /// </summary>
        public string Code { get; set; }

        /// <summary>
        /// Name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Slogan
        /// </summary>
        public string Slogan { get; set; }

        /// <summary>
        /// Logo
        /// </summary>
        public string Logo { get; set; }

        /// <summary>
        /// IsEnabled
        /// </summary>
        public bool IsEnabled { get; set; }

        /// <summary>
        /// AllocationProgramEntry
        /// </summary>
        public string AllocationProgramEntry { get; set; }

        /// <summary>
        /// SkinColour
        /// </summary>
        public int SkinColour { get; set; }

        /// <summary>
        /// SkinColourTxt
        /// </summary>
        public string SkinColourTxt { get; set; }
    }

    /// <summary>
    /// DefaultTests
    /// </summary>
    public class DefaultTests
    {
        /// <summary>
        /// DefaultTests
        /// </summary>
        public DefaultTests()
        {
            //+ �������ã����Nuget���򹤳����ü���ʹ�á�
            using (var startup = new XStartup())
            {
                startup.DoStartup();
            }
        }

        /// <summary>
        /// NullableTest
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
        /// <see cref="ConvertMap"/>��
        /// </summary>
        [Fact]
        public void ConvertTest()
        {
            var now = DateTime.Now;

            var date = Mapper.Map<DateTime>(now.ToString("yyyy-MM-dd"));

            Assert.True(now.Year == date.Year && now.Month == date.Month && now.Day == date.Day);
        }

        /// <summary>
        /// <see cref="ToStringMap"/>��
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

            var sourceDic = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
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

        /// <summary>
        /// MapPropertyIsNull
        /// </summary>
        [Fact]
        public void MapPropertyIsNull()
        {
            var e = new E
            {
                A1 = 1,
                A2 = "2",
                A3 = DateTime.Now
            };

            var f = Mapper.Map<F>(e);

            Assert.False(f.A4 is null);
        }

        /// <summary>
        /// RPC
        /// </summary>
        [Fact]
        public void TestRpc()
        {
            var r = new Basics.Rpc.Read.LineOfBusinessRpc.LineOfBusinessRpcDto();

            var f = Mapper.Map<LineOfBusinessOutDto>(r);
        }

        /// <summary>
        /// MapFromDictionary
        /// </summary>
        [Fact]
        public void MapFromIEnumerable()
        {
            using var instance = new MapperInstance();

            //? 优先按照完全名称匹配，如果匹配补上，会按照命名规则转换后进行匹配。
            var dic = new List<KeyValuePair<string, object>>
            {
                new("i5", "7042011313840586752"),
                new("p3", "2023-10-12"),
                new("p2", "test"),
                new("p1", DateTimeKind.Utc),
                new("d4", 1)
            };

            var c1 = instance.Map<C2>(dic);

            Assert.True(c1.I5 == 7042011313840586752L);
        }

        /// <summary>
        /// MapFromDictionary
        /// </summary>
        [Fact]
        public void MapFromDictionary()
        {
            using var instance = new MapperInstance();

            //? 通过【TryGetValue】方法，获得数据则进行映射，否则不映射。
            var dic = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
            {
                ["i5"] = "7042011313840586752",
                ["p3"] = "2023-10-12",
                ["p2"] = "test",
                ["p1"] = DateTimeKind.Utc,
                ["d4"] = "1"
            };

            var c1 = instance.Map<C2>(dic);

            var c2 = instance.Map<object>(1);

            Assert.True(c1.I5 == 7042011313840586752L);
        }

        /// <summary>
        /// MapToDictionary
        /// </summary>
        [Fact]
        public void MapToDictionary()
        {
            using var instance = new MapperInstance();

            var sourceC1 = new C1
            {
                P1 = 1,
                P2 = "Test",
                P3 = DateTime.Now,
                I5 = 10000
            };

            var dic = instance.Map<Dictionary<string, object>>(sourceC1);

            Assert.True(Convert.ToInt64(dic["I5"]) == sourceC1.I5);
        }
    }
}