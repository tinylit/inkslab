using Inkslab.Collections;
using System.Collections.Generic;
using Xunit;

namespace Inkslab.Tests
{
    /// <summary>
    /// 分片缓存"提前淘汰"回归测试：当键的哈希集中到同一分片时，
    /// 整体远未达到 <c>capacity</c> 就不应触发淘汰。
    /// </summary>
    public class ShardedCapacityTests
    {
        /// <summary>
        /// 强制所有键落入同一分片的比较器（<see cref="GetHashCode"/> 恒为 0）。
        /// </summary>
        private sealed class SingleBucketComparer : IEqualityComparer<int>
        {
            public bool Equals(int x, int y) => x == y;

            public int GetHashCode(int obj) => 0;
        }

        private const int Capacity = 1024;

        /// <summary>
        /// <see cref="Lru{T}"/>：键集中到单分片时，淘汰应以整体容量为界。
        /// </summary>
        [Fact]
        public void LruSingleType_KeysInOneShard_ShouldFillToTotalCapacity()
        {
            var lru = new Lru<int>(Capacity, new SingleBucketComparer());

            for (int i = 0; i < Capacity; i++)
            {
                lru.Put(i, out _);
            }

            Assert.Equal(Capacity, lru.Count);
        }

        /// <summary>
        /// <see cref="Lfu{T}"/>：键集中到单分片时，淘汰应以整体容量为界。
        /// </summary>
        [Fact]
        public void LfuSingleType_KeysInOneShard_ShouldFillToTotalCapacity()
        {
            var lfu = new Lfu<int>(Capacity, new SingleBucketComparer());

            for (int i = 0; i < Capacity; i++)
            {
                lfu.Put(i, out _);
            }

            Assert.Equal(Capacity, lfu.Count);
        }

        /// <summary>
        /// <see cref="Lru{TKey, TValue}"/>：键集中到单分片时，淘汰应以整体容量为界。
        /// </summary>
        [Fact]
        public void LruKeyValue_KeysInOneShard_ShouldFillToTotalCapacity()
        {
            var lru = new Lru<int, int>(Capacity, new SingleBucketComparer(), x => x);

            for (int i = 0; i < Capacity; i++)
            {
                lru.Get(i);
            }

            Assert.Equal(Capacity, lru.Count);
        }

        /// <summary>
        /// <see cref="Lfu{TKey, TValue}"/>：键集中到单分片时，淘汰应以整体容量为界。
        /// </summary>
        [Fact]
        public void LfuKeyValue_KeysInOneShard_ShouldFillToTotalCapacity()
        {
            var lfu = new Lfu<int, int>(Capacity, new SingleBucketComparer(), x => x);

            for (int i = 0; i < Capacity; i++)
            {
                lfu.Get(i);
            }

            Assert.Equal(Capacity, lfu.Count);
        }
    }
}
