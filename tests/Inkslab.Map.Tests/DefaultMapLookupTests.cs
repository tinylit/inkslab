#pragma warning disable CS1591
using Inkslab;
using Xunit;

namespace Inkslab.Map.Tests
{
    /// <summary>
    /// DefaultMap property-name index optimization behavior consistency tests.
    /// </summary>
    public class DefaultMapLookupTests
    {
        public class SourceA
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public string UPPERCASE { get; set; }
            public string Extra { get; set; }
        }

        public class TargetA
        {
            public int Id { get; set; }
            public string Name { get; set; }
            //? Case-insensitive match: source UPPERCASE -> target UpperCase.
            public string UpperCase { get; set; }
        }

        /// <summary>
        /// Initialize framework by running XStartup so Mapper singleton can resolve IMapper.
        /// </summary>
        public DefaultMapLookupTests()
        {
            using var startup = new XStartup();
            startup.DoStartup();
        }

        /// <summary>
        /// Verifies DefaultMap still matches property names case-insensitively.
        /// </summary>
        [Fact]
        public void Map_MatchesPropertyNamesIgnoreCase()
        {
            var src = new SourceA
            {
                Id = 7,
                Name = "hello",
                UPPERCASE = "world",
                Extra = "ignored"
            };

            var dst = Mapper.Map<TargetA>(src);

            Assert.NotNull(dst);
            Assert.Equal(7, dst.Id);
            Assert.Equal("hello", dst.Name);
            Assert.Equal("world", dst.UpperCase);
        }
    }
}
