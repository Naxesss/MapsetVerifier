namespace MapsetVerifier.Server.Service;

/// <summary>
/// Formats media durations the way they are shown throughout the overview pages. Unlike
/// <see cref="MapsetVerifier.Parser.Statics.Timestamp" />, this is a plain length rather than a
/// timestamp pointing at a moment in the map.
/// </summary>
internal static class DurationFormatter
{
    public static string Format(double durationMs)
    {
        var timeSpan = TimeSpan.FromMilliseconds(durationMs);

        return timeSpan.Hours > 0
            ? $"{timeSpan.Hours}:{timeSpan.Minutes:D2}:{timeSpan.Seconds:D2}"
            : $"{timeSpan.Minutes}:{timeSpan.Seconds:D2}";
    }
}
