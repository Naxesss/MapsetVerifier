using MapsetVerifier.Server.Model;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
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
        const int maxHeight = MaxImageHeight;

        var extLower = (extensionOverride ?? Path.GetExtension(imagePath)).ToLowerInvariant();

        var fi = new FileInfo(imagePath);
        var baseEtagCore = fi.Exists
            ? $"{fi.LastWriteTimeUtc.Ticks:x}-{fi.Length:x}"
            : Path.GetFileName(imagePath);

        var originalWidth = 0;
        var originalHeight = 0;
        try
        {
            var info = Image.Identify(imagePath);
            originalWidth = info.Width;
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

        var mime = extLower == ".png" ? "image/png" : "image/jpeg";
        var etag = '"' + baseEtagCore + $"-h{maxHeight}" + '"';

        if (etag == ifNoneMatch)
            return BeatmapImageResult.SuccessResult(imagePath, mime, etag);

        // Perform in-memory resize
        try
        {
            var ratio = (double)maxHeight / originalHeight;
            var targetSize = new Size(
                Math.Max(1, (int)Math.Round(originalWidth * ratio)),
                maxHeight
            );

            // Decoders can skip most of the work when told the size that's wanted, which for a large
            // JPEG is the difference between tens of megabytes and a fraction of one.
            using var img = Image.Load(
                new DecoderOptions { TargetSize = targetSize, Sampler = KnownResamplers.Lanczos3 },
                imagePath
            );
            img.Mutate(c =>
                c.Resize(
                    new ResizeOptions
                    {
                        // The decoder's result is only roughly the right shape, so this sets the size
                        // directly instead of working it out again from that and rounding differently.
                        Mode = ResizeMode.Stretch,
                        Size = targetSize,
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
            return BeatmapImageResult.SuccessStreamResult(ms, mime, etag);
        }
        catch (Exception ex)
        {
            return BeatmapImageResult.Error("Failed to resize image: " + ex.Message);
        }
    }
}
