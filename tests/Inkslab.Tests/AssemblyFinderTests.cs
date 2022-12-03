using Xunit;

namespace Inkslab.Tests
{
    /// <summary>
    /// <see cref="AssemblyFinder"/> ²âÊÔ¡£
    /// </summary>
    public class AssemblyFinderTests
    {
        /// <summary>
        /// ²éÕÒËùÓĞ¡£
        /// </summary>
        [Fact]
        public void FindAll()
        {
            var assemblies = AssemblyFinder.FindAll();

            foreach (var assembly in assemblies)
            {
                Assert.NotNull(assembly);
            }
        }
    }
}
