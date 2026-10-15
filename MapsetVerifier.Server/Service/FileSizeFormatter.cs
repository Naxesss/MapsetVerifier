namespace MapsetVerifier.Server.Service;

/// <summary>
/// Formats byte counts the way they are shown throughout the overview pages.
/// </summary>
internal static class FileSizeFormatter
{
    public static string Format(long bytes)
    {
        if (bytes >= 1024 * 1024 * 1024)
            return $"{bytes / (1024.0 * 1024.0 * 1024.0):0.##} GB";

        if (bytes >= 1024 * 1024)
            return $"{bytes / (1024.0 * 1024.0):0.##} MB";

        if (bytes >= 1024)
            return $"{bytes / 1024.0:0.##} KB";

        return $"{bytes} B";
    }
}
