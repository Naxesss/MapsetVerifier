using Serilog;
using Serilog.Events;
using System.IO;

namespace MapsetVerifier.Logging;

public static class LoggerConfigurator
{
    public static void Configure()
    {
        const string template = "[{Timestamp:HH:mm:ss} {Level:u3}] {Application} {ShortSourceContext} {Message:lj}{NewLine}{Exception}";

        // Determine log file path: when running under `src-tauri` (cargo watched), write logs to parent ../Logs to avoid triggering rebuilds.
        var cwd = Directory.GetCurrentDirectory();
        var logPath = "Logs/log-.txt"; // default
        if (cwd.EndsWith("src-tauri") || cwd.Contains(Path.DirectorySeparatorChar + "src-tauri" + Path.DirectorySeparatorChar))
        {
            logPath = Path.Combine("..", "Logs", "log-.txt");
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