using MapsetVerifier.Framework.Objects.Resources;
using SkiaSharp;
using Xunit;

namespace MapsetVerifier.Framework.Tests.Resources;

public sealed class ImageThumbnailTests : IDisposable
{
    private static readonly SKColor Colour = new(10, 120, 230);

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

    private static byte[] Image(
        int width,
        int height,
        SKEncodedImageFormat format = SKEncodedImageFormat.Png
    )
    {
        using var bitmap = new SKBitmap(width, height);
        bitmap.Erase(Colour);
        using var data = bitmap.Encode(format, 100);

        return data.ToArray();
    }

    [Theory]
    [InlineData(1260, 504, 315)]
    [InlineData(300, 150, 252)] // Less than half is taken off.
    [InlineData(4, 4000, 1)]
    [InlineData(4000, 130, 3877)]
    public void Thumbnail_KeepsTheShapeAndColours(int width, int height, int expectedWidth)
    {
        var path = Write("a.png", Image(width, height));

        using var thumbnail = ImageThumbnail.Create(path, 126, ImageFileFormat.Png);
        using var decoded = SKBitmap.Decode(thumbnail);

        Assert.Equal((expectedWidth, 126), (decoded.Width, decoded.Height));
        Assert.Equal(Colour, decoded.GetPixel(0, 0));
        Assert.Equal(Colour, decoded.GetPixel(expectedWidth - 1, 125));
    }

    [Theory]
    [InlineData(ImageFileFormat.Png)]
    [InlineData(ImageFileFormat.Jpeg)]
    public void Thumbnail_HasTheGivenFormat_WhateverTheImageHas(string format)
    {
        foreach (var from in new[] { SKEncodedImageFormat.Png, SKEncodedImageFormat.Jpeg })
        {
            // Such as those of lazer, which are named by their hash.
            var path = Write("0a1b2c", Image(400, 400, from));
            using var thumbnail = ImageThumbnail.Create(path, 100, format);

            Assert.Equal(
                format,
                ImageFileFormat.FromContent(Write("thumbnail", thumbnail.ToArray()))
            );
        }
    }

    [Theory]
    [InlineData(127, true)]
    [InlineData(126, false)]
    public void Height_IsThatOfTheImage(int height, bool expected)
    {
        var path = Write("a.png", Image(10, height));

        Assert.Equal(expected, ImageThumbnail.IsHigherThan(path, 126));
        // And the file is free again.
        File.Delete(path);
    }

    [Fact]
    public void Height_OfWhatIsNoImage_IsUnknown()
    {
        Assert.False(ImageThumbnail.IsHigherThan(Write("a.png", [1, 2, 3]), 126));
        Assert.False(ImageThumbnail.IsHigherThan(Path.Combine(folder, "missing.png"), 126));
    }

    [Fact]
    public void Thumbnail_OfWhatIsNoImage_Fails()
    {
        Assert.Throws<InvalidDataException>(() =>
            ImageThumbnail.Create(Write("a.png", [1, 2, 3]), 126, ImageFileFormat.Png)
        );
        Assert.Throws<IOException>(() =>
            ImageThumbnail.Create(Path.Combine(folder, "missing.png"), 126, ImageFileFormat.Png)
        );
    }

    [Fact]
    public void Thumbnail_OfAnImageCutOff_ShowsWhatIsThere()
    {
        var image = Image(1000, 1000);
        var path = Write("a.png", image[..(image.Length - 100)]);

        using var thumbnail = ImageThumbnail.Create(path, 126, ImageFileFormat.Png);
        using var decoded = SKBitmap.Decode(thumbnail);

        Assert.Equal(Colour, decoded.GetPixel(10, 1));
    }

    [Fact]
    public void Thumbnail_OfAnImageInUse_CanBeMade()
    {
        var path = Write("a.png", Image(400, 400));
        using var editing = new FileStream(path, FileMode.Open, FileAccess.Write, FileShare.Read);

        Assert.True(ImageThumbnail.IsHigherThan(path, 100));
    }
}
