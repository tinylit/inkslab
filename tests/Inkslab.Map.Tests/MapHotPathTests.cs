using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

#pragma warning disable CS1591

namespace Inkslab.Map.Tests
{
    public class MapHotPathTests
    {
        [Fact]
        public void CachedScalarDoesNotAllocate()
        {
            using var mapper = new MapperInstance();
            object source = 42;
            for (int i = 0; i < 100; i++) { mapper.Map<long>(source); }
            long start = GC.GetAllocatedBytesForCurrentThread();
            long sum = 0;
            for (int i = 0; i < 1000; i++) { sum += mapper.Map<long>(source); }
            long allocated = GC.GetAllocatedBytesForCurrentThread() - start;
            Assert.Equal(42000, sum);
            Assert.Equal(0, allocated);
        }

        [Fact]
        public void CachedReferenceDoesNotAllocate()
        {
            using var mapper = new CountingMapper();
            object source = 42;
            mapper.Map<string>(source);
            long start = GC.GetAllocatedBytesForCurrentThread();
            string result = null;
            for (int i = 0; i < 1000; i++) { result = mapper.Map<string>(source); }
            long allocated = GC.GetAllocatedBytesForCurrentThread() - start;
            Assert.Equal("mapped", result);
            Assert.Equal(0, allocated);
        }

        [Fact]
        public void ValueListToArrayAllocatesOnlyResult()
        {
            using var mapper = new MapperInstance();
            var source = Enumerable.Range(0, 1000).ToList();
            for (int i = 0; i < 100; i++) { mapper.Map<long[]>(source); }
            long start = GC.GetAllocatedBytesForCurrentThread();
            var result = mapper.Map<long[]>(source);
            long allocated = GC.GetAllocatedBytesForCurrentThread() - start;
            Assert.Equal(1000, result.Length);
            Assert.Equal(999, result[999]);
            Assert.InRange(allocated, 8000, 8100);
        }

        [Fact]
        public void ValueListToListAvoidsGrowthAndEnumeratorAllocations()
        {
            using var mapper = new MapperInstance();
            var source = Enumerable.Range(0, 1000).ToList();
            for (int i = 0; i < 100; i++) { mapper.Map<List<long>>(source); }
            long start = GC.GetAllocatedBytesForCurrentThread();
            var result = mapper.Map<List<long>>(source);
            long allocated = GC.GetAllocatedBytesForCurrentThread() - start;
            Assert.Equal(1000, result.Count);
            Assert.Equal(999, result[999]);
            Assert.InRange(allocated, 8000, 8100);
        }

        [Fact]
        public void AllNullListDoesNotAllocateBySourceCount()
        {
            using var mapper = new MapperInstance();
            var source = Enumerable.Repeat<string>(null, 10000).ToList();
            mapper.Map<string[]>(source);
            long start = GC.GetAllocatedBytesForCurrentThread();
            var result = mapper.Map<string[]>(source);
            long allocated = GC.GetAllocatedBytesForCurrentThread() - start;
            Assert.Empty(result);
            Assert.InRange(allocated, 0, 256);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task FirstConcurrentAccessBuildsOnlyOnceAsync(bool reference)
        {
            using var mapper = new CountingMapper();
            await Task.WhenAll(Enumerable.Range(0, 32).Select(_ => Task.Run(() =>
            {
                if (reference) { Assert.Equal("mapped", mapper.Map<string>(42)); }
                else { Assert.Equal(42, mapper.Map<long>(42)); }
            })));
            Assert.Equal(1, mapper.Builds);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void FailedCompilationIsCached(bool reference)
        {
            using var mapper = new CountingMapper { Fail = true };
            void map() { if (reference) { mapper.Map<string>(42); } else { mapper.Map<long>(42); } }
            var first = Assert.Throws<InvalidOperationException>(map);
            var second = Assert.Throws<InvalidOperationException>(map);
            Assert.Same(first, second);
            Assert.Equal(1, mapper.Builds);
        }

        [Fact]
        public void ListSubclassKeepsItsReimplementedEnumeration()
        {
            using var mapper = new MapperInstance();
            var source = new ReimplementedList { 1, 2 };
            Assert.Equal(new long[] { 7, 9 }, mapper.Map<long[]>(source));
            Assert.Equal(new long[] { 7, 9 }, mapper.Map<List<long>>(source));
            var holder = new SourceHolder { Items = source };
            Assert.Equal(new long[] { 7, 9 }, mapper.Map<DestinationHolder>(holder).Items);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void ListSourceExpressionIsEvaluatedOnce(bool toArray)
        {
            using var mapper = new MapperInstance();
            var source = new SourceHolder { Items = new List<int> { 3, 5 } };
            var parameter = Expression.Parameter(typeof(SourceHolder));
            var body = new Maps.EnumerableMap().ToSolve(Expression.Property(parameter, nameof(SourceHolder.Items)),
                toArray ? typeof(long[]) : typeof(List<long>), mapper);
            var map = Expression.Lambda<Func<SourceHolder, IEnumerable<long>>>(body, parameter).Compile();
            Assert.Equal(new long[] { 3, 5 }, map(source));
            Assert.Equal(1, source.Reads);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void ListMutationDuringElementMappingStillThrows(bool toArray)
        {
            using var mapper = new MapperInstance();
            var source = new List<MutatingSource>();
            source.Add(new MutatingSource { Read = () => { source.Clear(); return 3; } });
            Assert.Throws<InvalidOperationException>(() => mapper.Map(source, toArray ? typeof(Destination[]) : typeof(List<Destination>)));
        }

        [Fact]
        public void ValueListMutationDuringProfileMappingStillThrows()
        {
            var source = new List<int> { 3, 5 };
            using var mapper = new MutatingMapper(source);
            Assert.Throws<InvalidOperationException>(() => mapper.Map<long[]>(source));
        }

        private sealed class MutatingMapper : MapperInstance
        {
            private readonly List<int> _source;
            public MutatingMapper(List<int> source) => _source = source;
            protected override Expression Map(Expression sourceExpression, Type destinationType)
            {
                if (sourceExpression.Type == typeof(int))
                {
                    Expression<Func<int, long>> convert = value => ReadAndMutate(value);
                    return Expression.Invoke(convert, sourceExpression);
                }
                return base.Map(sourceExpression, destinationType);
            }
            public long ReadAndMutate(int value)
            {
                _source.Clear();
                return value;
            }
        }

        private sealed class CountingMapper : MapperInstance
        {
            public int Builds;
            public bool Fail;
            protected override Expression Map(Expression sourceExpression, Type destinationType)
            {
                Interlocked.Increment(ref Builds);
                if (Fail) { throw new InvalidOperationException("Invalid map configuration."); }
                return destinationType == typeof(string) ? Expression.Constant("mapped")
                    : Expression.Convert(sourceExpression, destinationType);
            }
        }

        public sealed class ReimplementedList : List<int>, IEnumerable<int>
        {
            IEnumerator<int> IEnumerable<int>.GetEnumerator() => ((IEnumerable<int>)new[] { 7, 9 }).GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable<int>)this).GetEnumerator();
        }

        public sealed class MutatingSource
        {
            public Func<int> Read { get; set; }
            public int Value => Read();
        }

        public sealed class Destination
        {
            public long Value { get; set; }
        }

        public sealed class SourceHolder
        {
            private List<int> _items;
            public int Reads;
            public List<int> Items { get { Reads++; return _items; } set => _items = value; }
        }

        public sealed class DestinationHolder
        {
            public long[] Items { get; set; }
        }
    }
}
