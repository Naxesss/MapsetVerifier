using System.IO;
using File = TagLib.File;

namespace MapsetVerifier.Framework.Objects.Resources
{
    public class FileAbstraction : File.IFileAbstraction
    {
        public string error;

        public FileAbstraction(string filePath)
        {
            error = null;

            ReadStream = filePath != null ? new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite) : null;

            Name = filePath;
        }

        public string Name { get; }

        public Stream ReadStream { get; }

        public Stream WriteStream => ReadStream;

        public void CloseStream(Stream stream) => stream.Position = 0;

        public File GetTagFile()
        {
            if (Name == null)
            {
                error = "Name cannot be null.";

                return null;
            }

            if (ReadStream == null)
            {
                error = "Could not open file for reading.";

                return null;
            }

            return File.Create(this);
        }
    }
}