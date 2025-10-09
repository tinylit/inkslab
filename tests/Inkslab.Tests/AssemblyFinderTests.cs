using Xunit;

namespace Inkslab.Tests
{
    /// <summary>
    /// <see cref="AssemblyFinder"/> 测试。
    /// </summary>
    public class AssemblyFinderTests
    {
        /// <summary>
        /// 测试 AssemblyFinder.FindAll 方法。
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
