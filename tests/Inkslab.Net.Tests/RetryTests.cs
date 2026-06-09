using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Inkslab.Net.Tests
{
    /// <summary>
    /// When/Then 重试路径的回归测试。
    /// </summary>
    public class RetryTests
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
        /// 首个响应 401 触发重试时，带 body 的重试必须重新发送 body（而非复用已释放的 Content）。
        /// </summary>
        [Fact]
        public async Task When401_RetryWithBody_ResendsBodyAsync()
        {
            var port = GetFreePort();
            var prefix = $"http://localhost:{port}/";

            const string payload = "{\"name\":\"inkslab\"}";

            var requestCount = 0;
            string secondBody = null;

            using var listener = new HttpListener();

            listener.Prefixes.Add(prefix);
            listener.Start();

            var serverTask = Task.Run(async () =>
            {
                //? 第一次请求：返回 401，触发 When/Then 重试。
                var ctx1 = await listener.GetContextAsync();

                Interlocked.Increment(ref requestCount);

                using (var reader = new StreamReader(ctx1.Request.InputStream, ctx1.Request.ContentEncoding))
                {
                    await reader.ReadToEndAsync();
                }

                ctx1.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                ctx1.Response.Close();

                //? 第二次请求（重试）：回显收到的 body，返回 200。
                var ctx2 = await listener.GetContextAsync();

                Interlocked.Increment(ref requestCount);

                using (var reader = new StreamReader(ctx2.Request.InputStream, ctx2.Request.ContentEncoding))
                {
                    secondBody = await reader.ReadToEndAsync();
                }

                var buffer = Encoding.UTF8.GetBytes(secondBody);

                ctx2.Response.StatusCode = (int)HttpStatusCode.OK;
                ctx2.Response.ContentLength64 = buffer.Length;

                await ctx2.Response.OutputStream.WriteAsync(buffer, 0, buffer.Length);

                ctx2.Response.Close();
            });

            var result = await RequestFactory.Create(prefix)
                .Body(payload, "application/json")
                .When(status => status == HttpStatusCode.Unauthorized)
                .ThenAsync(_ => Task.CompletedTask)
                .PostAsync(30000D);

            await serverTask;

            listener.Stop();

            Assert.Equal(2, requestCount);
            Assert.Equal(payload, secondBody); //? 重试必须重新发送 body。
            Assert.Equal(payload, result);
        }
    }
}
