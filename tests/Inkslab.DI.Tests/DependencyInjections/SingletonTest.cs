namespace Inkslab.DI.Tests.DependencyInjections
{
    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public class SingletonTest : Singleton<SingletonTest>
    {
        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        private SingletonTest()
        {
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <returns><inheritdoc/></returns>
        public bool Test() => true;
    }
}
