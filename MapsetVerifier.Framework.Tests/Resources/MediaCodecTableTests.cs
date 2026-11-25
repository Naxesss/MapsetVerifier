using MapsetVerifier.Framework.Objects.Resources;
using Xunit;

namespace MapsetVerifier.Framework.Tests.Resources;

/// <summary> Every identifier known by name. </summary>
public class MediaCodecTableTests
{
    [Theory]
    [InlineData("H.264 / AVC", "avc1 avc3 h264 x264")]
    [InlineData("H.265 / HEVC", "hev1 hvc1 h265 hevc")]
    [InlineData("VP8", "vp08 vp8")]
    [InlineData("VP9", "vp09 vp90")]
    [InlineData("AV1", "av01")]
    [InlineData("MPEG-4 Visual", "mp4v divx dx50 xvid fmp4")]
    [InlineData("H.263", "s263 h263")]
    [InlineData("Motion JPEG", "mjpg jpeg")]
    [InlineData("Windows Media Video", "wmv1 wmv2 wmv3 wvc1")]
    [InlineData("MPEG-1/2 Video", "mpg1 mpg2 mpeg")]
    public void Video(string expected, string codes)
    {
        foreach (var code in codes.Split(' '))
            Assert.Equal(expected, MediaCodecNames.ForVideoFourCc(code.ToUpperInvariant()));

        foreach (var code in new[] { "a", "abc1", "abc2", "hvxx", "vp0x", "wmv9x", "mpxx" })
            Assert.Equal(code, MediaCodecNames.ForVideoFourCc(code));
    }

    [Theory]
    [InlineData("AAC", "mp4a")]
    [InlineData("MP3", ".mp3 mp3")]
    [InlineData("AC-3", "ac-3")]
    [InlineData("E-AC-3", "ec-3")]
    [InlineData("ALAC", "alac")]
    [InlineData("Opus", "opus")]
    [InlineData("FLAC", "flac")]
    [InlineData("AMR", "samr")]
    [InlineData("PCM", "sowt twos lpcm in24 in32")]
    public void Audio(string expected, string codes)
    {
        foreach (var code in codes.Split(' '))
            Assert.Equal(expected, MediaCodecNames.ForAudioFourCc(code.ToUpperInvariant()));

        foreach (var code in new[] { "a", "mp4x", "acxx", "soxx", "inxx", "abcd" })
            Assert.Equal(code, MediaCodecNames.ForAudioFourCc(code));
    }

    [Theory]
    [InlineData("PCM", 0x0001)]
    [InlineData("ADPCM", 0x0002)]
    [InlineData("IEEE Float", 0x0003)]
    [InlineData("MP3", 0x0055)]
    [InlineData("AC-3", 0x0092, 0x2000)]
    [InlineData("AAC", 0x00FF, 0x1600, 0x1601)]
    [InlineData("Windows Media Audio", 0x0160, 0x0161, 0x0162, 0x0163)]
    [InlineData("DTS", 0x2001)]
    [InlineData("Vorbis", 0x674F, 0x6750, 0x6751)]
    [InlineData("Unknown (0x0004)", 0x0004)]
    public void Wave(string expected, params int[] tags)
    {
        foreach (var tag in tags)
            Assert.Equal(expected, MediaCodecNames.ForWaveFormatTag((ushort)tag));
    }

    [Fact]
    public void Flv()
    {
        string[] video =
        [
            "Unknown (0)",
            "JPEG",
            "Sorenson H.263",
            "Screen Video",
            "VP6",
            "VP6 with alpha",
            "Screen Video 2",
            "H.264 / AVC",
            "Unknown (8)",
        ];
        string[] audio =
        [
            "PCM",
            "ADPCM",
            "MP3",
            "PCM (little endian)",
            "Nellymoser",
            "Nellymoser",
            "Nellymoser",
            "G.711 A-law",
            "G.711 mu-law",
            "Unknown (9)",
            "AAC",
            "Speex",
            "Unknown (12)",
        ];

        Assert.Equal(video, Enumerable.Range(0, 9).Select(MediaCodecNames.ForFlvVideoCodecId));
        Assert.Equal(audio, Enumerable.Range(0, 13).Select(MediaCodecNames.ForFlvAudioCodecId));
        Assert.Equal("H.265 / HEVC", MediaCodecNames.ForFlvVideoCodecId(12));
        Assert.Equal("MP3 (8 kHz)", MediaCodecNames.ForFlvAudioCodecId(14));
    }

    [Theory]
    [InlineData(66, "Baseline@L4.0")]
    [InlineData(77, "Main@L4.0")]
    [InlineData(88, "Extended@L4.0")]
    [InlineData(100, "High@L4.0")]
    [InlineData(110, "High 10@L4.0")]
    [InlineData(122, "High 4:2:2@L4.0")]
    [InlineData(244, "High 4:4:4@L4.0")]
    [InlineData(1, "Profile 1@L4.0")]
    public void Avc(byte profile, string expected) =>
        Assert.Equal(expected, MediaCodecNames.FormatAvcProfile(profile, 40));

    [Theory]
    [InlineData(1, "Main@L4.1")]
    [InlineData(2, "Main 10@L4.1")]
    [InlineData(3, "Main Still Picture@L4.1")]
    [InlineData(9, "Profile 9@L4.1")]
    [InlineData(0x21, "Main@L4.1 High tier")]
    public void Hevc(byte profile, string expected) =>
        Assert.Equal(expected, MediaCodecNames.FormatHevcProfile(profile, 123));

    [Theory]
    [InlineData("a.jpg", "JPG")]
    [InlineData("a.JPEG", "JPG")]
    [InlineData("a.png", "PNG")]
    [InlineData("a.gif", "GIF")]
    [InlineData("a.bmp", "BMP")]
    [InlineData("a.webp", null)]
    [InlineData("a", null)]
    public void ImageExtensions(string path, string? expected) =>
        Assert.Equal(expected, ImageFileFormat.FromExtension(path));

    [Theory]
    [InlineData("a.png", new byte[] { 0xFF, 0xD8, 0xFF, 0 }, "image/jpeg")]
    [InlineData("a.jpg", new byte[] { 0x89, 0x50, 0x4E, 0x47 }, "image/png")]
    [InlineData("a.jpg", new byte[] { 0x47, 0x49, 0x46, 0x38 }, "image/gif")]
    [InlineData("a.jpg", new byte[] { 0x42, 0x4D, 0, 0 }, null)]
    [InlineData("a.jpg", new byte[] { 0xFF, 0xD8, 0xFF, 0 }, null)]
    [InlineData("a.jpg", new byte[] { 1, 2, 3, 4 }, null)]
    [InlineData("a.jpg", new byte[] { 1 }, null)]
    [InlineData("a.txt", new byte[] { 0x89, 0x50, 0x4E, 0x47 }, null)]
    public void MismatchedImages(string name, byte[] content, string? expected)
    {
        var folder = Directory.CreateTempSubdirectory("mv-image-tests").FullName;

        try
        {
            var path = Path.Combine(folder, name);
            File.WriteAllBytes(path, content);

            Assert.Equal(expected, ImageFileFormat.GetMimeTypeIfMismatched(path));
        }
        finally
        {
            Directory.Delete(folder, true);
        }
    }

    [Fact]
    public void MissingImage_HasNoFormat()
    {
        Assert.Null(ImageFileFormat.FromContent(Path.Combine(Path.GetTempPath(), "mv-none.png")));
    }
}
