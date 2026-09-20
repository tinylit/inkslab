using System;
#if NET_Traditional
using Inkslab.Exceptions;
using System.Configuration;
using System.Web.Configuration;
using System.Collections.Generic;
using Inkslab.Map;
#else
using System.Collections.Concurrent;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;
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
    public class DefaultConfigHelper : IConfigHelper, IDisposable
    {
#if NET_Traditional
        private readonly Configuration _config;
        private readonly Dictionary<string, string> _configs;
        private readonly Dictionary<string, ConnectionStringSettings> _connectionStrings;

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
            _configs = new Dictionary<string, string>();

            _connectionStrings = new Dictionary<string, ConnectionStringSettings>(StringComparer.OrdinalIgnoreCase);

            _config = environment switch
            {
                RuntimeEnvironment.Form or RuntimeEnvironment.Service => ConfigurationManager.OpenExeConfiguration(string.Empty),
                _ => WebConfigurationManager.OpenWebConfiguration("~"),
            };

            foreach (string key in _config.AppSettings.Settings.AllKeys)
            {
                _configs[key] = _config.AppSettings.Settings[key].Value;
            }

            foreach (ConnectionStringSettings connectionString in _config.ConnectionStrings.ConnectionStrings)
            {
                _connectionStrings[connectionString.Name] = connectionString;
            }
        }

        /// <summary>
        /// 获取配置。
        /// </summary>
        /// <typeparam name="T">返回类型。</typeparam>
        /// <param name="key">键。</param>
        /// <param name="defaultValue">默认值。</param>
        /// <returns>获取到的值。</returns>
        public T Get<T>(string key, T defaultValue = default)
        {
            if (key.IndexOf('/') == -1)
            {
                if (_configs.TryGetValue(key, out string value))
                {
#if NET_Traditional
                    if (typeof(T) == typeof(string))
                    {
                        return (T)(object)value;
                    }
                    if (typeof(T).IsPrimitive || typeof(T) == typeof(decimal))
                    {
                        return (T)Convert.ChangeType(value, typeof(T));
                    }
#endif
                    return Mapper.Map<T>(value);
                }

                return defaultValue;
            }

            var keys = key.Split('/');

            if (string.Equals(keys[0], "connectionStrings", StringComparison.OrdinalIgnoreCase))
            {
                if (keys.Length == 1)
                {
                    return Mapper.Map<T>(_connectionStrings);
                }

                if (_connectionStrings.TryGetValue(keys[1], out ConnectionStringSettings value))
                {
                    if (keys.Length == 2)
                    {
                        return Mapper.Map<T>(value);
                    }

                    if (keys.Length > 3)
                    {
                        throw new SyntaxException("以“connectionStrings”开始的键，最多支持3段！");
                    }

                    if (string.Equals(keys[2], "connectionString", StringComparison.OrdinalIgnoreCase))
                    {
#if NET_Traditional
                        if (typeof(T) == typeof(string)) { return (T)(object)value.ConnectionString; }
#endif
                        return Mapper.Map<T>(value.ConnectionString);
                    }

                    if (string.Equals(keys[2], "name", StringComparison.OrdinalIgnoreCase))
                    {
#if NET_Traditional
                        if (typeof(T) == typeof(string)) { return (T)(object)value.Name; }
#endif
                        return Mapper.Map<T>(value.Name);
                    }

                    if (string.Equals(keys[2], "providerName", StringComparison.OrdinalIgnoreCase))
                    {
#if NET_Traditional
                        if (typeof(T) == typeof(string)) { return (T)(object)value.ProviderName; }
#endif
                        return Mapper.Map<T>(value.ProviderName);
                    }
                }

                return defaultValue;
            }

            if (string.Equals(keys[0], "appStrings", StringComparison.OrdinalIgnoreCase))
            {
                if (keys.Length == 1)
                {
                    return Mapper.Map<T>(_configs);
                }

                if (keys.Length == 2 && _configs.TryGetValue(keys[1], out string value))
                {
#if NET_Traditional
                    if (typeof(T) == typeof(string)) { return (T)(object)value; }
#endif
                    return Mapper.Map<T>(value);
                }

                if (keys.Length > 2)
                {
                    throw new SyntaxException("以“appStrings”开始的键，最多支持2段！");
                }

                return defaultValue;
            }

            var sectionGroup = _config.GetSectionGroup(keys[0]);

            if (sectionGroup is null)
            {
                var section = _config.GetSection(key);

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

                if (!flag)
                {
                    break;
                }
            }

            if (keys.Length != index)
            {
                return defaultValue;
            }

            if (sectionGroup is T sectionValue)
            {
                return sectionValue;
            }

            return (T)(sectionGroup as object);
        }
