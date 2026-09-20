using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;
using Xunit;

#pragma warning disable CS1591

namespace Inkslab.Config.Tests
{
    public class ReloadTests
    {
        [Fact]
        public void ReloadResubscribesEvenWhenSubscriberThrows()
        {
            var config = new ReloadableConfiguration();
            var helper = new DefaultConfigHelper(config);
            var notifications = 0;
            helper.OnConfigChanged += _ =>
            {
                notifications++;
                if (notifications == 1)
                {
                    throw new InvalidOperationException("subscriber");
                }
            };

            Assert.ThrowsAny<Exception>(() => config.Reload());
            config.Reload();

            Assert.Equal(2, notifications);
        }

        [Fact]
        public void DisposingHelperInsideReloadCallbackPreventsResubscription()
        {
            var config = new ReloadableConfiguration();
            var helper = new DefaultConfigHelper(config);
            var notifications = 0;
            helper.OnConfigChanged += _ =>
            {
                notifications++;
                helper.Dispose();
            };

            config.Reload();
            config.Reload();

            Assert.Equal(1, notifications);
        }

        private sealed class ReloadableConfiguration : IConfiguration
        {
            private CancellationChangeTokenSource _reload = new CancellationChangeTokenSource();

            public string this[string key]
            {
                get => null;
                set { }
            }

            public IEnumerable<IConfigurationSection> GetChildren() => Array.Empty<IConfigurationSection>();

            public IChangeToken GetReloadToken() => _reload.Token;

            public IConfigurationSection GetSection(string key) => new EmptySection(key);

            public void Reload()
            {
                var previous = _reload;
                _reload = new CancellationChangeTokenSource();
                previous.Signal();
            }
        }

        private sealed class CancellationChangeTokenSource
        {
            private readonly System.Threading.CancellationTokenSource _source = new System.Threading.CancellationTokenSource();

            public IChangeToken Token => new CancellationChangeToken(_source.Token);

            public void Signal() => _source.Cancel();
        }

        private sealed class EmptySection : IConfigurationSection
        {
            public EmptySection(string key)
            {
                Key = key;
                Path = key;
            }

            public string this[string key] { get => null; set { } }
            public string Key { get; }
            public string Path { get; }
            public string Value { get; set; }
            public IEnumerable<IConfigurationSection> GetChildren() => Array.Empty<IConfigurationSection>();
            public IChangeToken GetReloadToken() => new CancellationChangeToken(CancellationToken.None);
            public IConfigurationSection GetSection(string key) => new EmptySection($"{Path}:{key}");
        }
    }
}
