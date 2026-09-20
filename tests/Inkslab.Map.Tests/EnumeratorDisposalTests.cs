using System;
using System.Collections;
using System.Collections.Generic;
using Xunit;

#pragma warning disable CS1591

namespace Inkslab.Map.Tests
{
    public class EnumeratorDisposalTests
    {
        [Fact]
        public void GenericEnumeratorIsDisposedAfterSuccessfulMapping()
        {
            var source = new TrackingEnumerable<int>(1, 2, 3);

            using var mapper = new MapperInstance();
            var result = mapper.Map<long[]>(source);

            Assert.Equal(new long[] { 1, 2, 3 }, result);
            Assert.True(source.Disposed);
        }

        [Fact]
        public void GenericEnumeratorIsDisposedForEmptySequence()
        {
            var source = new TrackingEnumerable<int>();

            using var mapper = new MapperInstance();
            var result = mapper.Map<long[]>(source);

            Assert.Empty(result);
            Assert.True(source.Disposed);
        }

        [Fact]
        public void GenericEnumeratorIsDisposedWhenMappingToCollection()
        {
            var source = new TrackingEnumerable<int>(1, 2, 3);

            using var mapper = new MapperInstance();
            var result = mapper.Map<List<long>>(source);

            Assert.Equal(new long[] { 1, 2, 3 }, result);
            Assert.True(source.Disposed);
        }

        [Fact]
        public void GenericEnumeratorIsDisposedWhenMoveNextThrows()
        {
            var source = new TrackingEnumerable<int>(new InvalidOperationException("move-next"));

            using var mapper = new MapperInstance();
            var exception = Assert.Throws<InvalidOperationException>(() => mapper.Map<long[]>(source));

            Assert.Equal("move-next", exception.Message);
            Assert.True(source.Disposed);
        }

        [Fact]
        public void GenericEnumeratorIsDisposedWhenElementMappingThrows()
        {
            var source = new TrackingEnumerable<ThrowingString>(new ThrowingString());

            using var mapper = new MapperInstance();
            var exception = Assert.Throws<InvalidOperationException>(() => mapper.Map<string[]>(source));

            Assert.Equal("to-string", exception.Message);
            Assert.True(source.Disposed);
        }

        [Fact]
        public void NonGenericEnumeratorIsDisposedAfterSuccessfulMapping()
        {
            var source = new NonGenericTrackingEnumerable(1L, 2L, 3L);

            using var mapper = new MapperInstance();
            var result = mapper.Map<long[]>(source);

            Assert.Equal(new long[] { 1, 2, 3 }, result);
            Assert.True(source.Disposed);
        }

        [Fact]
        public void NonGenericEnumeratorIsDisposedWhenMoveNextThrows()
        {
            var source = new NonGenericTrackingEnumerable(new InvalidOperationException("move-next"));

            using var mapper = new MapperInstance();
            var exception = Assert.Throws<InvalidOperationException>(() => mapper.Map<long[]>(source));

            Assert.Equal("move-next", exception.Message);
            Assert.True(source.Disposed);
        }

        [Fact]
        public void NonGenericEnumeratorIsDisposedWhenMappingToCollection()
        {
            var source = new NonGenericTrackingEnumerable(1L, 2L, 3L);

            using var mapper = new MapperInstance();
            var result = mapper.Map<List<long>>(source);

            Assert.Equal(new long[] { 1, 2, 3 }, result);
            Assert.True(source.Disposed);
        }

        [Fact]
        public void KeyValueEnumeratorIsDisposedAfterObjectMapping()
        {
            var source = new TrackingEnumerable<KeyValuePair<string, object>>(
                new KeyValuePair<string, object>(nameof(Target.Name), "mapped"));

            using var mapper = new MapperInstance();
            var result = mapper.Map<Target>(source);

            Assert.Equal("mapped", result.Name);
            Assert.True(source.Disposed);
        }

        private sealed class Target
        {
            public string Name { get; set; }
        }

        private sealed class ThrowingString
        {
            public override string ToString() => throw new InvalidOperationException("to-string");
        }

        private sealed class TrackingEnumerable<T> : IEnumerable<T>
        {
            private readonly T[] _items;
            private readonly Exception _moveNextException;

            public TrackingEnumerable(params T[] items)
            {
                _items = items;
            }

            public TrackingEnumerable(Exception moveNextException)
            {
                _items = Array.Empty<T>();
                _moveNextException = moveNextException;
            }

            public bool Disposed { get; private set; }

            public IEnumerator<T> GetEnumerator() => new Enumerator(this);

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            private sealed class Enumerator : IEnumerator<T>
            {
                private readonly TrackingEnumerable<T> _owner;
                private int _index = -1;

                public Enumerator(TrackingEnumerable<T> owner)
                {
                    _owner = owner;
                }

                public T Current => _owner._items[_index];

                object IEnumerator.Current => Current;

                public bool MoveNext()
                {
                    if (_owner._moveNextException != null)
                    {
                        throw _owner._moveNextException;
                    }

                    return ++_index < _owner._items.Length;
                }

                public void Reset() => _index = -1;

                public void Dispose() => _owner.Disposed = true;
            }
        }

        private sealed class NonGenericTrackingEnumerable : IEnumerable
        {
            private readonly object[] _items;
            private readonly Exception _moveNextException;

            public NonGenericTrackingEnumerable(params object[] items)
            {
                _items = items;
            }

            public NonGenericTrackingEnumerable(Exception moveNextException)
            {
                _items = Array.Empty<object>();
                _moveNextException = moveNextException;
            }

            public bool Disposed { get; private set; }

            public IEnumerator GetEnumerator() => new Enumerator(this);

            private sealed class Enumerator : IEnumerator, IDisposable
            {
                private readonly NonGenericTrackingEnumerable _owner;
                private int _index = -1;

                public Enumerator(NonGenericTrackingEnumerable owner)
                {
                    _owner = owner;
                }

                public object Current => _owner._items[_index];

                public bool MoveNext()
                {
                    if (_owner._moveNextException != null)
                    {
                        throw _owner._moveNextException;
                    }

                    return ++_index < _owner._items.Length;
                }

                public void Reset() => _index = -1;

                public void Dispose() => _owner.Disposed = true;
            }
        }
    }
}
