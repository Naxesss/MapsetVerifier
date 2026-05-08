namespace MapsetVerifier.Server.Model;

public readonly struct ApiLazerLookupResult(
    string status,
    string? message,
    string? detectedMetadata,
    string? folderPath,
    string? lookupRoot,
    ApiBeatmap? beatmap
)
{
    public string Status { get; } = status;
    public string? Message { get; } = message;
    public string? DetectedMetadata { get; } = detectedMetadata;
    public string? FolderPath { get; } = folderPath;
    public string? LookupRoot { get; } = lookupRoot;
    public ApiBeatmap? Beatmap { get; } = beatmap;
}
