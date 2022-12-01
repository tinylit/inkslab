namespace Inkslab.DI.Tests.DependencyInjections
{
    /// <summary>
    /// 泛型注入测试。
    /// </summary>
    public interface ITestGeneric<T> where T : new()
    {
        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <returns><inheritdoc/></returns>
        T CreateNew();
    }

    /// <summary>
    /// 泛型注入测试。
    /// </summary>
    public class TestGeneric<T> : ITestGeneric<T> where T : new()
    {
        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <returns><inheritdoc/></returns>
        public T CreateNew() => new T();
    }
}
