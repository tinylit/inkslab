namespace Inkslab.DI.Tests.DependencyInjections
{
    /// <summary>
    /// 测试方法FromServices注入。
    /// </summary>
    public interface ITestFromServices
    {
        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        bool Test();
    }

    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public class TestFromServices : ITestFromServices
    {
        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public bool Test() => true;
    }
}
