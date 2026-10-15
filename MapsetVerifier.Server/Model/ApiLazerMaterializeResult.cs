namespace MapsetVerifier.Server.Model;

public readonly struct ApiLazerMaterializeResult(
    bool success,
    string? folderPath,
    string? errorMessage,
    string? beatmapSetId = null
)
{
    public bool Success { get; } = success;
    public string? FolderPath { get; } = folderPath;
    public string? ErrorMessage { get; } = errorMessage;

    /// <summary>
    /// Realm beatmapset id that was actually materialized. May differ from the request when the
    /// original set was soft-deleted and replaced by a redownload (new GUID, same OnlineID).
    /// </summary>
    public string? BeatmapSetId { get; } = beatmapSetId;

    public static ApiLazerMaterializeResult SuccessResult(string folderPath, string beatmapSetId) =>
        new(true, folderPath, null, beatmapSetId);

    public static ApiLazerMaterializeResult Error(string message) => new(false, null, message);
}
