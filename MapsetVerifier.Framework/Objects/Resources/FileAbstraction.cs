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
                lock (TagLibLock)
                    return File.Create(this);
            }
            catch (Exception exception)
            {
                Log.Warning(exception, "Could not read file {FilePath}", Name);
                return null;
            }
        }
    }
}
