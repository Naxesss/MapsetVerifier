using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using MapsetVerifier.Framework;
using MapsetVerifier.Logging;
using MapsetVerifier.Server;
using MapsetVerifier.Snapshots;
using Microsoft.Extensions.Hosting;
using osu.Game.Beatmaps.Formats;
using osu.Game.Rulesets;
using Serilog;

namespace MapsetVerifier
{
    internal static class Program
    {
        private const string ExternalsFolderName = "Mapset Verifier Externals";

        private static void Main(string[] args)
        {
            var sw = Stopwatch.StartNew();

            try
            {
                // Temp logger that wil later be replaced
                Log.Logger = new LoggerConfiguration()
                    .WriteTo.Console()
                    .CreateBootstrapLogger();
                
                LoggerConfigurator.Configure();
                Log.Information("Mapset Verifier starting up");

                Log.Information(
                    "Runtime: {Runtime}, OS: {OS}, Version: {Version}",
                    RuntimeInformation.RuntimeIdentifier,
                    RuntimeInformation.OSDescription,
                    Environment.Version
                );

                // Needed for osu!lazer beatmap parsing to work
                var rulesets = new AssemblyRulesetStore();
                Decoder.RegisterDependencies(rulesets);
                Log.Information("Done setting up osu!lazer ruleset store");

                CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
                CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;

                var folder = RuntimeInformation.IsOSPlatform(OSPlatform.Linux)
                    ? Environment.SpecialFolder.LocalApplicationData
                    : Environment.SpecialFolder.ApplicationData;
                var appdataPath = Environment.GetFolderPath(folder);
                Log.Information("App data root resolved to: {AppDataPath}", appdataPath);

                Checker.RelativeDLLDirectory = Path.Combine(
                    appdataPath,
                    ExternalsFolderName,
                    Checker.DefaultRelativeDLLDirectory
                );
                Log.Information("Checker DLL directory: {Path}", Checker.RelativeDLLDirectory);

                Snapshotter.RelativeDirectory = Path.Combine(appdataPath, ExternalsFolderName);
                Log.Information("Snapshotter directory: {Path}", Snapshotter.RelativeDirectory);

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
                sw.Stop();
                Log.Information(
                    "Total application lifetime (process scope): {ElapsedMs}ms",
                    sw.ElapsedMilliseconds
                );

                Log.CloseAndFlush();
            }
        }
    }
}
