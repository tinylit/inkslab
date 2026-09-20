using System;

namespace Inkslab.Config.Tests
{
    internal static class StartupFixture
    {
        private static readonly Lazy<bool> _initialized = new Lazy<bool>(() =>
        {
            using var startup = new XStartup(new[] { typeof(DefaultConfigHelper).Assembly });
            startup.DoStartup();
            return true;
        });

        internal static void EnsureInitialized() => _ = _initialized.Value;
    }
}
