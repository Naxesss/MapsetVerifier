namespace MapsetVerifier.Server.Model;

/// <summary>Result from resolving beatmap-set audio relative to Songs folder.</summary>
public readonly struct BeatmapAudioResult(
    bool success,
    string? audioPath,
    string? mimeType,
    string? errorMessage
)
{
    public bool Success { get; } = success;
    public string? AudioPath { get; } = audioPath;
    public string? MimeType { get; } = mimeType;
    public string? ErrorMessage { get; } = errorMessage;

    public static BeatmapAudioResult Error(string message) => new(false, null, null, message);

    public static BeatmapAudioResult ForFile(string path, string mime) =>
        new(true, path, mime, null);
}
