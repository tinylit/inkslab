using Inkslab.Map;
using Xunit;

namespace Inkslab.Map.Tests
{
    /// <summary>
    /// 仅含可选参数构造函数的目标类型。
    /// </summary>
    public class OptionalCtorDest
    {
        /// <summary>
        /// 仅含可选参数的构造函数（无无参构造函数）。
        /// </summary>
        /// <param name="version">版本。</param>
        public OptionalCtorDest(int version = 1) => Version = version;

        /// <summary>
        /// 版本。
        /// </summary>
        public int Version { get; set; }

        /// <summary>
        /// 名称。
        /// </summary>
        public string Name { get; set; }
    }

    /// <summary>
    /// 映射源类型。
    /// </summary>
    public class OptionalCtorSource
    {
        /// <summary>
        /// 版本。
        /// </summary>
        public int Version { get; set; }

        /// <summary>
        /// 名称。
        /// </summary>
        public string Name { get; set; }
    }

    /// <summary>
    /// 可选参数构造函数映射回归测试。
    /// </summary>
    public class OptionalCtorMapTests
    {
        /// <summary>
        /// 目标类型仅含"全可选参数构造函数"时应可正常映射。
        /// </summary>
        [Fact]
        public void Map_ToTypeWithOnlyOptionalArgsConstructor_Succeeds()
        {
            using var instance = new MapperInstance();

            var source = new OptionalCtorSource { Version = 7, Name = "abc" };

            var dest = instance.Map<OptionalCtorDest>(source);

            Assert.Equal(7, dest.Version);
            Assert.Equal("abc", dest.Name);
        }
    }
}
