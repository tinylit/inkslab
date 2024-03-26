using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace Inkslab.DI.Tests.Services
{
    /// <summary>
    /// 检查注入。
    /// </summary>
    public interface ITestServie
    {
        /// <summary>
        /// 执行。
        /// </summary>
        Task Do(CancellationToken stoppingToken);
    }


    /// <inheritdoc/>
    public class TestServie : ITestServie
    {
        /// <inheritdoc/>
        public async Task Do(CancellationToken stoppingToken)
        {
            Debug.WriteLine("检查自动注入成功！");

            await Task.Delay(500, stoppingToken);
        }
    }

    /// <summary>
    /// 测试检查注入。
    /// </summary>
    public class TestServies : BackgroundService
    {
        private readonly ITestServie _service;

        /// <summary>
        /// 测试服务。
        /// </summary>
        public TestServies(ITestServie service)
        {
            _service = service;
        }

        /// <inheritdoc/>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await _service.Do(stoppingToken);
            }
        }
    }
}