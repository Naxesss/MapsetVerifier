using Serilog.Core;
using Serilog.Events;

namespace MapsetVerifier.Logging;

public class ShortSourceContextEnricher : ILogEventEnricher
{
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        if (!logEvent.Properties.TryGetValue("SourceContext", out var sc))
        {
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("ShortSourceContext", "main"));
            return;
        }

        var raw = sc is ScalarValue sv ? sv.Value?.ToString() : sc.ToString();
        if (string.IsNullOrWhiteSpace(raw))
        {
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("ShortSourceContext", raw ?? ""));
            return;
        }

        var parts = raw.Split('.');
        if (parts.Length > 1)
        {
            for (int i = 0; i < parts.Length - 1; i++)
                if (parts[i].Length > 0)
                    parts[i] = parts[i][0].ToString();
            raw = string.Join('.', parts);
        }

        logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("ShortSourceContext", raw));
    }
}

