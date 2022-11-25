#if !NET_Traditional
using Microsoft.Extensions.Configuration;
using System;
using System.IO;

namespace Inkslab.Config.Settings
{
    /// <summary>
    /// Json 文件路径配置设置。
    /// </summary>
    public class JsonPathConfigSettings : IJsonConfigSettings
    {
        private readonly string[] configPaths;

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="configPaths">配置文件物理路径。</param>
        /// <exception cref="ArgumentNullException">参数 <paramref name="configPaths"/> is null.</exception>
        public JsonPathConfigSettings(params string[] configPaths)
        {
            this.configPaths = configPaths ?? throw new ArgumentNullException(nameof(configPaths));
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <param name="configurationBuilder"><inheritdoc/></param>
        public void Config(IConfigurationBuilder configurationBuilder)
        {
            if (configPaths.Length == 0)
            {
                return;
            }

            string dir = Directory.GetCurrentDirectory();

            foreach (var path in configPaths)
            {
                if (File.Exists(path))
                {
                    configurationBuilder.AddJsonFile(path, false, true);

                    continue;
                }

                string absolutePath = Path.Combine(dir, path);

                if (File.Exists(absolutePath))
                {
                    configurationBuilder.AddJsonFile(absolutePath, false, true);

                    continue;
                }

                throw new FileNotFoundException($"文件“{path}”未找到!");
            }
        }
    }
}
#endif