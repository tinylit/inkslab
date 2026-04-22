#pragma warning disable CS1591
using System.Threading.Tasks;
using Xunit;

namespace Inkslab.Map.Tests
{
    /// <summary>
    /// Profile / MapConfiguration 的并发安全测试。
    /// </summary>
    public class ProfileConcurrencyTests
    {
        public class SrcModel
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }

        public class DstModel
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }

        public class AnotherSrc
        {
            public int Value { get; set; }
        }

        public class AnotherDst
        {
            public int Value { get; set; }
        }

        private class ConcurrencyTestProfile : Profile
        {
            public ConcurrencyTestProfile()
            {
                Map<SrcModel, DstModel>();
            }
        }

        /// <summary>
        /// 并发调用 Profile.IsMatch 不应因内部字典/哈希集的非并发读写而抛出异常。
        /// </summary>
        [Fact]
        public async Task IsMatch_Concurrent_NoException()
        {
            var profile = new ConcurrencyTestProfile();

            var tasks = new Task[16];
            for (int i = 0; i < tasks.Length; i++)
            {
                tasks[i] = Task.Run(() =>
                {
                    for (int k = 0; k < 500; k++)
                    {
                        //? 命中 _mapCachings 的热路径
                        Assert.True(profile.IsMatch(typeof(SrcModel), typeof(DstModel)));
                        //? 命中 _missCachings 的路径
                        Assert.False(profile.IsMatch(typeof(AnotherSrc), typeof(AnotherDst)));
                    }
                });
            }

            await Task.WhenAll(tasks);
        }

        /// <summary>
        /// 并发调用 IsMatch 的同时持续添加新的 Map 配置，不应触发 "Collection was modified" 异常。
        /// </summary>
        [Fact]
        public async Task IsMatch_AndConfigure_Concurrent_NoException()
        {
            var profile = new ConcurrencyTestProfile();

            bool stop = false;

            var writer = Task.Run(() =>
            {
                //? 持续添加映射，模拟动态配置场景
                for (int i = 0; i < 200 && !stop; i++)
                {
                    //? 重复注册相同类型对不会抛错，仅更新缓存引用
                    profile.Map<SrcModel, DstModel>();
                }
            });

            var readers = new Task[8];
            for (int i = 0; i < readers.Length; i++)
            {
                readers[i] = Task.Run(() =>
                {
                    for (int k = 0; k < 1000; k++)
                    {
                        profile.IsMatch(typeof(SrcModel), typeof(DstModel));
                        profile.IsMatch(typeof(AnotherSrc), typeof(AnotherDst));
                    }
                });
            }

            await Task.WhenAll(readers);
            stop = true;
            await writer;
        }

        /// <summary>
        /// MapConfiguration.AddProfile 并发调用时应保持内部数组快照一致。
        /// </summary>
        [Fact]
        public async Task MapConfiguration_AddProfile_Concurrent_IsMatchStable()
        {
            using var config = new MapConfiguration(MapConfiguration.DefaultMaps, new Configuration
            {
                IsDepthMapping = true,
                AllowPropagationNullValues = false
            });

            //? 并行添加 32 个相同 Profile，并发读取 IsMatch 不应抛异常
            var writers = new Task[32];
            for (int i = 0; i < writers.Length; i++)
            {
                writers[i] = Task.Run(() => config.AddProfile(new ConcurrencyTestProfile()));
            }

            var readers = new Task[8];
            for (int i = 0; i < readers.Length; i++)
            {
                readers[i] = Task.Run(() =>
                {
                    for (int k = 0; k < 500; k++)
                    {
                        config.IsMatch(typeof(SrcModel), typeof(DstModel));
                    }
                });
            }

            await Task.WhenAll(writers);
            await Task.WhenAll(readers);

            Assert.True(config.IsMatch(typeof(SrcModel), typeof(DstModel)));
        }

        /// <summary>
        /// MapConfiguration.Dispose 应释放所有已注册的 Profile 资源（Profile.Dispose 幂等）。
        /// </summary>
        [Fact]
        public void MapConfiguration_Dispose_ReleasesProfiles()
        {
            var config = new MapConfiguration(MapConfiguration.DefaultMaps, new Configuration
            {
                IsDepthMapping = true,
                AllowPropagationNullValues = false
            });

            var profile = new ConcurrencyTestProfile();
            config.AddProfile(profile);

            config.Dispose();

            //? 多次 Dispose 幂等
            config.Dispose();
        }
    }
}
