using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;
using Xunit;

#pragma warning disable CS1591

namespace Inkslab.Config.Tests
{
    public class LifetimeTests
    {
        [Fact]
        public void DisposingHelperDoesNotDisposeInjectedConfiguration()
        {
            var config = new DisposableConfiguration();
            var helper = new DefaultConfigHelper(config);

            Assert.IsAssignableFrom<IDisposable>(helper).Dispose();

            Assert.False(config.IsDisposed);
        }

        [Fact]
        public void DisposingHelperDisposesConfigurationBuiltByHelper()
        {
            var config = new DisposableConfiguration();
            var helper = new DefaultConfigHelper(new TestConfigurationBuilder(config));

            Assert.IsAssignableFrom<IDisposable>(helper).Dispose();

            Assert.True(config.IsDisposed);
        }

        private sealed class TestConfigurationBuilder : IConfigurationBuilder
        {
            private readonly IConfigurationRoot _configuration;

            public TestConfigurationBuilder(IConfigurationRoot configuration)
            {
                _configuration = configuration;
            }

            public IDictionary<string, object> Properties { get; } = new Dictionary<string, object>();
            public IList<IConfigurationSource> Sources { get; } = new List<IConfigurationSource>();
            public IConfigurationBuilder Add(IConfigurationSource source)
            {
                Sources.Add(source);
                return this;
            }

            public IConfigurationRoot Build() => _configuration;
        }

        private sealed class DisposableConfiguration : IConfigurationRoot, IDisposable
        {
            public bool IsDisposed { get; private set; }
            public IEnumerable<IConfigurationProvider> Providers => Array.Empty<IConfigurationProvider>();
            public string this[string key] { get => null; set { } }
            public IEnumerable<IConfigurationSection> GetChildren() => Array.Empty<IConfigurationSection>();
            public IChangeToken GetReloadToken() => new CancellationChangeToken(CancellationToken.None);
            public IConfigurationSection GetSection(string key) => null;
            public void Reload() { }
            public void Dispose() => IsDisposed = true;
        }
    }
}
