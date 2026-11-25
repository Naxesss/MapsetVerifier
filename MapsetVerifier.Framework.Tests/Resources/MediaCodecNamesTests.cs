using MapsetVerifier.Framework.Objects.Resources;
using Xunit;

namespace MapsetVerifier.Framework.Tests.Resources;

public class MediaCodecNamesTests
{
    [Theory]
    [InlineData("avc1", "H.264 / AVC")]
    [InlineData("H264", "H.264 / AVC")]
    [InlineData("hvc1", "H.265 / HEVC")]
    [InlineData("vp09", "VP9")]
    [InlineData("av01", "AV1")]
    [InlineData("XVID", "MPEG-4 Visual")]
    [InlineData("WMV3", "Windows Media Video")]
    [InlineData(" abcd ", "abcd")] // Unknown ones are passed through.
    [InlineData("  ", null)]
    [InlineData(null, null)]
    public void VideoFourCc(string? fourCc, string? expected) =>
        Assert.Equal(expected, MediaCodecNames.ForVideoFourCc(fourCc));

    [Theory]
    [InlineData("mp4a", "AAC")]
    [InlineData(".mp3", "MP3")]
    [InlineData("Opus", "Opus")]
    [InlineData("sowt", "PCM")]
    [InlineData("xyz1", "xyz1")]
    [InlineData("", null)]
    public void AudioFourCc(string? fourCc, string? expected) =>
        Assert.Equal(expected, MediaCodecNames.ForAudioFourCc(fourCc));

    // Registered in mmreg.h.
    [Theory]
    [InlineData(0x0001, "PCM")]
    [InlineData(0x0055, "MP3")]
    [InlineData(0x00FF, "AAC")]
    [InlineData(0x0161, "Windows Media Audio")]
    [InlineData(0x2000, "AC-3")]
    [InlineData(0xABCD, "Unknown (0xABCD)")]
    public void WaveFormatTag(int tag, string expected) =>
        Assert.Equal(expected, MediaCodecNames.ForWaveFormatTag((ushort)tag));

    // From the FLV specification.
    [Theory]
    [InlineData(2, "Sorenson H.263")]
    [InlineData(4, "VP6")]
    [InlineData(7, "H.264 / AVC")]
    [InlineData(99, "Unknown (99)")]
    public void FlvVideoCodec(int id, string expected) =>
        Assert.Equal(expected, MediaCodecNames.ForFlvVideoCodecId(id));

    [Theory]
    [InlineData(2, "MP3")]
    [InlineData(10, "AAC")]
    [InlineData(11, "Speex")]
    [InlineData(99, "Unknown (99)")]
    public void FlvAudioCodec(int id, string expected) =>
        Assert.Equal(expected, MediaCodecNames.ForFlvAudioCodecId(id));

    [Theory]
    [InlineData(66, 30, "Baseline@L3.0")]
    [InlineData(77, 31, "Main@L3.1")]
    [InlineData(100, 42, "High@L4.2")]
    [InlineData(200, 10, "Profile 200@L1.0")]
    public void AvcProfile(int profile, int level, string expected) =>
        Assert.Equal(expected, MediaCodecNames.FormatAvcProfile((byte)profile, (byte)level));

    [Theory]
    [InlineData(0x01, 120, "Main@L4")]
    [InlineData(0x22, 123, "Main 10@L4.1 High tier")]
    public void HevcProfile(int profile, int level, string expected) =>
        Assert.Equal(expected, MediaCodecNames.FormatHevcProfile((byte)profile, (byte)level));
}
