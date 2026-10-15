using MapsetVerifier.Server.Model;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Processing;

namespace MapsetVerifier.Server.Service;

/// <summary>
/// Shared thumbnail/ETag logic behind <c>GET /beatmap/image</c> (stable) and
/// <c>GET /beatmap/lazer/image</c>. Lazer images are resolved from content-addressed files whose
/// on-disk name is just a hash, so the real extension (needed to pick a mime type/encoder) is
/// passed in separately from the resolved path.
/// </summary>
public static class BeatmapImageProcessing
{
    private static readonly string[] SupportedImageExtensions = [".jpg", ".jpeg", ".png", ".gif"];

    /// <summary>
    /// Desired thumbnail height
    /// </summary>
    private const int MaxImageHeight = 126;

    public static bool IsSupportedImageFile(string filePath) =>
        SupportedImageExtensions.Contains(Path.GetExtension(filePath).ToLowerInvariant());

    public static BeatmapImageResult BuildResult(
        string imagePath,
        bool original,
        string? extensionOverride = null
    )
    {
        const int maxHeight = MaxImageHeight;

        var extLower = (extensionOverride ?? Path.GetExtension(imagePath)).ToLowerInvariant();

        var fi = new FileInfo(imagePath);
        var baseEtagCore = fi.Exists
            ? $"{fi.LastWriteTimeUtc.Ticks:x}-{fi.Length:x}"
            : Path.GetFileName(imagePath);

        var originalHeight = 0;
        try
        {
            var info = Image.Identify(imagePath);
            originalHeight = info.Height;
        }
        catch (Exception)
        {
            // ignore identify errors
        }

        var needsResize = !original && originalHeight > maxHeight;

        if (!needsResize)
        {
            var mimeOriginal = extLower == ".png" ? "image/png" : "image/jpeg";
            var etagOriginal = '"' + baseEtagCore + '"';
            return BeatmapImageResult.SuccessResult(imagePath, mimeOriginal, etagOriginal);
        }

        // Perform in-memory resize
        try
        {
            using var img = Image.Load(imagePath);
            var ratio = (double)maxHeight / img.Height;
            var targetW = Math.Max(1, (int)Math.Round(img.Width * ratio));
            var targetH = maxHeight;
            img.Mutate(c =>
                c.Resize(
                    new ResizeOptions
                    {
                        Mode = ResizeMode.Max,
                        Size = new Size(targetW, targetH),
                        Sampler = KnownResamplers.Lanczos3,
                    }
                )
            );
            var ms = new MemoryStream();
            if (extLower == ".png")
                img.Save(ms, new PngEncoder());
            else
                img.Save(ms, new JpegEncoder { Quality = 85 });
            ms.Position = 0; // reset for reading
            var mime = extLower == ".png" ? "image/png" : "image/jpeg";
            var etag = '"' + baseEtagCore + $"-h{maxHeight}" + '"';
            return BeatmapImageResult.SuccessStreamResult(ms, mime, etag);
        }
        catch (Exception ex)
        {
            return BeatmapImageResult.Error("Failed to resize image: " + ex.Message);
        }
    }
}
