using Xunit;

namespace Inkslab.Tests
{
    /// <summary>
    /// <see cref="AssemblyFinder"/> ���ԡ�
    /// </summary>
    public class AssemblyFinderTests
    {
        /// <summary>
        /// �������С�
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
