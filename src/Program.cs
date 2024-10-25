using System;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using MapsetVerifier.Framework;
using MapsetVerifier.Server;
using MapsetVerifier.Snapshots;

namespace MapsetVerifier
{
    internal static class Program
    {
        private static void Main()
        {
            // Ensures that numbers are displayed consistently across cultures, for example
            // that decimals are indicated by a period and not a comma.
            CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;

            // Use `AppData/Roaming/` for windows and `~/.local/share` for linux.
            var appdataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                appdataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

            Checker.RelativeDLLDirectory = Path.Combine(appdataPath, "Mapset Verifier Externals", "checks");
            Snapshotter.RelativeDirectory = Path.Combine(appdataPath, "Mapset Verifier Externals");

            // Loads both the default auto-updated checks and any checks from plugins.
            Checker.LoadDefaultChecks();
            Checker.LoadCustomChecks();

            Host.Initialize();
        }
    }
}