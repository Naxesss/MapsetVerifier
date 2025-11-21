namespace MapsetVerifier.Server.Model;

using System.IO; // added for Stream type

public readonly struct BeatmapImageResult(bool success, string? imagePath, string? mimeType, string? etag, string? errorMessage, Stream? dataStream = null)
{
    public bool Success { get; } = success;
    public string? ImagePath { get; } = imagePath;
    public string? MimeType { get; } = mimeType;
    public string? ETag { get; } = etag;
    public string? ErrorMessage { get; } = errorMessage;
    public Stream? DataStream { get; } = dataStream; // new property for in-memory image data

    public static BeatmapImageResult Error(string message) => new(false, null, null, null, message);
    public static BeatmapImageResult SuccessResult(string path, string mime, string etag) => new(true, path, mime, etag, null);
    public static BeatmapImageResult SuccessStreamResult(Stream stream, string mime, string etag) => new(true, null, mime, etag, null, stream);
}
