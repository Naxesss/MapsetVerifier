using System.Runtime.InteropServices;
using Serilog;
using Serilog.Events;

namespace MapsetVerifier.Logging;

public static class LoggerConfigurator
{
    private const string ExternalsFolderName = "Mapset Verifier Externals";

    public static void Configure()
    {
        const string template =
            "[{Timestamp:HH:mm:ss} {Level:u3}] {Application} {ShortSourceContext} {Message:lj}{NewLine}{Exception}";

        try
        {
            Log.Information("Initializing logger...");
            var appdataPath = ResolveAppDataPath();
            Log.Information("Resolved base app data path: {Path}", appdataPath);

            var logPath = Path.Combine(appdataPath, ExternalsFolderName, "Logs", "log-.txt");
            var logDir = Path.GetDirectoryName(logPath);

            if (string.IsNullOrWhiteSpace(logDir))
            {
                throw new InvalidOperationException("Log directory path could not be resolved.");
            }

            Directory.CreateDirectory(logDir);

            Log.Information("Log directory ensured at: {LogDir}", logDir);
            Log.Information("Log file pattern: {LogPath}", logPath);

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                .MinimumLevel.Override("System", LogEventLevel.Warning)
                .Enrich.WithProperty("Application", "MapsetVerifier")
                .Enrich.FromLogContext()
                .Enrich.With<ShortSourceContextEnricher>()
                .WriteTo.Console(outputTemplate: template)
                .WriteTo.File(
                    logPath,
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 7,
                    outputTemplate: template
                )
                .CreateLogger();

            Log.Information("Logger configured successfully");
        }
        catch (Exception ex)
        {
            // Important: fallback safety logger if Serilog setup fails
            Console.Error.WriteLine("FATAL: Logger configuration failed");
            Console.Error.WriteLine(ex);

            throw;
        }
    }

    private static string ResolveAppDataPath()
    {
        var folder = RuntimeInformation.IsOSPlatform(OSPlatform.Linux)
            ? Environment.SpecialFolder.LocalApplicationData
            : Environment.SpecialFolder.ApplicationData;

        return Environment.GetFolderPath(folder);
    }
}
