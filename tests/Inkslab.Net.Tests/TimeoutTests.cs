#pragma warning disable CS1591
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
namespace Inkslab.Net.Tests
{
    public class TimeoutTests
    {
        [Fact]
        public async Task PerRequestTimeout_ThrowsTimeoutExceptionAsync()
        {
            using var server = new LoopbackHttpServer((headers, stream, token) => Task.Delay(Timeout.Infinite, token));
            var exception = await Assert.ThrowsAsync<TimeoutException>(() => RequestFactory.Create(server.Url).GetAsync(100));
            Assert.NotNull(exception.InnerException);
        }
        [Fact]
        public async Task DifferentTimeouts_BothSucceedAsync()
        {
            using var server = new LoopbackHttpServer((headers, stream, token) => LoopbackHttpServer.RespondAsync(stream, token), 2);
            var factory = new RequestFactory(new RequestInitialize());
            Assert.Equal("ok", await factory.CreateRequestable(server.Url).GetAsync(5000));
            Assert.Equal("ok", await factory.CreateRequestable(server.Url).GetAsync(8000));
            await server.Completion;
        }
    }
}
