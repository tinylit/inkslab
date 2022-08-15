using Xunit;

namespace Insklab.Tests
{
    public class AssemblyFinderTests
    {
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
