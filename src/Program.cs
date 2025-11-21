using System;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using MapsetVerifier.Framework;
using MapsetVerifier.Logging;
using MapsetVerifier.Server;
using MapsetVerifier.Snapshots;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace MapsetVerifier
{
    internal static class Program
    {
        private const string ExternalsFolderName = "Mapset Verifier Externals";
        
        private static void Main(string[] args)
        {
            LoggerConfigurator.Configure();
            Log.Information("Mapset Verifier Server starting up");
            
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

            Log.Information("Start loading checks");
            Checker.LoadDefaultChecks();
            Checker.LoadCustomChecks();

            Log.Information("Starting API");
            var host = HostBuilderFactory.Build(args);
            host.Run();
            Log.CloseAndFlush();
        }
    }
}