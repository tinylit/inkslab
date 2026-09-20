using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Inkslab.Net.Tests
{
    internal sealed class LoopbackHttpServer : IDisposable
    {
        private readonly TcpListener _listener = new TcpListener(IPAddress.Loopback, 0);
        private readonly CancellationTokenSource _stop = new CancellationTokenSource(TimeSpan.FromSeconds(20));
        private readonly Func<string, NetworkStream, CancellationToken, Task> _respond;
        private readonly int _requests;
        public TaskCompletionSource<bool> HeadersSent { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource<bool> ReleaseBody { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public string Url { get; }
        public Task Completion { get; }
        public LoopbackHttpServer(Func<string, NetworkStream, CancellationToken, Task> respond = null, int requests = 1)
        {
            _respond = respond;
            _requests = requests;
            _listener.Start();
            Url = $"http://127.0.0.1:{((IPEndPoint)_listener.LocalEndpoint).Port}/";
            Completion = RunAsync();
        }
        private async Task RunAsync()
        {
            try
            {
                for (int attempt = 0; attempt < _requests; attempt++)
                {
                using var client = await _listener.AcceptTcpClientAsync(_stop.Token);
                using var stream = client.GetStream();
                var request = new StringBuilder();
                var next = new byte[1];
                while (!request.ToString().EndsWith("\r\n\r\n", StringComparison.Ordinal))
                {
                    if (await stream.ReadAsync(next, _stop.Token) == 0) { return; }
                    request.Append((char)next[0]);
                }
                if (_respond != null)
                {
                    await _respond(request.ToString(), stream, _stop.Token);
                    continue;
                }
                await stream.WriteAsync(Encoding.ASCII.GetBytes("HTTP/1.1 200 OK\r\nContent-Length: 6\r\nConnection: close\r\n\r\nabc"), _stop.Token);
                HeadersSent.TrySetResult(true);
                await ReleaseBody.Task.WaitAsync(_stop.Token);
                await stream.WriteAsync(Encoding.ASCII.GetBytes("def"), _stop.Token);
                }
            }
            catch (Exception ex) when (ex is OperationCanceledException || ex is IOException || ex is ObjectDisposedException) { }
        }
        internal static async Task<string> ReadBodyAsync(string headers, NetworkStream stream, CancellationToken token)
        {
            int length = 0;
            foreach (var line in headers.Split("\r\n"))
            { if (line.StartsWith("Content-Length:", StringComparison.OrdinalIgnoreCase)) { length = int.Parse(line.Substring(15).Trim()); } }
            using var body = new MemoryStream();
            var buffer = new byte[8192];
            while (length > 0)
            {
                var read = await stream.ReadAsync(buffer.AsMemory(0, Math.Min(length, buffer.Length)), token);
                if (read == 0) { throw new IOException("The request body ended early."); }
                body.Write(buffer, 0, read);
                length -= read;
            }
            return Encoding.UTF8.GetString(body.ToArray());
        }
        internal static Task RespondAsync(NetworkStream stream, CancellationToken token, string body = "ok", int status = 200)
        {
            var bytes = Encoding.UTF8.GetBytes(body);
            var header = Encoding.ASCII.GetBytes($"HTTP/1.1 {status} Response\r\nContent-Length: {bytes.Length}\r\nConnection: close\r\n\r\n");
            return WriteAsync();
            async Task WriteAsync() { await stream.WriteAsync(header, token); await stream.WriteAsync(bytes, token); }
        }
        public void Dispose()
        {
            ReleaseBody.TrySetResult(true);
            _stop.Cancel();
            _listener.Stop();
            _stop.Dispose();
        }
    }
}
