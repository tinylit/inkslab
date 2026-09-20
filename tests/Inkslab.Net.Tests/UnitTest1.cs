#pragma warning disable CS1591
using Inkslab.Json;
using Inkslab.Serialize.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Xunit;
namespace Inkslab.Net.Tests
{
    public class UnitTest1
    {
        static UnitTest1() => SingletonPools.TryAdd<IJsonHelper, DefaultJsonHelper>();
        [Theory]
        [InlineData(false)] [InlineData(true)]
        public async Task QueryRetryAndDeserializeAsync(bool custom)
        {
            int calls = 0;
            using var handler = new TestHttpMessageHandler((request, token) =>
            {
                calls++;
                Assert.Contains("wd=sql", request.RequestUri.Query);
                if (calls == 1) { return Task.FromResult(TestHttpMessageHandler.Response("", HttpStatusCode.Unauthorized)); }
                Assert.Contains("debug=false", request.RequestUri.Query);
                if (calls == 2) { return Task.FromResult(TestHttpMessageHandler.Response("", HttpStatusCode.Forbidden)); }
                Assert.Equal("token", string.Join("", request.Headers.GetValues("X-Auth")));
                return Task.FromResult(TestHttpMessageHandler.Response("{\"value\":\"ok\"}"));
            });
            using var client = handler.CreateClient();
            var request = new TestRequestFactory(client).CreateRequestable("https://unit.test/")
                .AppendQueryString(new { wd = "sql", page = 1 })
                .When(s => s == HttpStatusCode.Unauthorized).ThenAsync(r => { r.AppendQueryString("debug=false"); return Task.CompletedTask; })
                .When(s => s == HttpStatusCode.Forbidden).ThenAsync(r => { r.AssignHeader("X-Auth", "token"); return Task.CompletedTask; });
            var value = custom
                ? await request.CustomCast(text => JsonHelper.Json<Dictionary<string, string>>(text)).GetAsync()
                : await request.JsonCast<Dictionary<string, string>>().GetAsync();
            Assert.Equal("ok", value["value"]); Assert.Equal(3, calls);
        }
        [Fact]
        public async Task AssignHeader_SkipValidation_WorksInRetryPathAsync()
        {
            int calls = 0;
            using var handler = new TestHttpMessageHandler((r, t) =>
            {
                calls++;
                if (calls == 1) { return Task.FromResult(TestHttpMessageHandler.Response("", HttpStatusCode.Unauthorized)); }
                Assert.Equal("not-a-valid-date", string.Join("", r.Headers.GetValues("Date")));
                return Task.FromResult(TestHttpMessageHandler.Response());
            });
            using var client = handler.CreateClient();
            Assert.Equal("ok", await new TestRequestFactory(client).CreateRequestable("https://unit.test/")
                .When(s => s == HttpStatusCode.Unauthorized).ThenAsync(r => { r.AssignHeader("Date", "not-a-valid-date", true); return Task.CompletedTask; }).GetAsync());
        }
        [Fact]
        public async Task AssignHeader_SkipValidationFalse_RemovesFromSkipSetAsync()
        {
            using var handler = new TestHttpMessageHandler((r, t) => throw new InvalidOperationException("Must not send"));
            using var client = handler.CreateClient();
            await Assert.ThrowsAsync<FormatException>(() => new TestRequestFactory(client).CreateRequestable("https://unit.test/")
                .AssignHeader("Date", "invalid", true).AssignHeader("Date", "invalid", false).GetAsync());
        }
        [Fact]
        public async Task DownloadAsync()
        {
            using var handler = new TestHttpMessageHandler((r, t) => Task.FromResult(TestHttpMessageHandler.Response("file")));
            using var client = handler.CreateClient();
            using var stream = await new TestRequestFactory(client).CreateRequestable("https://unit.test/").DownloadAsync();
            using var reader = new StreamReader(stream); Assert.Equal("file", await reader.ReadToEndAsync());
        }
    }
}
