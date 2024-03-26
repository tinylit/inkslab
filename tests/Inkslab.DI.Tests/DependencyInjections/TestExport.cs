using Inkslab.Annotations;
using Inkslab.DI.Annotations;

namespace Inkslab.DI.Tests.DependencyInjections
{
    /// <summary>
    /// 测试导出。
    /// </summary>
    [Export]
    public interface ITestExport
    {
    }

    /// <summary>
    /// 测试导出。
    /// </summary>
    [Singleton]
    public class TestExport : ITestExport
    {

    }
}
