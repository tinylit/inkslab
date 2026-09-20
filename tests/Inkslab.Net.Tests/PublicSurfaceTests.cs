using System;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Runtime.CompilerServices;
using Xunit;

namespace Inkslab.Net.Tests
{
    /// <summary>Production assemblies expose only the approved entry points.</summary>
    public class PublicSurfaceTests
    {
        /// <summary>Clients cannot be supplied through the removed constructor.</summary>
        [Fact]
        public void FactoryDoesNotExposeClientInjection()
        {
            Assert.DoesNotContain(typeof(RequestFactory).GetConstructors(), constructor =>
                constructor.GetParameters().Any(parameter => parameter.ParameterType == typeof(HttpClient)));
        }

        /// <summary>Nested request implementation methods use public visibility consistently.</summary>
        [Fact]
        public void CoreExecutionOverridesArePublic()
        {
            var methods = typeof(RequestFactory).GetNestedTypes(BindingFlags.NonPublic)
                .SelectMany(type => type.GetMethods(BindingFlags.Instance | BindingFlags.Public
                    | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
                .Where(method => method.Name == "SendCoreAsync").ToArray();
            Assert.NotEmpty(methods);
            Assert.All(methods, method => Assert.True(method.IsPublic, method.DeclaringType.FullName));
        }

        /// <summary>Validation marker lookup does not retain entity types in a static cache.</summary>
        [Fact]
        public void EntityValidatorDoesNotCacheMarkerTypes()
        {
            var validator = typeof(RequestFactory).Assembly.GetType("Inkslab.Net.Validation.EntityValidator", throwOnError: true);
            Assert.Empty(validator.GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic));
        }

#if !DEBUG
        /// <summary>Release library metadata does not contain friend assemblies.</summary>
        [Fact]
        public void ReleaseAssemblyHasNoFriendAssemblies()
        {
            Assert.Empty(typeof(RequestFactory).Assembly.GetCustomAttributes(typeof(InternalsVisibleToAttribute), false));
        }
#endif
    }
}
