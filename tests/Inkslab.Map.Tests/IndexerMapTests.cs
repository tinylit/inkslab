using Xunit;
using System.Collections.Generic;

#pragma warning disable CS1591

namespace Inkslab.Map.Tests
{
    public class IndexerMapTests
    {
        [Fact]
        public void DefaultMappingIgnoresIndexersAndMapsOrdinaryProperties()
        {
            var source = new SourceWithIndexer { Name = "mapped" };
            source[0] = "index-value";

            using var mapper = new MapperInstance();
            var result = mapper.Map<DestinationWithIndexer>(source);

            Assert.Equal("mapped", result.Name);
            Assert.Null(result[0]);
        }

        [Fact]
        public void KeyValueMappingIgnoresDestinationIndexers()
        {
            var source = new Dictionary<string, object> { [nameof(DestinationWithIndexer.Name)] = "mapped" };

            using var mapper = new MapperInstance();
            var result = mapper.Map<DestinationWithIndexer>(source);

            Assert.Equal("mapped", result.Name);
            Assert.Null(result[0]);
        }

        private sealed class SourceWithIndexer
        {
            private string _value;

            public string Name { get; set; }

            public string this[int index]
            {
                get => _value;
                set => _value = value;
            }
        }

        private sealed class DestinationWithIndexer
        {
            private string _value;

            public string Name { get; set; }

            public string this[int index]
            {
                get => _value;
                set => _value = value;
            }
        }
    }
}
