using Microsoft.Extensions.Configuration;
using System;
using System.IO;

namespace Insklab.Config
{
    /// <summary>
    /// Json 配置助手。
    /// </summary>
    public class DefaultConfigHelper : IConfigHelper
    {
        /// <summary>
        /// 获取默认配置。
        /// </summary>
        /// <returns></returns>
        private static IConfigurationBuilder ConfigurationBuilder()
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;

            string variable = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

            bool isDevelopment = string.Equals(variable, "Development", StringComparison.OrdinalIgnoreCase);

            if (isDevelopment)
            {
                string dir = Directory.GetCurrentDirectory();

                if (File.Exists(Path.Combine(dir, "appsettings.json")))
                {
                    baseDir = dir;
                }
            }

            var builder = new ConfigurationBuilder()
                    .SetBasePath(baseDir);

            var path = Path.Combine(baseDir, "appsettings.json");

            if (File.Exists(path))
            {
                builder.AddJsonFile(path, false, true);
            }

            if (isDevelopment)
            {
                var pathDev = Path.Combine(baseDir, "appsettings.Development.json");

                if (File.Exists(pathDev))
                {
                    builder.AddJsonFile(pathDev, true, true);
                }
            }

            return builder;
        }

        private static IConfigurationBuilder MakeConfigurationBuilder(string[] configPaths)
        {
            var builder = ConfigurationBuilder();

            if (configPaths is null || configPaths.Length == 0)
            {
                return builder;
            }

            string dir = Directory.GetCurrentDirectory();

            foreach (var path in configPaths)
            {
                if (File.Exists(path))
                {
                    builder.AddJsonFile(path, false, true);

                    continue;
                }

                string absolutePath = Path.Combine(dir, path);

                if (File.Exists(absolutePath))
                {
                    builder.AddJsonFile(absolutePath, false, true);

                    continue;
                }

                throw new FileNotFoundException($"文件“{path}”未找到!");
            }

            return builder;
        }

        private static IConfigurationBuilder MakeConfigurationBuilder(IConfigurationSource[] configurationSources)
        {
            var builder = ConfigurationBuilder();

            if (configurationSources is null || configurationSources.Length == 0)
            {
                return builder;
            }

            foreach (var configurationSource in configurationSources)
            {
                if (configurationSource is null)
                {
                    continue;
                }

                builder.Add(configurationSource);
            }

            return builder;
        }

        private readonly ConcurrentDictionary<string, IConfiguration> configCache = new();

        /// <summary>
        /// 构造函数。
        /// </summary>
        public DefaultConfigHelper() : this(ConfigurationBuilder())
        {

        }

        /// <summary>
        /// 构造函数（除默认文件，添加额外的配置文件）。
        /// </summary>
        /// <param name="configPaths">配置地址。</param>
        public DefaultConfigHelper(params string[] configPaths) : this(MakeConfigurationBuilder(configPaths))
        {
        }

        /// <summary>
        /// 构造函数（除默认文件，添加额外的配置文件）。
        /// </summary>
        /// <param name="configurationSources">配置资源。</param>
        public DefaultConfigHelper(params IConfigurationSource[] configurationSources) : this(MakeConfigurationBuilder(configurationSources))
        {
        }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="builder">配置。</param>
        public DefaultConfigHelper(IConfigurationBuilder builder) : this(builder.Build())
        {

        }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="config">配置。</param>
        public DefaultConfigHelper(IConfiguration config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));

            _callbackRegistration = config.GetReloadToken()
                .RegisterChangeCallback(ConfigChanged, config);
        }

        private readonly IConfiguration _config;
        private IDisposable _callbackRegistration;

        /// <summary> 配置文件变更事件。 </summary>
        public event Action<object> OnConfigChanged;

        /// <summary> 当前配置。 </summary>
        public IConfiguration Config => _config;

        /// <summary>
        /// 配置变更事件。
        /// </summary>
        /// <param name="state">状态。</param>
        private void ConfigChanged(object state)
        {
            configCache.Clear();

            OnConfigChanged?.Invoke(state);

            _callbackRegistration?.Dispose();

            _callbackRegistration = _config.GetReloadToken()
                .RegisterChangeCallback(ConfigChanged, state);
        }

        /// <summary>
        /// 配置文件读取。
        /// </summary>
        /// <typeparam name="T">读取数据类型。</typeparam>
        /// <param name="key">健。</param>
        /// <param name="defaultValue">默认值。</param>
        /// <returns></returns>
        public T Get<T>(string key, T defaultValue = default)
        {
            try
            {
                var type = typeof(T);

                //简单类型直接获取其值
                if (type.IsSimpleType())
                {
                    return _config.GetValue(key, defaultValue);
                }

                var configuration = configCache.GetOrAdd(key, name => _config.GetSection(name));
                if (type == typeof(IConfiguration))
                {
                    return (T)_config;
                }
                if (type == typeof(object) || type == typeof(IConfigurationSection))
                {
                    return (T)configuration;
                }

                // 复杂类型
                return configuration.Get<T>();
            }
            catch
            {
                return defaultValue;
            }
        }
    }
}
