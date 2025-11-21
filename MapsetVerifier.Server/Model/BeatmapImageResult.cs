namespace MapsetVerifier.Server.Model;

public readonly struct BeatmapImageResult(bool success, string? imagePath, string? mimeType, string? etag, string? errorMessage)
{
    public bool Success { get; } = success;
    public string? ImagePath { get; } = imagePath;
    public string? MimeType { get; } = mimeType;
    public string? ETag { get; } = etag;
    public string? ErrorMessage { get; } = errorMessage;

    public static BeatmapImageResult Error(string message) => new(false, null, null, null, message);
    public static BeatmapImageResult SuccessResult(string path, string mime, string etag) => new(true, path, mime, etag, null);
}
