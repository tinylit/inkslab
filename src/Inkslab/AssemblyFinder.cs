﻿using Inkslab.Collections;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Inkslab
{
    /// <summary>
    /// 程序集缓存。
    /// </summary>
    public static class AssemblyFinder
    {
        private static readonly string _assemblyPath;

        private static readonly Lfu<string, Assembly> _assemblyLoads = new Lfu<string, Assembly>(100, x =>
        {
            try
            {
                return Assembly.LoadFrom(x);
            }
            catch (Exception e)
            {
                throw new Exception($"尝试加载程序文件({x})异常!", e);
            }
        });

        /// <summary>
        /// 静态构造函数。
        /// </summary>
        static AssemblyFinder()
        {
            if (!Directory.Exists(_assemblyPath = AppDomain.CurrentDomain.RelativeSearchPath))
            {
                _assemblyPath = AppDomain.CurrentDomain.BaseDirectory;
            }
        }

        private class DirectoryDefault : IDirectory
        {
            private static readonly Regex _patternSni = new Regex(@"(\.|\\|\/)[\w-]*(sni|std|crypt|copyright|32|64|86)\.", RegexOptions.IgnoreCase | RegexOptions.RightToLeft | RegexOptions.Compiled);

            private static readonly List<string> _strings = new List<string>(40)
            {
                "Google.",
                "Microsoft.",
                "System.",
                "Newtonsoft.",
                "Grpc.",
                "MongoDB.",
                "Nacos.",
                "Nest.",
                "Elasticsearch.",
                "NPOI.",
                "OpenTelemetry.",
                "OpenTracing.",
                "EntityFrameworkCore.",
                "RabbitMQ.",
                "StackExchange.",
                "Quartz.",
                "Hangfire.",
                "Serilog.",
                "NLog.",
                "Log4Net.",
                "log4stash.",
                "Dapper.",
                "AutoMapper.",
                "FluentValidation.",
                "MediatR.",
                "Swashbuckle.",
                "Swagger.",
                "IdentityServer.",
                "IdentityModel.",
                "SixLabors.",
                "Snappier.",
                "ZstdSharp.",
                "NetTopologySuite.",
                "DnsClient.",
                "AWSSDK.",
                "Azure.",
                "BouncyCastle.",
                "FastReport.",
                "Zstandard."
            };

            public string[] GetFiles(string path, string searchPattern)
            {
                bool flag = true;

                for (int i = 0, length = searchPattern.Length - 4; i < length; i++)
                {
                    char c = searchPattern[i];

                    if (c is '*' or '.')
                    {
                        continue;
                    }

                    flag = false;

                    break;
                }

                var files = Directory.GetFiles(path, searchPattern);

                if (flag)
                {
                    var results = new List<string>(files.Length);

                    foreach (var file in files)
                    {
                        if (_patternSni.IsMatch(file))
                        {
                            continue;
                        }

                        string fileName = Path.GetFileName(file);

                        if (_strings.Exists(s => fileName.StartsWith(s)))
                        {
                            continue;
                        }

                        results.Add(file);
                    }

                    return results.ToArray();
                }

                return files;
            }
        }

        private static string[] GetFiles(IDirectory directory, string assemblyPath, string pattern)
        {
            if (pattern is null)
            {
                throw new ArgumentNullException(nameof(pattern));
            }

            if (pattern.Length == 0)
            {
                throw new ArgumentException($"“{nameof(pattern)}”不能为空。", nameof(pattern));
            }

            if (!pattern.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
            {
                pattern += ".dll";
            }

            return directory.GetFiles(assemblyPath, pattern);
        }

        private static List<Assembly> GetAssemblies(IEnumerable<string> files, int capacity)
        {
            var results = new List<Assembly>(capacity);

            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();

            foreach (var file in files)
            {
                bool loading = true;

                foreach (var assembly in assemblies)
                {
                    if (assembly.IsDynamic)
                    {
                        continue;
                    }

                    if (string.Equals(file, assembly.Location, StringComparison.OrdinalIgnoreCase))
                    {
                        loading = false;

                        results.Add(assembly);
                    }
                }

                if (loading)
                {
                    results.Add(_assemblyLoads.Get(file));
                }
            }

            return results;
        }

        /// <summary>
        /// 所有程序集。
        /// </summary>
        /// <returns>程序集。</returns>
        public static IReadOnlyList<Assembly> FindAll() => Find("*.dll");

        /// <summary>
        /// 满足指定条件的程序集。
        /// </summary>
        /// <param name="pattern">DLL过滤规则。<see cref="Directory.GetFiles(string, string)"/></param>
        /// <returns>程序集。</returns>
        public static IReadOnlyList<Assembly> Find(string pattern)
        {
            if (pattern is null)
            {
                throw new ArgumentNullException(nameof(pattern));
            }

            var directory = SingletonPools.Singleton<IDirectory, DirectoryDefault>();

            var files = GetFiles(directory, _assemblyPath, pattern);

            return GetAssemblies(files, files.Length);
        }

        /// <summary>
        /// 满足指定条件的程序集。
        /// </summary>
        /// <param name="patterns">DLL过滤规则。<see cref="Directory.GetFiles(string, string)"/></param>
        /// <returns>程序集。</returns>
        public static IReadOnlyList<Assembly> Find(params string[] patterns)
        {
            if (patterns is null)
            {
                throw new ArgumentNullException(nameof(patterns));
            }

            var files = new HashSet<string>();

            var directory = SingletonPools.Singleton<IDirectory, DirectoryDefault>();

            for (int i = 0; i < patterns.Length; i++)
            {
                foreach (var file in GetFiles(directory, _assemblyPath, patterns[i]))
                {
                    files.Add(file);
                }
            }

            return GetAssemblies(files, files.Count);
        }
    }
}