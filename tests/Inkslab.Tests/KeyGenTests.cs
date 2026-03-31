using Inkslab.Keys;
using Inkslab.Keys.Snowflake;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Inkslab.Tests
{
    /// <summary>
    /// 主键生成器测试。
    /// </summary>
    public class KeyGenTests
    {
        /// <summary>
        /// 默认。
        /// </summary>
        /// <returns></returns>
        [Fact]
        public Task Default() => CheckAsync();

        /// <summary>
        /// 指定机房机号。
        /// </summary>
        /// <param name="workerId">机号。</param>
        /// <param name="datacenterId">机房。</param>
        /// <returns></returns>
        [Theory]
        [InlineData(1, 1)] //? 框架设置，只要使用了一次Id生成器，则配置不再生效，顾只能指定一次。
        public Task Assign(int workerId, int datacenterId)
        {
            SingletonPools.TryAdd(new KeyOptions(workerId, datacenterId));

            return CheckAsync();
        }

        /// <summary>
        /// 检测。
        /// </summary>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        private static async Task CheckAsync()
        {
            int len = 10000;

            var keys = new HashSet<long>();
            var tasks = new List<Task<long>>(len);

            for (int i = 0; i < len; i++)
            {
                tasks.Add(Task.Run(() => KeyGen.Id()));
            }

            await Task.WhenAll(tasks);

            for (int i = 0; i < len; i++)
            {
                var id = await tasks[i];

                if (keys.Add(id))
                {
                    continue;
                }

                throw new Exception("主键重复!");
            }
        }

        /// <summary>
        /// 高并发ID唯一性测试（50000个并发任务）。
        /// </summary>
        [Fact]
        public async Task HighConcurrency_ShouldGenerateUniqueIdsAsync()
        {
            const int count = 50000;
            var ids = new ConcurrentBag<long>();
            var tasks = new List<Task>(count);

            for (int i = 0; i < count; i++)
            {
                tasks.Add(Task.Run(() => ids.Add(KeyGen.Id())));
            }

            await Task.WhenAll(tasks);

            var distinct = ids.Distinct().Count();
            Assert.Equal(count, distinct);
        }

        /// <summary>
        /// ID应具有时间递增性。
        /// </summary>
        [Fact]
        public void Ids_ShouldBeMonotonicallyIncreasing()
        {
            long previous = 0;

            for (int i = 0; i < 1000; i++)
            {
                var current = KeyGen.Id();
                Assert.True(current > previous, $"ID {current} should be greater than {previous}");
                previous = current;
            }
        }

        /// <summary>
        /// Key对象应正确解析workerId和datacenterId。
        /// </summary>
        [Fact]
        public void KeyNew_ShouldParseCorrectly()
        {
            var id = KeyGen.Id();
            var key = KeyGen.New(id);

            Assert.True(key.WorkId >= 0 && key.WorkId <= 31);
            Assert.True(key.DataCenterId >= 0 && key.DataCenterId <= 31);
            Assert.True(key.ToUniversalTime() <= DateTime.UtcNow);
            Assert.True(key.ToUniversalTime() > DateTime.UtcNow.AddMinutes(-1));
        }

        /// <summary>
        /// SnowflakeKeyGen无效参数应抛出异常。
        /// </summary>
        [Theory]
        [InlineData(-1, 0)]
        [InlineData(0, -1)]
        [InlineData(32, 0)]
        [InlineData(0, 32)]
        public void SnowflakeKeyGen_InvalidArgs_ShouldThrow(int workerId, int datacenterId)
        {
            Assert.Throws<ArgumentException>(() => new SnowflakeKeyGen(workerId, datacenterId));
        }

        /// <summary>
        /// SnowflakeKeyGen边界合法参数。
        /// </summary>
        [Theory]
        [InlineData(0, 0)]
        [InlineData(31, 31)]
        [InlineData(0, 31)]
        [InlineData(31, 0)]
        public void SnowflakeKeyGen_BoundaryArgs_ShouldSucceed(int workerId, int datacenterId)
        {
            var gen = new SnowflakeKeyGen(workerId, datacenterId);
            var id = gen.Id();
            var key = gen.New(id);

            Assert.Equal(workerId, key.WorkId);
            Assert.Equal(datacenterId, key.DataCenterId);
        }
    }
}
