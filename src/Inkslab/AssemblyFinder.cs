using Inkslab.Collections;
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
        private static readonly string assemblyPath;

        private static readonly LFU<string, Assembly> assemblyLoads = new LFU<string, Assembly>(x =>
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
            if (!Directory.Exists(assemblyPath = AppDomain.CurrentDomain.RelativeSearchPath))
            {
                assemblyPath = AppDomain.CurrentDomain.BaseDirectory;
            }
        }

        private class DirectoryDefault : IDirectory
        {
            private static readonly Regex patternSni = new Regex(@"(\.|\\|\/)[\w-]*(sni|std|crypt|copyright|32|64|86)\.", RegexOptions.IgnoreCase | RegexOptions.RightToLeft | RegexOptions.Compiled);

            public string[] GetFiles(string path, string searchPattern)
            {
                bool flag = true;

                for (int i = 0, length = searchPattern.Length - 4; i < length; i++)
                {
                    char c = searchPattern[i];

                    if (c == '*' || c == '.')
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
                        if (patternSni.IsMatch(file))
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

        /// <summary>
        /// 所有程序集。
        /// </summary>
        /// <returns></returns>
        public static IReadOnlyList<Assembly> FindAll() => Find("*.dll");

        /// <summary>
        /// 满足指定条件的程序集。
        /// </summary>
        /// <param name="pattern">DLL过滤规则。<see cref="Directory.GetFiles(string, string)"/></param>
        /// <returns></returns>
        public static IReadOnlyList<Assembly> Find(string pattern)
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

            var directory = SingletonPools.Singleton<IDirectory, DirectoryDefault>();

            var files = directory.GetFiles(assemblyPath, pattern);

            var results = new List<Assembly>(files.Length);

            if (files.Length == 0)
            {
                return results;
            }

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

                        break;
                    }
                }

                if (loading)
                {
                    results.Add(assemblyLoads.Get(file));
                }
            }

            return results;
        }
    }
}