using System;
using Inkslab.Map;

namespace Inkslab.Map.Tests
{
    internal static class StartupFixture
    {
        private static readonly Lazy<bool> _initialized = new Lazy<bool>(() =>
        {
            using var startup = new XStartup(new[] { typeof(MapperInstance).Assembly });
            startup.DoStartup();
            return true;
        });

        internal static void EnsureInitialized() => _ = _initialized.Value;
    }
}
