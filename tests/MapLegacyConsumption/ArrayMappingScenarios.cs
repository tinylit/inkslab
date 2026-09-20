using System;
using System.Collections.Generic;
using System.Linq;
using Inkslab.Map;
using Inkslab.Map.Expressions;

#pragma warning disable CS1591

namespace MapCompatibility
{
    public static class ArrayMappingScenarios
    {
        public static void ReferenceArrayWithNulls(bool allowNulls)
        {
            using var mapper = CreateMapper(allowNulls);
            var source = new[] { null, new Source { Value = 3 }, null, new Source { Value = 5 }, null };
            var result = mapper.Map<Destination[]>(source);
            Equal(allowNulls ? new long?[] { null, 3, null, 5, null } : new long?[] { 3, 5 },
                result.Select(x => x?.Value));
        }

        public static void ReferenceArrayWithoutNulls(bool allowNulls)
        {
            using var mapper = CreateMapper(allowNulls);
            var result = mapper.Map<Destination[]>(new[] { new Source { Value = 3 }, new Source { Value = 5 } });
            Equal(new long[] { 3, 5 }, result.Select(x => x.Value));
        }

        public static void ReferenceArrayAllNulls(bool allowNulls)
        {
            using var mapper = CreateMapper(allowNulls);
            var result = mapper.Map<Destination[]>(new Source[] { null, null });
            Equal(allowNulls ? new Destination[] { null, null } : Array.Empty<Destination>(), result);
        }

        public static void NullableArrayWithNulls(bool allowNulls)
        {
            using var mapper = CreateMapper(allowNulls);
            var result = mapper.Map<int?[]>(new int?[] { null, 3, null, 5, null });
            Equal(allowNulls ? new int?[] { null, 3, null, 5, null } : new int?[] { 3, 5 }, result);
        }

        public static void NullableArrayWithoutNulls(bool allowNulls)
        {
            using var mapper = CreateMapper(allowNulls);
            var result = mapper.Map<int?[]>(new int?[] { 3, 5 });
            Equal(new int?[] { 3, 5 }, result);
        }

        public static void NullableArrayToValues(bool allowNulls)
        {
            using var mapper = CreateMapper(allowNulls);
            var result = mapper.Map<long[]>(new int?[] { null, 3, null, 5, null });
            Equal(allowNulls ? new long[] { 0, 3, 0, 5, 0 } : new long[] { 3, 5 }, result);
        }

        public static void IntegerArray(bool allowNulls)
        {
            using var mapper = CreateMapper(allowNulls);
            Equal(new long[] { 3, 5 }, mapper.Map<long[]>(new[] { 3, 5 }));
            Equal(Array.Empty<long>(), mapper.Map<long[]>(Array.Empty<int>()));
        }

        public static void Scalar(bool allowNulls)
        {
            using var mapper = CreateMapper(allowNulls);
            if (mapper.Map<long>(3) != 3L || mapper.Map<long>("5") != 5L)
            {
                throw new InvalidOperationException("Scalar conversion returned an unexpected value.");
            }
        }

        private static ConfiguredMapper CreateMapper(bool allowNulls) => new ConfiguredMapper(
            new MapConfiguration(new Configuration { AllowPropagationNullValues = allowNulls }));

        private static void Equal<T>(IEnumerable<T> expected, IEnumerable<T> actual)
        {
            if (!expected.SequenceEqual(actual))
            {
                throw new InvalidOperationException("Expected [" + string.Join(", ", expected)
                    + "]; actual [" + string.Join(", ", actual) + "].");
            }
        }

        private sealed class ConfiguredMapper : ProfileExpression<ConfiguredMapper, MapConfiguration>
        {
            public ConfiguredMapper(MapConfiguration configuration) : base(configuration) { }
        }

        private sealed class Source
        {
            public int Value { get; set; }
        }

        private sealed class Destination
        {
            public long Value { get; set; }
        }
    }
}
