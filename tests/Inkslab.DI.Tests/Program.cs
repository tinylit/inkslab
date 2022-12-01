using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace Inkslab.DI.Tests
{
    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public class Program
    {
        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }
}
