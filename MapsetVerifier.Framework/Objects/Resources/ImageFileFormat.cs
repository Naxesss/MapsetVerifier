namespace MapsetVerifier.Framework.Objects.Resources
{
    /// <summary> Tells apart what an image file claims to be from what it actually contains. </summary>
    public static class ImageFileFormat
    {
        public const string Jpeg = "JPG";
        public const string Png = "PNG";
        public const string Gif = "GIF";
        public const string Bmp = "BMP";

        /// <summary> Returns the format the file name claims, or null if it isn't an image extension. </summary>
        public static string? FromExtension(string filePath) =>
            Path.GetExtension(filePath).ToLowerInvariant() switch
            {
                ".jpg" or ".jpeg" => Jpeg,
                ".png" => Png,
                ".gif" => Gif,
                ".bmp" => Bmp,
                _ => null,
            };

        /// <summary> Returns the format the file's content starts with, or null if unknown or unreadable. </summary>
        public static string? FromContent(string filePath)
        {
            Span<byte> header = stackalloc byte[4];

            try
            {
                using var stream = new FileStream(
                    filePath,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.ReadWrite
                );
                if (stream.Read(header) < header.Length)
                    return null;
            }
            catch (Exception exception)
                when (exception is IOException or UnauthorizedAccessException)
            {
                return null;
            }

            return header switch
            {
                [0xFF, 0xD8, 0xFF, _] => Jpeg,
                [0x89, 0x50, 0x4E, 0x47] => Png,
                [0x47, 0x49, 0x46, 0x38] => Gif,
                [0x42, 0x4D, _, _] => Bmp,
                _ => null,
            };
        }

        /// <summary>
        ///     Returns the mime type to read this file with when its content is a different image format
        ///     than its name claims, otherwise null. Null for bitmaps too, since TagLib can't read those.
        /// </summary>
        public static string? GetMimeTypeIfMismatched(string filePath)
        {
            var claimed = FromExtension(filePath);
            if (claimed == null)
                return null;

            var actual = FromContent(filePath);
            if (actual == null || actual == claimed)
                return null;

            return actual switch
            {
                Jpeg => "image/jpeg",
                Png => "image/png",
                Gif => "image/gif",
                _ => null,
            };
        }
    }
}
