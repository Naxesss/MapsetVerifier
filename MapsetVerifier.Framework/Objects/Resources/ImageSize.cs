using System.Drawing;

namespace MapsetVerifier.Framework.Objects.Resources
{
    public static class ImageSize
    {
        /// <summary> Returns the size of the image at the given path, or null if it's missing or can't be read. </summary>
        public static Size? Read(string filePath)
        {
            if (!File.Exists(filePath))
                return null;

            try
            {
                var abstraction = new FileAbstraction(filePath);

                using (abstraction.ReadStream)
                {
                    var properties = abstraction.GetTagFile()?.Properties;

                    return properties is { PhotoWidth: > 0, PhotoHeight: > 0 }
                        ? new Size(properties.PhotoWidth, properties.PhotoHeight)
                        : null;
                }
            }
            catch (Exception exception)
                when (exception is IOException or UnauthorizedAccessException)
            {
                return null;
            }
        }
    }
}
