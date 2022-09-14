using Inkslab.Keys;
using Xunit;

namespace Inkslab.Tests
{
    public class KeyGenTests
    {
        [Fact]
        public Task Default() => CheckAsync();

        [Theory]
        [InlineData(1, 1)] //? 框架设置，只要使用了一次Id生成器，则配置不再生效，顾只能指定一次。
        public Task Assign(int workerId, int datacenterId)
        {
            RuntimeServPools.TryAddSingleton(new KeyOptions(workerId, datacenterId));

            var key = KeyGen.New();

            Assert.True(key.DataCenterId == datacenterId && key.WorkId == workerId);

            return CheckAsync();
        }

        private static async Task CheckAsync()
        {
            int len = 10000;

            var keys = new HashSet<long>(len);
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
    }
}
