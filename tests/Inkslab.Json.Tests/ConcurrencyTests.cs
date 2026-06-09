using Inkslab.Json;
using Inkslab.Serialize.Json;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Xunit;

namespace Inkslab.Json.Tests
{
    /// <summary>
    /// 多字段命名模型，用于校验命名约定。
    /// </summary>
    public class NamingModel
    {
        /// <summary>
        /// 多词属性，命名约定不同结果不同。
        /// </summary>
        public string UserName { get; set; } = "John";
    }

    /// <summary>
    /// DefaultJsonHelper 并发正确性测试。
    /// </summary>
    public class ConcurrencyTests
    {
        /// <summary>
        /// 并发调用不同命名/缩进时，每次输出都必须与单线程结果一致。
        /// 共享可变 JsonSerializerSettings 会导致 ContractResolver/Formatting 跨线程串改，从而失败。
        /// </summary>
        [Fact]
        public void ConcurrentSerializeIsConsistent()
        {
            var helper = new DefaultJsonHelper();
            var model = new NamingModel();

            //? 单线程预先计算各配置的正确输出。
            var configs = new[]
            {
                (naming: NamingType.Normal, indented: false, expected: helper.ToJson(model, NamingType.Normal, false)),
                (naming: NamingType.SnakeCase, indented: false, expected: helper.ToJson(model, NamingType.SnakeCase, false)),
                (naming: NamingType.CamelCase, indented: true, expected: helper.ToJson(model, NamingType.CamelCase, true))
            };

            var failures = new ConcurrentQueue<string>();

            Parallel.For(0, 200_000, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount }, i =>
            {
                var cfg = configs[i % configs.Length];

                var actual = helper.ToJson(model, cfg.naming, cfg.indented);

                if (!string.Equals(actual, cfg.expected, StringComparison.Ordinal))
                {
                    failures.Enqueue($"{cfg.naming}/{cfg.indented} => {actual}");
                }
            });

            Assert.True(failures.IsEmpty, $"{failures.Count} 次输出不一致，例如：{(failures.TryPeek(out var s) ? s : string.Empty)}");
        }
    }
}
