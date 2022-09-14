using System;
using Hys.Exceptions;
#if NET_Traditional
using System.Configuration;
using System.Web.Configuration;
using System.Collections.Generic;
using Inkslab.Map;
#else
using System.IO;
using System.Collections.Concurrent;
using Microsoft.Extensions.Configuration;
#endif

namespace Inkslab.Config
{

#if NET_Traditional
    /// <summary>
    /// 运行环境。
    /// </summary>
    public enum RuntimeEnvironment
    {
        /// <summary>
        /// Web
        /// </summary>
        Web = 1,
        /// <summary>
        /// From
        /// </summary>
        Form = 2,
        /// <summary>
        /// Windows Service
        /// </summary>
        Service = 3
    }
#endif

    /// <summary>
    /// 配置助手。
    /// </summary>
    public class DefaultConfigHelper : IConfigHelper
    {
#if NET_Traditional
        private readonly Configuration Config;
        private readonly Dictionary<string, string> Configs;
        private readonly Dictionary<string, ConnectionStringSettings> ConnectionStrings;

        /// <summary>
        /// 构造函数。
        /// </summary>
        public DefaultConfigHelper() : this(RuntimeEnvironment.Web) { }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="environment">运行环境。</param>
        public DefaultConfigHelper(RuntimeEnvironment environment)
        {
            Configs = new Dictionary<string, string>();

            ConnectionStrings = new Dictionary<string, ConnectionStringSettings>(StringComparer.OrdinalIgnoreCase);

            switch (environment)
            {
                case RuntimeEnvironment.Form:
                case RuntimeEnvironment.Service:
                    Config = ConfigurationManager.OpenExeConfiguration(string.Empty);
                    break;
                case RuntimeEnvironment.Web:
                default:
                    Config = WebConfigurationManager.OpenWebConfiguration("~");
                    break;
            }
        }

        /// <summary>
        /// 获取配置。
        /// </summary>
        /// <typeparam name="T">返回类型。</typeparam>
        /// <param name="key">键。</param>
        /// <param name="defaultValue">默认值。</param>
        /// <returns></returns>
        public T Get<T>(string key, T defaultValue = default)
        {
            if (key.IndexOf('/') == -1)
            {
                if (Configs.TryGetValue(key, out string value))
                {
                    return Mapper.Map(value, defaultValue);
                }

                return defaultValue;
            }

            var keys = key.Split('/');

            if (string.Equals(keys[0], "connectionStrings", StringComparison.OrdinalIgnoreCase))
            {
                if (keys.Length == 1)
                {
                    return Mapper.Map(ConnectionStrings, defaultValue);
                }

                if (ConnectionStrings.TryGetValue(keys[1], out ConnectionStringSettings value))
                {
                    if (keys.Length == 2)
                    {
                        return Mapper.Map(value, defaultValue);
                    }

                    if (keys.Length > 3)
                    {
                        throw new SyntaxException("以“connectionStrings”开始的键，最多支持3段！");
                    }

                    if (string.Equals(keys[2], "connectionString", StringComparison.OrdinalIgnoreCase))
                        return Mapper.Map(value.ConnectionString, defaultValue);

                    if (string.Equals(keys[2], "name", StringComparison.OrdinalIgnoreCase))
                        return Mapper.Map(value.Name, defaultValue);

                    if (string.Equals(keys[2], "providerName", StringComparison.OrdinalIgnoreCase))
                        return Mapper.Map(value.ProviderName, defaultValue);
                }

                return defaultValue;
            }

            if (string.Equals(keys[0], "appStrings", StringComparison.OrdinalIgnoreCase))
            {
                if (keys.Length == 1)
                {
                    return Mapper.Map(Configs, defaultValue);
                }

                if (keys.Length == 2 && Configs.TryGetValue(keys[1], out string value))
                {
                    return Mapper.Map(value, defaultValue);
                }

                if (keys.Length > 2)
                {
                    throw new SyntaxException("以“appStrings”开始的键，最多支持2段！");
                }

                return defaultValue;
            }

            var sectionGroup = Config.GetSectionGroup(keys[0]);

            if (sectionGroup is null)
            {
                var section = Config.GetSection(key);

                if (section is null)
                {
                    return defaultValue;
                }

                return (T)(object)section;
            }

            var index = 1;

            while (keys.Length > index)
            {
                bool flag = false;

                foreach (ConfigurationSectionGroup sectionGroupItem in sectionGroup.SectionGroups)
                {
                    if (string.Equals(sectionGroupItem.SectionGroupName, keys[index], StringComparison.OrdinalIgnoreCase))
                    {
                        index += 1;
                        flag = true;
                        sectionGroup = sectionGroupItem;

                        break;
                    }
                }

                if (!flag) break;
            }

            if (sectionGroup is null)
            {
                return defaultValue;
            }

            if (keys.Length == index)
            {
                if (sectionGroup is T sectionValue)
                {
                    return sectionValue;
                }

                return (T)(object)sectionGroup;
            }

            return defaultValue;
        }
#else
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

        private readonly ConcurrentDictionary<string, IConfiguration> configCache = new ConcurrentDictionary<string, IConfiguration>();

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
                if (type.IsSimple())
                {
                    return _config.GetValue(key, defaultValue);
                }

                var configuration = configCache.GetOrAdd(key, name => _config.GetSection(name));

                if (type == typeof(object) || type == typeof(IConfiguration) || type == typeof(IConfigurationSection))
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
#endif
    }
}
