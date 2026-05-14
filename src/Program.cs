using System;
using System.Globalization;
using System.Reflection;
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
            try
            {
                // Temp logger that wil later be replaced
                Log.Logger = new LoggerConfiguration().WriteTo.Console().CreateBootstrapLogger();

                LoggerConfigurator.Configure();

                var version = typeof(Program)
                    .Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                    ?.InformationalVersion;
                Log.Information("Mapset Verifier starting up {Version}", version);

                Log.Information(
                    "Runtime: {Runtime}, OS: {OS}, Version: {Version}",
                    RuntimeInformation.RuntimeIdentifier,
                    RuntimeInformation.OSDescription,
                    Environment.Version
                );

                CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
                CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;

                var folder = RuntimeInformation.IsOSPlatform(OSPlatform.Linux)
                    ? Environment.SpecialFolder.LocalApplicationData
                    : Environment.SpecialFolder.ApplicationData;
                var appDataPath = Environment.GetFolderPath(folder);
                Log.Information("App data root resolved to: {AppDataPath}", appDataPath);

                Checker.ConfigureCustomChecksPath(appDataPath, ExternalsFolderName);
                Snapshotter.ConfigurePath(appDataPath, ExternalsFolderName);

                Log.Information("Loading default checks...");
                Checker.LoadDefaultChecks();

                Log.Information("Loading custom checks...");
                Checker.LoadCustomChecks();

                var host = HostBuilderFactory.Build(args);
                Log.Information("Running host...");
                host.Run();
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Fatal error during startup or runtime initialization");
                throw;
            }
            finally
            {
                Log.Information("Mapset Verifier stopped");
                Log.CloseAndFlush();
            }
        }
    }
}
