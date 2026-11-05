using Serilog;
using File = TagLib.File;

namespace MapsetVerifier.Framework.Objects.Resources
{
    public class FileAbstraction(string filePath) : File.IFileAbstraction
    {
        /// <summary>
        ///     TagLibSharp 2.3.0 shares unprotected static state while reading EXIF data, so reading files
        ///     in parallel randomly fails. Fixed in their source, so this can go once a newer version is out.
        /// </summary>
        private static readonly object TagLibLock = new();

        public string? Error = null;

        public string Name { get; } = filePath;

        public Stream ReadStream { get; } =
            new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

        public Stream WriteStream => ReadStream;

        public void CloseStream(Stream stream) => stream.Position = 0;

        public File? GetTagFile()
        {
            try
            {
                // TagLib picks its parser from the extension, which isn't always the actual format.
                var mimeType = ImageFileFormat.GetMimeTypeIfMismatched(Name);

                lock (TagLibLock)
                    return mimeType == null
                        ? File.Create(this)
                        : File.Create(this, mimeType, TagLib.ReadStyle.Average);
            }
            catch (Exception exception)
            {
                Log.Warning(exception, "Could not read file {FilePath}", Name);
                return null;
            }
        }
    }
}
