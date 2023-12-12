﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Inkslab
{
    /// <summary>
    /// 启动。
    /// </summary>
    public class XStartup : IDisposable
    {
        private readonly List<Type> types;
        private static readonly HashSet<Type> startupCachings = new HashSet<Type>();
        private static readonly Type startupType = typeof(IStartup);

        /// <summary>
        /// 启动（获取所有DLL的类型启动）<see cref="AssemblyFinder.FindAll()"/>。
        /// </summary>
        public XStartup() : this(AssemblyFinder.FindAll())
        {
        }

        /// <summary>
        /// 启动（满足指定条件的程序集）<see cref="AssemblyFinder.Find(string)"/>。
        /// </summary>
        /// <param name="pattern">DLL过滤规则。</param>
        public XStartup(string pattern) : this(AssemblyFinder.Find(pattern))
        {
        }

        /// <summary>
        /// 启动（获取指定程序集的所有类型启动）。
        /// </summary>
        /// <param name="assemblies">程序集集合。</param>

        public XStartup(IEnumerable<Assembly> assemblies) : this(assemblies.SelectMany(x => x.GetTypes()))
        {
        }

        /// <summary>
        /// 启动（指定类型启动）。
        /// </summary>
        /// <param name="types">类型集合。</param>
        public XStartup(IEnumerable<Type> types)
        {
            if (types is null)
            {
                throw new ArgumentNullException(nameof(types));
            }

            this.types = types.Where(x => x.IsClass && !x.IsAbstract && startupType.IsAssignableFrom(x)).ToList();
        }

        /// <summary>
        /// 执行启动项。
        /// </summary>
        public void DoStartup()
        {
            var startups = new List<IStartup>(types.Count);

            foreach (var type in types)
            {
                if (startupCachings.Contains(type))
                {
                    continue;
                }

                startups.Add((IStartup)Activator.CreateInstance(type, true));
            }

            startups
                .GroupBy(x => x.Code)
                .OrderBy(x => x.Key)
                .ForEach(x =>
                {
                    foreach (IStartup startup in x.OrderByDescending(y => y.Weight))
                    {
                        if (ToStartup(startup))
                        {
                            if (startupCachings.Add(startup.GetType()))
                            {
                                startup.Startup();
                            }
                            
                            break;
                        }
                    }
                });
        }

        /// <summary>
        /// 支持启动。
        /// </summary>
        /// <param name="startup">启动类型。</param>
        /// <returns></returns>
        protected virtual bool ToStartup(IStartup startup) => true;

        #region IDisposable Support
        private bool disposedValue; // 要检测冗余调用

        /// <summary>
        /// 是否资源。
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing && !disposedValue)
            {
                types.Clear();

                disposedValue = true;
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            Dispose(true);

            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
