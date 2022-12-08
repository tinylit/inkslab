using Inkslab.Keys;
using System;
using System.Collections.Generic;
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
    }
}
