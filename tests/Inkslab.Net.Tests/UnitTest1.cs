using Inkslab.Json;
using Inkslab.Serialize.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace Inkslab.Net.Tests
{
    /// <summary>
    /// 请求组件测试。
    /// </summary>
    public class UnitTest1
    {
        static UnitTest1()
        {
            SingletonPools.TryAdd<IJsonHelper, DefaultJsonHelper>();
        }

        private static readonly IRequestFactory requestFactory = new RequestFactory();

        /// <summary>
        /// Get 请求。
        /// </summary>
        [Fact]
        public async Task Get()
        {
            var requestable = requestFactory.Create("http://www.baidu.com/");

            var value = await requestable.AppendQueryString(new
            {
                wd = "sql",
                rsv_spt = 1,
                rsv_iqid = "0x822dd2a900206e39",
                issp = 1,
                rsv_bp = 1,
                rsv_idx = 2,
                ie = "utf8"
            })
            .TryThenAsync(r =>
            {
                //? 获取认证。
                r.AppendQueryString("debug=false");

                return Task.CompletedTask;
            })
            .If(status => status == System.Net.HttpStatusCode.Unauthorized)
            .ThenAsync(r =>
            {
                //? 获取有效的认证。
                r.AssignHeader("Authorization", "{{Authorization}}");

                return Task.CompletedTask;
            })
            .If(status => status == System.Net.HttpStatusCode.Forbidden)
            .Or(status => status == System.Net.HttpStatusCode.ProxyAuthenticationRequired)
            .JsonCast<Dictionary<string, string>>()
            .JsonCatch((s, e) =>
            {
                return new Dictionary<string, string>();
            })
            //.DataVerify(x => x.Count > 0) //? 结果数据校验。
            //.Fail(r => new Exception())
            //.Success(r => DateTime.Now)
            .GetAsync();
        }

        /// <summary>
        /// 下载。
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task Download()
        {
            var requestable = requestFactory.Create("https://download.visualstudio.microsoft.com/download/pr/53f250a1-318f-4350-8bda-3c6e49f40e76/e8cbbd98b08edd6222125268166cfc43/dotnet-sdk-3.0.100-win-x64.exe");

            using var stream = await requestable.TryThenAsync(r =>
               {
                   return Task.CompletedTask;
               })
               .If(status => status == System.Net.HttpStatusCode.Unauthorized)
               .DownloadAsync(360000D);
        }
    }
}