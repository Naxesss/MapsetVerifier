using MapsetVerifier.Framework.Objects.Resources;
using MapsetVerifier.Server.Model;

namespace MapsetVerifier.Server.Service;

/// <summary>
/// Shared thumbnail/ETag logic behind <c>GET /beatmap/image</c> (stable) and
/// <c>GET /beatmap/lazer/image</c>. Lazer images are resolved from content-addressed files whose
/// on-disk name is just a hash, so the real extension (needed to pick a mime type/encoder) is
/// passed in separately from the resolved path.
/// </summary>
public static class BeatmapImageProcessing
{
    private const int ThumbnailHeight = 126;

    public static bool IsSupportedImageFile(string filePath) =>
        ImageFileFormat.FromExtension(filePath)
            is ImageFileFormat.Jpeg
                or ImageFileFormat.Png
                or ImageFileFormat.Gif;

    /// <param name="ifNoneMatch">
    /// The ETag the client already has. When it's still current the thumbnail isn't made, as all the
    /// client needs to hear is that nothing changed.
    /// </param>
    public static BeatmapImageResult BuildResult(
        string imagePath,
        bool original,
        string? extensionOverride = null,
        string? ifNoneMatch = null
    )
    {
        var format = ImageFileFormat.FromExtension(extensionOverride ?? imagePath);

        var file = new FileInfo(imagePath);
        var version = file.Exists ? $"{file.LastWriteTimeUtc.Ticks:x}-{file.Length:x}" : file.Name;

        if (original || !ImageThumbnail.IsHigherThan(imagePath, ThumbnailHeight))
            return BeatmapImageResult.SuccessResult(
                imagePath,
                ImageFileFormat.GetMimeType(format) ?? "image/jpeg",
                $"\"{version}\""
            );

        var thumbnailFormat =
            format == ImageFileFormat.Png ? ImageFileFormat.Png : ImageFileFormat.Jpeg;
        var mimeType = ImageFileFormat.GetMimeType(thumbnailFormat)!;
        var etag = $"\"{version}-h{ThumbnailHeight}\"";

        if (etag == ifNoneMatch)
            return BeatmapImageResult.SuccessResult(imagePath, mimeType, etag);

        try
        {
            return BeatmapImageResult.SuccessStreamResult(
                ImageThumbnail.Create(imagePath, ThumbnailHeight, thumbnailFormat),
                mimeType,
                etag
            );
        }
        catch (Exception ex)
        {
            return BeatmapImageResult.Error("Failed to resize image: " + ex.Message);
        }
    }
}
