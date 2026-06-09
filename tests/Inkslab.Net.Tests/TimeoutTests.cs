using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Inkslab.Net.Tests
{
    /// <summary>
    /// 每请求超时与多 timeout 共享客户端的契约测试。
    /// </summary>
    public class TimeoutTests
    {
        private static int GetFreePort()
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);

            listener.Start();

            try
            {
                return ((IPEndPoint)listener.LocalEndpoint).Port;
            }
            finally
            {
                listener.Stop();
            }
        }

        /// <summary>
        /// 服务端慢于超时时间时，应抛出 TimeoutException。
        /// </summary>
        [Fact]
        public async Task PerRequestTimeout_ThrowsTimeoutExceptionAsync()
        {
            var port = GetFreePort();
            var prefix = $"http://localhost:{port}/";

            using var listener = new HttpListener();

            listener.Prefixes.Add(prefix);
            listener.Start();

            var serverTask = Task.Run(async () =>
            {
                try
                {
                    var ctx = await listener.GetContextAsync();

                    await Task.Delay(3000);

                    ctx.Response.StatusCode = (int)HttpStatusCode.OK;
                    ctx.Response.Close();
                }
                catch
                {
                    //? 监听器停止时忽略。
                }
            });

            await Assert.ThrowsAsync<TimeoutException>(() => RequestFactory.Create(prefix).GetAsync(300D));

            listener.Stop();
        }

        /// <summary>
        /// 不同 timeout 的请求都应正常工作（共享单一客户端）。
        /// </summary>
        [Fact]
        public async Task DifferentTimeouts_BothSucceedAsync()
        {
            var port = GetFreePort();
            var prefix = $"http://localhost:{port}/";

            using var listener = new HttpListener();

            listener.Prefixes.Add(prefix);
            listener.Start();

            var serverTask = Task.Run(async () =>
            {
                for (var i = 0; i < 2; i++)
                {
                    var ctx = await listener.GetContextAsync();

                    var buffer = Encoding.UTF8.GetBytes("ok");

                    ctx.Response.StatusCode = (int)HttpStatusCode.OK;
                    ctx.Response.ContentLength64 = buffer.Length;

                    await ctx.Response.OutputStream.WriteAsync(buffer, 0, buffer.Length);

                    ctx.Response.Close();
                }
            });

            var r1 = await RequestFactory.Create(prefix).GetAsync(5000D);
            var r2 = await RequestFactory.Create(prefix).GetAsync(8000D);

            await serverTask;

            listener.Stop();

            Assert.Equal("ok", r1);
            Assert.Equal("ok", r2);
        }
    }
}
