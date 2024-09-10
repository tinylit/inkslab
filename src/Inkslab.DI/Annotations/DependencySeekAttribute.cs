using System;
using System.Collections.Generic;

namespace Inkslab.DI.Annotations
{
    /// <summary>
    /// 依赖。
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = true)]
    public abstract class DependencySeekAttribute : Attribute
    {
        /// <summary>
        /// 查找更多的依赖项。
        /// </summary>
        /// <param name="implementationType">被标记类型的实现类。</param>
        /// <returns></returns>
        public abstract IEnumerable<Type> Dependencies(Type implementationType);
    }
}