#else
        /// <summary>
        /// 获取默认配置。
        /// </summary>
        /// <returns></returns>
        private static IConfigurationBuilder ConfigurationBuilder()
        {
            string environmentVariable = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

            var builder = new ConfigurationBuilder();

            builder.SetBasePath(Environment.OSVersion.Platform is PlatformID.Win32NT or PlatformID.Win32S or PlatformID.Win32Windows or PlatformID.WinCE
                ? AppContext.BaseDirectory
                : Environment.CurrentDirectory);

            builder.AddJsonFile("appsettings.json", false, true);

            if (environmentVariable is { Length: > 0 })
            {
                builder.AddJsonFile($"appsettings.{environmentVariable}.json", true, true);
            }

            return builder;
        }

        private static IConfigurationBuilder MakeConfigurationBuilder(IJsonConfigSettings settings)
        {
            var builder = ConfigurationBuilder();

            settings?.Config(builder);

            return builder;
        }

        private readonly IConfiguration _config;

        private readonly IDisposable _ownedConfiguration;

        private readonly object _reloadLock = new object();

        private bool _disposed;

        private readonly ConcurrentDictionary<string, IConfigurationSection> _cachings = new ConcurrentDictionary<string, IConfigurationSection>();

        /// <summary>
        /// 构造函数。
        /// </summary>
        public DefaultConfigHelper() : this(ConfigurationBuilder())
        {
        }

        /// <summary>
        /// 构造函数（设置配置构造器）。
        /// </summary>
        /// <param name="settings">设置。</param>
        public DefaultConfigHelper(IJsonConfigSettings settings) : this(MakeConfigurationBuilder(settings))
        {
        }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="builder">配置。</param>
        public DefaultConfigHelper(IConfigurationBuilder builder) : this(builder?.Build(), true)
        {
        }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="config">配置。</param>
        public DefaultConfigHelper(IConfiguration config) : this(config, false)
        {
        }

        private DefaultConfigHelper(IConfiguration config, bool ownsConfiguration)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));

            if (ownsConfiguration)
            {
                _ownedConfiguration = config as IDisposable;
            }

            callbackRegistration = ChangeToken.OnChange(config.GetReloadToken, ConfigChanged, config);
        }
        private IDisposable callbackRegistration;

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
            lock (_reloadLock)
            {
                if (_disposed)
                {
                    return;
                }
            }

            _cachings.Clear();

            OnConfigChanged?.Invoke(state);
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

                var configuration = _cachings.GetOrAdd(key, name => _config.GetSection(name));

                if (type == typeof(object) || type == typeof(IConfiguration) || type == typeof(IConfigurationSection))
                {
                    return (T)configuration;
                }

                if (configuration.Exists())
                {
                    return configuration.Get<T>();
                }

                return defaultValue;
            }
            catch
            {
                return defaultValue;
            }
        }
#endif

        /// <summary>
        /// 释放配置刷新订阅和由当前实例创建的配置根。
        /// </summary>
        public void Dispose()
        {
#if !NET_Traditional
            IDisposable registration;

            lock (_reloadLock)
            {
                if (_disposed)
                {
                    return;
                }

                _disposed = true;
                registration = callbackRegistration;
                callbackRegistration = null;
            }

            registration?.Dispose();
            _ownedConfiguration?.Dispose();
#endif
            GC.SuppressFinalize(this);
        }
    }
}
