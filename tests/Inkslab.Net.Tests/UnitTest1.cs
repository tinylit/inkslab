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

        /// <summary>
        /// Get 请求。
        /// </summary>
        [Fact]
        public async Task Get()
        {
            var requestable = RequestFactory.Create("http://www.baidu.com/");

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
            .When(status => status == System.Net.HttpStatusCode.Unauthorized)
            .ThenAsync(r =>
            {
                //? 获取认证。
                r.AppendQueryString("debug=false");

                return Task.CompletedTask;
            })
            .When(status => status == System.Net.HttpStatusCode.Forbidden || status == System.Net.HttpStatusCode.ProxyAuthenticationRequired)
            .ThenAsync(r =>
            {
                //? 获取有效的认证。
                r.AssignHeader("Authorization", "{{Authorization}}");

                return Task.CompletedTask;
            })
            .JsonCast<Dictionary<string, string>>()
            .JsonCatch(e =>
            {
                return new Dictionary<string, string>();
            })
            //.DataVerify(r => r.Count > 0) //? 结果数据校验。
            //.Success(r => r.Count)
            //.Fail(r => new NotSupportedException())
            .GetAsync(5000D);
        }

        /// <summary>
        /// 下载。
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task Download()
        {
            var requestable = RequestFactory.Create("https://download.visualstudio.microsoft.com/download/pr/53f250a1-318f-4350-8bda-3c6e49f40e76/e8cbbd98b08edd6222125268166cfc43/dotnet-sdk-3.0.100-win-x64.exe");

            using var stream = await requestable
               .When(status => status == System.Net.HttpStatusCode.Unauthorized)
               .ThenAsync(r =>
               {
                   return Task.CompletedTask;
               })
               .DownloadAsync(360000D);
        }

        /// <summary>
        /// 测试 HttpContent 重试释放问题和认证问题。
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task Post()
        {
            //var requestable = RequestFactory.Create("http://localhost:5000/api/di-test");

            //var value = await requestable
            //    .AssignHeader("Authorization", "Bearer eyJhbGciOiJSUzI1NiIsImtpZCI6IkQ4NjdGNzEwMEM1OENDRDFBNUUzMzVFNEEzN0RGNTUwIiwidHlwIjoiSldUIn0.eyJuYmYiOjE2NzgwNjU2MjAsImV4cCI6MTY3ODQyNTYyMCwiaXNzIjoiaHR0cHM6Ly93d3cuaHlzemJiLmNvbSIsImF1ZCI6WyJIeXNNYWxsLlN5c01hbmFnZW1lbnQuQVBJIiwiempzLm9zcy5hcGkiXSwiY2xpZW50X2lkIjoiSHlzTWFsbCIsInN1YiI6IjUwMjIzZjIzLTdlNzMtNDE1Yy04YzExLTlhOGJjMTcwNzQxZiIsImF1dGhfdGltZSI6MTY3ODA2NTYyMCwiaWRwIjoibG9jYWwiLCJuYW1lIjoicm9vdCIsIm5pY2tuYW1lIjoiIiwicm9sZSI6IkFkbWluaXN0cmF0b3IiLCJ0aW1lc3RhbXAiOiI2MzgxMzY5MTIyMDA1NTYxNjciLCJqdGkiOiJCQjIzNDQ4RTEyN0MwNzFFOEM0ODVDNjZFMTE4NTE2QiIsImlhdCI6MTY3ODA2NTYyMCwic2NvcGUiOiJIeXNNYWxsLlN5c01hbmFnZW1lbnQuQVBJIG9wZW5pZCBwcm9maWxlIHpqcy5vc3MuYXBpIG9mZmxpbmVfYWNjZXNzIiwiYW1yIjpbImN1c3RvbSJdfQ.JqiZIDL-BLJXgHrhSRvwR8wmcE78zz--KqCJO4VgT7DTJTuOrphL1s8vEIFsmyXtKQkp7TsJXWfiORbE3D8Iinz-EoDLcqJefSvsmmRFJq75fRwN3C1nUdBF0aY-uTp7iIJ4ofMICGKS6vaDsWsKn5HlzowdOG5-6F8Dh1H4Ff1Nq01i2Ya_8mfJgO2cAcoTrGIeYF__PT9jgfBD9cBxUOiEuUabrMR0d7A7xu-GjzO2DQDihZ5pknUJL6O-7VlBW2XfWJfN1Lk2yCWYomZLRbzV6O9_L5jZwggENNdeNTx38lYltDGdaPwKstfLDe8oc3hrhcYIxeUoiYC8JAVoOA")
            //    .Json(new { Id = 100, Name = "测试" })
            //    .When(status => status == System.Net.HttpStatusCode.Unauthorized)
            //    .ThenAsync(r =>
            //    {
            //        return Task.CompletedTask;
            //    })
            //    .When(status => status == System.Net.HttpStatusCode.Unauthorized)
            //    .ThenAsync(r =>
            //    {
            //        return Task.CompletedTask;
            //    })
            //    .JsonCast(new { Id = 0, Name = string.Empty })
            //    .PostAsync();

            await Task.Delay(1000);
        }
    }
}