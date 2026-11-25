using MapsetVerifier.Framework.Objects.Resources;
using Xunit;

namespace MapsetVerifier.Framework.Tests.Resources;

public sealed class ImageSizeTests : IDisposable
{
    private readonly string folder = Directory
        .CreateTempSubdirectory("MapsetVerifierFrameworkTests")
        .FullName;

    public void Dispose() => Directory.Delete(folder, true);

    private string Write(string name, byte[] content)
    {
        var path = Path.Combine(folder, name);
        File.WriteAllBytes(path, content);

        return path;
    }

    /// <summary> The smallest possible gif of that size. </summary>
    private static byte[] Gif(byte width, byte height) =>
        [.. "GIF89a"u8, width, 0, height, 0, 0, 0, 0, 0x3B];

    [Fact]
    public void Size_IsThatOfTheImage()
    {
        var path = Write("a.gif", Gif(30, 20));

        Assert.Equal(new System.Drawing.Size(30, 20), ImageSize.Read(path));
        // And the file is free again.
        File.Delete(path);
    }

    [Fact]
    public void Size_OfWhatIsNoImage_IsUnknown()
    {
        Assert.Null(ImageSize.Read(Path.Combine(folder, "missing.png")));
        Assert.Null(ImageSize.Read(Write("a.png", [1, 2, 3])));
        Assert.Null(ImageSize.Read(Write("a.gif", Gif(0, 0))));
        Assert.Null(ImageSize.Read(Write("a.xyz", [1, 2, 3])));
    }

    [Fact]
    public void Size_OfAFileInUse_IsUnknown()
    {
        var path = Write("a.gif", Gif(30, 20));
        using var locked = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.None);

        // Elsewhere, files aren't locked.
        if (OperatingSystem.IsWindows())
            Assert.Null(ImageSize.Read(path));
    }
}
