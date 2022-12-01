﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Inkslab
{
    /// <summary>
    /// 启动。
    /// </summary>
    public class XStartup : IDisposable
    {
        private readonly List<Type> types;
        private static readonly object lockObj = new object();
        private static readonly HashSet<Type> Startups = new HashSet<Type>();
        private static readonly Type StartupType = typeof(IStartup);

        /// <summary>
        /// 启动（获取所有DLL的类型启动）<see cref="AssemblyFinder.FindAll()"/>。
        /// </summary>
        public XStartup() : this(AssemblyFinder.FindAll())
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

            this.types = types.Where(x => x.IsClass && !x.IsAbstract && StartupType.IsAssignableFrom(x)).ToList();
        }

        /// <summary>
        /// 执行启动项。
        /// </summary>
        public void DoStartup()
        {
            var startups = new List<IStartup>(types.Count);

            foreach (var type in types)
            {
                if (Startups.Contains(type))
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
                            bool flag = false;

                            lock (lockObj)
                            {
                                flag = Startups.Add(startup.GetType());
                            }

                            if (flag)
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
        private bool disposedValue = false; // 要检测冗余调用

        /// <inheritdoc />
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                types.Clear();

                if (disposing)
                {
                    GC.SuppressFinalize(this);
                }

                disposedValue = true;
            }
        }

        /// <inheritdoc />
        public void Dispose() => Dispose(true);
        #endregion
    }
}
