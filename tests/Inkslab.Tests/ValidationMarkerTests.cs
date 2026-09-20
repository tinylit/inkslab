using System;
using Xunit;

namespace Inkslab.Tests
{
    /// <summary>The core assembly owns both optional validation markers.</summary>
    public class ValidationMarkerTests
    {
        /// <summary>Markers are sealed, direct-only DTO attributes with no HTTP dependency.</summary>
        [Theory]
        [InlineData("Inkslab.Annotations.ValidateInputAttribute")]
        [InlineData("Inkslab.Annotations.ValidateOutputAttribute")]
        public void MarkersAreDefinedInCoreAssembly(string name)
        {
            var assembly = typeof(SingletonPools).Assembly;
            var type = assembly.GetType(name);
            Assert.NotNull(type);
            Assert.Equal(typeof(Attribute), type.BaseType);
            Assert.True(type.IsSealed);
            var usage = Assert.IsType<AttributeUsageAttribute>(Attribute.GetCustomAttribute(type, typeof(AttributeUsageAttribute)));
            Assert.Equal(AttributeTargets.Class | AttributeTargets.Struct, usage.ValidOn);
            Assert.False(usage.AllowMultiple);
            Assert.False(usage.Inherited);
            Assert.DoesNotContain(assembly.GetReferencedAssemblies(), reference => reference.Name == "Inkslab.Net");
        }
    }
}
