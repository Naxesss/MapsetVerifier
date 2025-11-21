using Serilog;
using Serilog.Events;

namespace MapsetVerifier.Logging;

public static class LoggerConfigurator
{
    public static void Configure()
    {
        const string template = "[{Timestamp:HH:mm:ss} {Level:u3}] {Application} {ShortSourceContext} {Message:lj}{NewLine}{Exception}";

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("System", LogEventLevel.Warning)
            .Enrich.WithProperty("Application", "MapsetVerifier")
            .Enrich.FromLogContext()
            .Enrich.With<ShortSourceContextEnricher>()
            .WriteTo.Console(outputTemplate: template)
            .WriteTo.File("Logs/log-.txt",
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 7,
                outputTemplate: template)
            .CreateLogger();
    }
}