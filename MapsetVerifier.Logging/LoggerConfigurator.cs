using System;
using System.IO;
using System.Runtime.InteropServices;
using Serilog;
using Serilog.Events;

namespace MapsetVerifier.Logging;

public static class LoggerConfigurator
{
    private const string ExternalsFolderName = "Mapset Verifier Externals";

    public static void Configure()
    {
        const string template = "[{Timestamp:HH:mm:ss} {Level:u3}] {Application} {ShortSourceContext} {Message:lj}{NewLine}{Exception}";

        // Determine log file path based on environment:
        // - Dev mode (CWD contains src-tauri): use relative paths to avoid triggering cargo rebuilds
        // - Production: use a user-writable appdata directory (same pattern as Program.cs)
        var cwd = Directory.GetCurrentDirectory();
        string logPath;

        if (cwd.EndsWith("src-tauri") || cwd.Contains(Path.DirectorySeparatorChar + "src-tauri" + Path.DirectorySeparatorChar))
        {
            logPath = Path.Combine("..", "Logs", "log-.txt");
        }
        else
        {
            var appdataPath = RuntimeInformation.IsOSPlatform(OSPlatform.Linux)
                ? Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)
                : Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

            logPath = Path.Combine(appdataPath, ExternalsFolderName, "Logs", "log-.txt");
        }

        var logDir = Path.GetDirectoryName(logPath);
        if (!string.IsNullOrWhiteSpace(logDir))
        {
            Directory.CreateDirectory(logDir);
        }

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("System", LogEventLevel.Warning)
            .Enrich.WithProperty("Application", "MapsetVerifier")
            .Enrich.FromLogContext()
            .Enrich.With<ShortSourceContextEnricher>()
            .WriteTo.Console(outputTemplate: template)
            .WriteTo.File(logPath,
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 7,
                outputTemplate: template)
            .CreateLogger();
    }
}