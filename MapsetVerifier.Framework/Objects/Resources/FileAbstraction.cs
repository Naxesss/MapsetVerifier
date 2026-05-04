using File = TagLib.File;

namespace MapsetVerifier.Framework.Objects.Resources
{
    public class FileAbstraction(string filePath) : File.IFileAbstraction
    {
        public string? Error = null;

        public string Name { get; } = filePath;

        public Stream ReadStream { get; } = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

        public Stream WriteStream => ReadStream;

        public void CloseStream(Stream stream) => stream.Position = 0;

        public File? GetTagFile()
        {
            try
            {
                return File.Create(this);
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}