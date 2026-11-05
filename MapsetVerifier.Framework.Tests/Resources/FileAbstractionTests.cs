using MapsetVerifier.Framework.Objects.Resources;
using Xunit;

namespace MapsetVerifier.Framework.Tests.Resources;

public sealed class FileAbstractionTests : IDisposable
{
    // The smallest JPEG that makes TagLib read EXIF data: a 1x1 image with an empty EXIF block.
    private static readonly byte[] JpegWithExif =
    [
        0xFF,
        0xD8,
        0xFF,
        0xE1,
        0x00,
        0x16,
        0x45,
        0x78,
        0x69,
        0x66,
        0x00,
        0x00,
        0x49,
        0x49,
        0x2A,
        0x00,
        0x08,
        0x00,
        0x00,
        0x00,
        0x00,
        0x00,
        0x00,
        0x00,
        0x00,
        0x00,
        0xFF,
        0xC0,
        0x00,
        0x0B,
        0x08,
        0x00,
        0x01,
        0x00,
        0x01,
        0x01,
        0x01,
        0x11,
        0x00,
        0xFF,
        0xDA,
        0x00,
        0x08,
        0x01,
        0x01,
        0x00,
        0x00,
        0x3F,
        0x00,
        0x00,
        0xFF,
        0xD9,
    ];

    private readonly string directory = Directory
        .CreateTempSubdirectory("MapsetVerifierTagLib")
        .FullName;

    private string WriteImage(string fileName, byte[] content)
    {
        var path = Path.Combine(directory, fileName);
        File.WriteAllBytes(path, content);

        return path;
    }

    private static TagLib.File? Read(string path)
    {
        var abstraction = new FileAbstraction(path);
        try
        {
            return abstraction.GetTagFile();
        }
        finally
        {
            abstraction.ReadStream.Dispose();
        }
    }

    [Fact]
    public void JpegWithExif_IsRead()
    {
        var tagFile = Read(WriteImage("image.jpg", JpegWithExif));

        Assert.NotNull(tagFile);
        Assert.Equal(1, tagFile.Properties.PhotoWidth);
    }

    [Fact]
    public void JpegWithExif_ReadInParallel_AlwaysSucceeds()
    {
        var path = WriteImage("image.jpg", JpegWithExif);
        var failures = 0;

        Parallel.For(
            0,
            5000,
            new ParallelOptions { MaxDegreeOfParallelism = 16 },
            _ =>
            {
                if (Read(path) == null)
                    Interlocked.Increment(ref failures);
            }
        );

        Assert.Equal(0, failures);
    }

    [Fact]
    public void UnreadableFile_ReturnsNull()
    {
        // A PNG signature in a file claiming to be a JPEG.
        var tagFile = Read(
            WriteImage("image.jpg", [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A])
        );

        Assert.Null(tagFile);
    }

    public void Dispose() => Directory.Delete(directory, true);
}
