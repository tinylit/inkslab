using System;
using System.Collections.Generic;
using System.Linq;
using Inkslab.DI.Annotations;

namespace Inkslab.DI.Tests
{
    /// <summary>
    /// 测试参数查找。
    /// </summary>
    [TestSeek]
    public interface ITestSeekArguments { }

    /// <inheritdoc/>
    public class TestSeekAttribute : DependencySeekAttribute
    {
        /// <inheritdoc/>
        public override IEnumerable<Type> Dependencies(Type implementationType)
        {
            return implementationType
                .GetMethod("Invoke")
                .GetParameters()
                .Select(x => x.ParameterType);
        }
    }
}
