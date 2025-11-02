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
        private const string ExternalsFolderName = "Mapset Verifier Externals";
        
        private static void Main()
        {
            // Ensures that numbers are displayed consistently across cultures, for example
            // that decimals are indicated by a period and not a comma.
            CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;

            // Use `AppData/Roaming/` for windows and `~/.local/share` for linux.
            string appdataPath;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                appdataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            else
                appdataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

            Checker.RelativeDLLDirectory = Path.Combine(appdataPath, ExternalsFolderName, Checker.DefaultRelativeDLLDirectory);
            Snapshotter.RelativeDirectory = Path.Combine(appdataPath, ExternalsFolderName);

            // Loads both the default auto-updated checks and any checks from plugins.
            Checker.LoadDefaultChecks();
            Checker.LoadCustomChecks();

            Host.Initialize();
        }
    }
}