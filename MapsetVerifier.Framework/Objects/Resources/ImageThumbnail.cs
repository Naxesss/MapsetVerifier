using SkiaSharp;

namespace MapsetVerifier.Framework.Objects.Resources
{
    /// <summary> Smaller versions of images, whatever format their content is in. </summary>
    public static class ImageThumbnail
    {
        private const int JpegQuality = 85;

        /// <summary> Returns whether the image can be read and is higher than this. Only reads its header. </summary>
        public static bool IsHigherThan(string filePath, int height)
        {
            using var stream = OpenFile(filePath);
            using var codec = stream == null ? null : SKCodec.Create(stream);

            return codec?.Info.Height > height;
        }

        /// <summary>
        ///     Returns the image with the given height, keeping its shape, as either PNG or JPEG. Throws if it can't
        ///     be read.
        /// </summary>
        public static MemoryStream Create(string filePath, int height, string format)
        {
            using var stream =
                OpenFile(filePath) ?? throw new IOException($"Could not open \"{filePath}\".");
            using var codec =
                SKCodec.Create(stream)
                ?? throw new InvalidDataException($"\"{filePath}\" is not an image.");
            using var thumbnail = Shrink(codec, height);
            using var data = thumbnail.Encode(
                format == ImageFileFormat.Png
                    ? SKEncodedImageFormat.Png
                    : SKEncodedImageFormat.Jpeg,
                JpegQuality
            );

            var result = new MemoryStream();
            data.SaveTo(result);
            result.Position = 0;

            return result;
        }

        private static FileStream? OpenFile(string filePath)
        {
            try
            {
                // It may be being edited.
                return new FileStream(
                    filePath,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.ReadWrite
                );
            }
            catch (Exception exception)
                when (exception is IOException or UnauthorizedAccessException)
            {
                return null;
            }
        }

        private static SKImage Shrink(SKCodec codec, int height)
        {
            var scale = (float)height / codec.Info.Height;
            var targetSize = new SKSizeI(
                Math.Max(1, (int)Math.Round(codec.Info.Width * scale)),
                height
            );

            // Decoders can skip most of the work when told the size that's wanted, which for a large JPEG is the
            // difference between tens of megabytes and a fraction of one. What they support may be larger than that.
            var decodedSize = codec.GetScaledDimensions(scale);
            if (decodedSize.Width < targetSize.Width || decodedSize.Height < targetSize.Height)
                decodedSize = codec.Info.Size;

            using var decoded = new SKBitmap(
                new SKImageInfo(
                    decodedSize.Width,
                    decodedSize.Height,
                    SKColorType.Rgba8888,
                    SKAlphaType.Premul
                )
            );

            // The rest of an image that's cut off stays blank.
            var result = codec.GetPixels(decoded.Info, decoded.GetPixels());
            if (result is not (SKCodecResult.Success or SKCodecResult.IncompleteInput))
                throw new InvalidDataException($"The image could not be decoded ({result}).");

            using var image = SKImage.FromBitmap(decoded);

            // The filter for the last step is sharp, but only looks at so many pixels.
            var doubleSize = new SKSizeI(targetSize.Width * 2, targetSize.Height * 2);
            if (image.Width <= doubleSize.Width)
                return Draw(image, targetSize, new SKSamplingOptions(SKCubicResampler.CatmullRom));

            using var halfway = Draw(
                image,
                doubleSize,
                new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.Linear)
            );

            return Draw(halfway, targetSize, new SKSamplingOptions(SKCubicResampler.CatmullRom));
        }

        private static SKImage Draw(SKImage image, SKSizeI size, SKSamplingOptions sampling)
        {
            using var surface = SKSurface.Create(
                new SKImageInfo(size.Width, size.Height, SKColorType.Rgba8888, SKAlphaType.Premul)
            );
            surface.Canvas.DrawImage(image, SKRect.Create(size), sampling);

            return surface.Snapshot();
        }
    }
}
