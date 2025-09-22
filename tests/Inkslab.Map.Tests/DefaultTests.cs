using Inkslab.Map.Maps;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
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
        private readonly A _a;

        /// <summary>
        /// <inheritdoc/>.
        /// </summary>
        /// <param name="a"><inheritdoc/>.</param>
        public B(A a)
        {
            _a = a ?? throw new ArgumentNullException(nameof(a));
        }

        /// <summary>
        /// B1.
        /// </summary>
        public int B1 => _a.A1;

        /// <summary>
        /// B2.
        /// </summary>
        public string B2 => _a.A2;

        /// <summary>
        /// B3.
        /// </summary>
        public DateTime B3 => _a.A3;
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
    /// 用户角色。
    /// </summary>
    [Flags]
    public enum EnumUserRole
    {
        /// <summary>
        /// 用户。
        /// </summary>
        User = 1 << 0,
        /// <summary>
        /// 员工。
        /// </summary>
        Worker = 1 << 1,
        /// <summary>
        /// 老板。
        /// </summary>
        Boss = 1 << 2,
        /// <summary>
        /// 业务员。
        /// </summary>
        Salesman = 1 << 3,
        /// <summary>
        /// 电销。
        /// </summary>
        Telesales = 1 << 4,
        /// <summary>
        /// 客服。
        /// </summary>
        Service = 1 << 5,
        /// <summary>
        /// (内部或外部)开发者。
        /// </summary>
        Developer = 1 << 6,
        /// <summary>
        /// 医生。
        /// </summary>
        Doctor = 1 << 7,
        /// <summary>
        /// 药师。
        /// </summary>
        Pharmacist = 1 << 8,
        /// <summary>
        /// 专家。
        /// </summary>
        Specialist = 1 << 9,
        /// <summary>
        /// 管理员。
        /// </summary>
        Administrator = 1 << 10
    }

    /// <summary>
    /// 管理员
    /// </summary>
    public class YKAdministrator
    {
        /// <summary>
        /// 认证中心Id。
        /// </summary>
        public long Id { get; set; }
        /// <summary>
        /// 用户Id
        /// </summary>
        public long UserId { get; set; }
        /// <summary>
        /// 用户名称
        /// </summary>
        public string UserName { get; set; }
        /// <summary>
        /// 角色
        /// </summary>
        public EnumUserRole Role { get; set; }
        /// <summary>
        /// 事业部
        /// </summary>
        public long BusinessId { get; set; }
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
    /// 递归测试。
    /// </summary>
    public class Recursive
    {
        /// <summary>
        /// 主键。
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// 名称。
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 子节点。
        /// </summary>
        public List<Recursive> Childrens { get; set; }
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
        /// <see cref="FromKeyIsStringValueIsAnyMap"/>.
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

            Assert.True(c1.I5 == 7042011313840586752L);
        }

        /// <summary>
        /// <see cref="FromKeyIsStringValueIsAnyMap"/>.
        /// </summary>
        [Fact]
        public void MapFromDictionaryByString()
        {
            using var instance = new MapperInstance();

            //? 通过【TryGetValue】方法，获得数据则进行映射，否则不映射。
            var dic = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["i5"] = "7042011313840586752",
                ["p3"] = "2023-10-12",
                ["p2"] = "test",
                ["b1"] = "False",
                ["p1"] = "1",
                ["d4"] = "1"
            };

            var c1 = instance.Map<C2>(dic);

            Assert.True(c1.I5 == 7042011313840586752L);
        }

        /// <summary>
        /// <see cref="FromKeyIsStringValueIsAnyMap"/>.
        /// </summary>
        [Fact]
        public void MapFromDictionaryByLong()
        {
            using var instance = new MapperInstance();

            //? 通过【TryGetValue】方法，获得数据则进行映射，否则不映射。
            var dic = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase)
            {
                ["i5"] = 7042011313840586752,
                ["p3"] = DateTime.Now.Ticks,
                ["p2"] = 100,
                ["p1"] = 1,
                ["d4"] = 1
            };

            var c1 = instance.Map<C1>(dic);

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

        /// <summary>
        /// 递归测试。
        /// </summary>
        [Fact]
        public void RecursiveTest()
        {
            var source = new Recursive
            {
                Id = 1,
                Name = "测试1",
                Childrens = new List<Recursive>
                {
                    new Recursive
                    {
                        Id = 2,
                        Name = "测试2"
                    },
                    new Recursive
                    {
                        Id = 3,
                        Name = "测试3"
                    }
                }
            };

            using var instance = new MapperInstance();

            //? 不支持递归关系处理。
            Assert.Throws<NotSupportedException>(() => instance.Map<Recursive>(source));
        }

        /// <summary>
        /// 数组到数组的测试。
        /// </summary>
        [Fact]
        public void ArrayToArray()
        {
            var sourceC = new C
            {
                A1 = 100,
                A2 = "Test",
                A3 = DateTime.Now
            };

            var sourceArr = new C[] { sourceC };

            var destinationArr = Mapper.Map<D[]>(sourceArr);

            var destinationD = destinationArr.Single();

            Assert.True(destinationD.A1 == sourceC.A1);
            Assert.True(destinationD.A2 == sourceC.A2);
            Assert.True(destinationD.A3 == sourceC.A3);
        }

        /// <summary>
        /// 数组中存在为 Null 的原生到数组的映射。
        /// </summary>
        [Fact]
        public void ArrayWithNullItemToArray()
        {
            var sourceC = new C
            {
                A1 = 100,
                A2 = "Test",
                A3 = DateTime.Now
            };

            var sourceArr = new C[] { null, sourceC };

            var destinationArr = Mapper.Map<D[]>(sourceArr);

            var destinationD = destinationArr.Single();

            Assert.True(destinationD.A1 == sourceC.A1);
            Assert.True(destinationD.A2 == sourceC.A2);
            Assert.True(destinationD.A3 == sourceC.A3);
        }

        /// <summary>
        /// 数组中存在为 Null 的原生到集合的映射。
        /// </summary>
        [Fact]
        public void ArrayWithNullItemToCollect()
        {
            var sourceC = new C
            {
                A1 = 100,
                A2 = "Test",
                A3 = DateTime.Now
            };

            var sourceArr = new C[] { null, sourceC };

            var destinationArr = Mapper.Map<IEnumerable<D>>(sourceArr);

            var destinationD = destinationArr.Single();

            Assert.True(destinationD.A1 == sourceC.A1);
            Assert.True(destinationD.A2 == sourceC.A2);
            Assert.True(destinationD.A3 == sourceC.A3);
        }

        /// <summary>
        /// <see cref="FromKeyIsStringValueIsAnyMap"/>.
        /// </summary>
        [Fact]
        public void MapFromDictionaryByEnum()
        {
            using var instance = new MapperInstance();

            //? 通过【TryGetValue】方法，获得数据则进行映射，否则不映射。
            var dic = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
            {
                {"id",123123123123123L},
                {"role", "Administrator"},
                { "UserId", 123123123123123L },
                { "UserName", "redis.UserName" },
                { "BusinessId", 1000L },
            };

            var c1 = instance.Map<YKAdministrator>(dic);

            Assert.True(c1.Role == EnumUserRole.Administrator);
        }

        /// <summary>
        /// 字符串到布尔值的映射测试。
        /// </summary>
        [Fact]
        public void TestString2Bool()
        {
            using var instance = new MapperInstance();

            var c1 = instance.Map<bool>("1");
            Assert.True(c1);
            var c2 = instance.Map<bool>("1.0");
            Assert.True(c2);
            var c3 = instance.Map<bool>("true");
            Assert.True(c3);
            var c4 = instance.Map<bool>("True");
            Assert.True(c4);
            var c5 = instance.Map<bool>("TRUE");
            Assert.True(c5);
            var c6 = instance.Map<bool>("0");
            Assert.False(c6);
            var c7 = instance.Map<bool>("0.0");
            Assert.False(c7);
            var c8 = instance.Map<bool>("false");
            Assert.False(c8);
            var c9 = instance.Map<bool>("False");
            Assert.False(c9);
            var c10 = instance.Map<bool>("FALSE");
            Assert.False(c10);
        }
    }
}