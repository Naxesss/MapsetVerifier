using MapsetVerifier.Checks.AllModes.General.Resources;
using MapsetVerifier.Framework.Objects;
using Xunit;

namespace MapsetVerifier.Checks.Tests.AllModes.Resources;

public class CheckImageFileFormatTests
{
    private static readonly byte[] PngHeader = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
    private static readonly byte[] JpegHeader = [0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10];
    private static readonly byte[] GifHeader = [0x47, 0x49, 0x46, 0x38, 0x39, 0x61];
    private static readonly byte[] BmpHeader = [0x42, 0x4D, 0x00, 0x00, 0x00, 0x00];

    private static List<Issue> Run(string fileName, byte[] content)
    {
        using var context = CheckTestContext.CreateFromOsuFiles(
            [("test.osu", new OsuBuilder().Title("Format Test").Build())],
            extraBinaryFiles: [(fileName, content)]
        );

        return context.RunGeneralCheck<CheckImageFileFormat>();
    }

    [Fact]
    public void PngNamedJpg_IsReported_WithItsPathInTheSongFolder()
    {
        var issue = Assert.Single(Run("sb/White.jpg", PngHeader));

        Assert.Equal(Issue.Level.Warning, issue.level);
        Assert.Contains("\"sb/White.jpg\"", issue.message);
        Assert.Contains("is a PNG file, but its name says JPG", issue.message);
    }

    [Theory]
    [InlineData("image.png", "is a JPG file, but its name says PNG")]
    [InlineData("IMAGE.PNG", "is a JPG file, but its name says PNG")]
    public void JpegNamedPng_IsReported(string fileName, string expected)
    {
        Assert.Contains(expected, Assert.Single(Run(fileName, JpegHeader)).message);
    }

    [Fact]
    public void GifAndBmpNamedJpg_AreReported()
    {
        Assert.Contains("is a GIF file", Assert.Single(Run("a.jpg", GifHeader)).message);
        Assert.Contains("is a BMP file", Assert.Single(Run("b.jpg", BmpHeader)).message);
    }

    [Theory]
    [InlineData("image.png")]
    [InlineData("image.PNG")]
    public void MatchingFormat_IsNotReported(string fileName)
    {
        Assert.Empty(Run(fileName, PngHeader));
    }

    [Theory]
    [InlineData("image.jpg")]
    [InlineData("image.jpeg")]
    public void JpegExtensions_AreBothAccepted(string fileName)
    {
        Assert.Empty(Run(fileName, JpegHeader));
    }

    [Fact]
    public void NonImageFile_IsNotReported()
    {
        Assert.Empty(Run("notes.txt", PngHeader));
    }

    [Theory]
    [InlineData(new byte[] { 0x00, 0x01, 0x02, 0x03, 0x04 })]
    [InlineData(new byte[0])]
    public void UnrecognizedContent_IsNotReported(byte[] content)
    {
        // Left to the checks that read the image, which already report it couldn't be checked.
        Assert.Empty(Run("image.jpg", content));
    }
}
