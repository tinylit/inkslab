using System;
using System.Collections.Concurrent;
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

        private static readonly Regex patternSni = new Regex(@"(\.|\\|\/)[\w-]*(sni|std|crypt|copyright|32|64|86)\.", RegexOptions.IgnoreCase | RegexOptions.RightToLeft | RegexOptions.Compiled);

        private static readonly ConcurrentDictionary<string, Assembly> AassemblyLoads = new ConcurrentDictionary<string, Assembly>();

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

        /// <summary>
        /// 所有程序集。
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<Assembly> FindAll() => Find("*.dll");

        /// <summary>
        /// 满足指定条件的程序集。
        /// </summary>
        /// <param name="pattern">DLL过滤规则。<see cref="Directory.GetFiles(string, string)"/></param>
        /// <returns></returns>
        public static IEnumerable<Assembly> Find(string pattern)
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

            bool flag = true;

            for (int i = 0, length = pattern.Length - 4; i < length; i++)
            {
                char c = pattern[i];

                if (c == '*' || c == '.')
                {
                    continue;
                }

                flag = false;

                break;
            }

            Assembly[] assemblies = null;

            var files = Directory.GetFiles(assemblyPath, pattern);

            foreach (var file in files)
            {
                if (flag && patternSni.IsMatch(file))
                {
                    continue;
                }

                yield return AassemblyLoads.GetOrAdd(file, x =>
                {
                    if (assemblies is null)
                    {
                        assemblies = AppDomain.CurrentDomain.GetAssemblies();
                    }

                    foreach (var assembly in assemblies)
                    {
                        if (assembly.IsDynamic)
                        {
                            continue;
                        }

                        if (string.Equals(file, assembly.Location, StringComparison.OrdinalIgnoreCase))
                        {
                            return assembly;
                        }
                    }

                    try
                    {
                        return Assembly.LoadFrom(file);
                    }
                    catch (Exception e)
                    {
                        throw new Exception($"尝试加载程序文件({file})异常!", e);
                    }
                });
            }
        }
    }
}