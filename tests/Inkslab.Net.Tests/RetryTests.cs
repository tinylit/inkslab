#pragma warning disable CS1591
using System;
using System.Net;
using System.Threading.Tasks;
using Xunit;
namespace Inkslab.Net.Tests
{
    public class RetryTests
    {
        [Fact]
        public async Task When401_RetryWithBody_ResendsBodyAsync()
        {
            int calls = 0;
            using var handler = new TestHttpMessageHandler(async (r, t) =>
            {
                calls++; Assert.Equal("{\"name\":\"inkslab\"}", await r.Content.ReadAsStringAsync(t));
                return TestHttpMessageHandler.Response("ok", calls == 1 ? HttpStatusCode.Unauthorized : HttpStatusCode.OK);
            });
            using var client = handler.CreateClient();
            Assert.Equal("ok", await new TestRequestFactory(client).CreateRequestable("https://unit.test/")
                .Body("{\"name\":\"inkslab\"}", "application/json").When(s => s == HttpStatusCode.Unauthorized).ThenAsync(_ => Task.CompletedTask).PostAsync());
            Assert.Equal(2, calls);
        }
    }
}
