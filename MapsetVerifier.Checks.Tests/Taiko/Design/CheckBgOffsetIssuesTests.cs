using System.IO.Compression;
using MapsetVerifier.Checks.Taiko.Design;
using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Parser.Objects;
using Xunit;

namespace MapsetVerifier.Checks.Tests.Taiko.Design;

public class CheckBgOffsetIssuesTests
{
    [Fact]
    public void FlagsInconsistentBackgroundOffsetsAcrossTaikoDifficulties()
    {
        using var context = CheckTestContext.CreateFromOsuFiles(
            [
                ("kantan.osu", BuildTaikoOsu("Kantan", "0,0,\"bg.png\",0,20")),
                ("oni.osu", BuildTaikoOsu("Oni", "0,0,\"bg.png\",0,0")),
            ],
            extraFiles: ["bg.png"]
        );

        var issues = context.RunBeatmapSetCheck<CheckBgOffsetIssues>();

        Assert.Equal(2, issues.Count);
        Assert.All(issues, issue => Assert.Equal(Issue.Level.Warning, issue.level));
        Assert.Contains(
            issues,
            issue => issue.message.Contains("Kantan") && issue.message.Contains("0, 20")
        );
        Assert.Contains(
            issues,
            issue => issue.message.Contains("Oni") && issue.message.Contains("0, 0")
        );
    }

    [Fact]
    public void DoesNotFlagConsistentYOnlyBackgroundOffsets()
    {
        using var context = CheckTestContext.CreateFromOsuFiles(
            [
                ("kantan.osu", BuildTaikoOsu("Kantan", "0,0,\"bg.png\",0,20")),
                ("futsuu.osu", BuildTaikoOsu("Futsuu", "0,0,\"bg.png\",0,20")),
            ],
            extraFiles: ["bg.png"]
        );

        var issues = context.RunBeatmapSetCheck<CheckBgOffsetIssues>();

        Assert.Empty(issues);
    }

    [Fact]
    public void FlagsNonZeroXOffsetOnIndividualDifficulties()
    {
        using var context = CheckTestContext.CreateFromOsuFiles(
            [("oni.osu", BuildTaikoOsu("Oni", "0,0,\"bg.png\",12,20"))],
            extraFiles: ["bg.png"]
        );

        var issues = context.RunBeatmapSetCheck<CheckBgOffsetIssues>();

        var issue = Assert.Single(issues);
        Assert.Equal(Issue.Level.Warning, issue.level);
        Assert.Equal("Oni", issue.beatmap?.MetadataSettings.version);
        Assert.Contains("X offset of 12", issue.message);
        Assert.Contains("Ensure this is intentional", issue.message);
    }

    [Fact]
    public void FlagsNonZeroXOffsetEvenWhenConsistentAcrossDifficulties()
    {
        using var context = CheckTestContext.CreateFromOsuFiles(
            [
                ("kantan.osu", BuildTaikoOsu("Kantan", "0,0,\"bg.png\",5,20")),
                ("futsuu.osu", BuildTaikoOsu("Futsuu", "0,0,\"bg.png\",5,20")),
            ],
            extraFiles: ["bg.png"]
        );

        var issues = context.RunBeatmapSetCheck<CheckBgOffsetIssues>();

        Assert.Equal(2, issues.Count);
        Assert.All(
            issues,
            issue =>
            {
                Assert.Equal(Issue.Level.Warning, issue.level);
                Assert.Contains("X offset of 5", issue.message);
            }
        );
    }

    [Fact]
    public void DoesNotCompareAgainstNonTaikoBeatmaps()
    {
        using var context = CheckTestContext.CreateFromOsuFiles(
            [
                ("taiko.osu", BuildTaikoOsu("Oni", "0,0,\"bg.png\",0,0")),
                ("standard.osu", BuildOsu(Beatmap.Mode.Standard, "Hard", "0,0,\"bg.png\",0,20")),
            ],
            extraFiles: ["bg.png"]
        );

        var issues = context.RunBeatmapSetCheck<CheckBgOffsetIssues>();

        Assert.Empty(issues);
    }

    [Theory]
    [InlineData(16, 9, 115)]
    [InlineData(4, 3, 200)]
    [InlineData(2, 1, 86)]
    public void VerticalOffsetLimitFollowsAspectRatio(int width, int height, int expected) =>
        Assert.Equal(expected, CheckBgOffsetIssues.MaxVerticalOffset(width, height));

    [Theory]
    [InlineData(160, 90, 115, true)]
    [InlineData(160, 90, 114, false)]
    [InlineData(160, 120, 200, true)]
    [InlineData(160, 120, 199, false)]
    [InlineData(200, 100, 86, true)]
    [InlineData(200, 100, 85, false)]
    public void FlagsVerticalOffsetAtOrAboveTheAspectRatioLimit(
        int width,
        int height,
        int yOffset,
        bool shouldFlag
    )
    {
        using var context = CheckTestContext.CreateFromOsuFiles(
            [("oni.osu", BuildTaikoOsu("Oni", $"0,0,\"bg.png\",0,{yOffset}"))],
            extraBinaryFiles: [("bg.png", CreatePng(width, height))]
        );

        var issues = context
            .RunBeatmapSetCheck<CheckBgOffsetIssues>()
            .Where(issue => issue.message.Contains("vertical offset"))
            .ToList();

        if (!shouldFlag)
        {
            Assert.Empty(issues);
            return;
        }

        var issue = Assert.Single(issues);
        Assert.Equal(Issue.Level.Warning, issue.level);
        Assert.Contains($"vertical offset ({yOffset})", issue.message);
    }

    private static string BuildTaikoOsu(string version, string backgroundLine) =>
        BuildOsu(Beatmap.Mode.Taiko, version, backgroundLine);

    private static string BuildOsu(Beatmap.Mode mode, string version, string backgroundLine) =>
        new OsuBuilder()
            .Mode(mode)
            .Title("Background Offset Test")
            .Artist("MapsetVerifier")
            .Version(version)
            .Events(backgroundLine)
            .WithDefaultTiming()
            .WithDefaultHitObject()
            .Build();

    private static byte[] CreatePng(int width, int height)
    {
        var raw = new byte[height * (1 + width * 3)];
        using var deflated = new MemoryStream();
        using (var zlib = new ZLibStream(deflated, CompressionLevel.SmallestSize, true))
            zlib.Write(raw);

        var idat = deflated.ToArray();
        using var png = new MemoryStream();
        png.Write(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 });
        WriteChunk(png, "IHDR", Ihdr(width, height));
        WriteChunk(png, "IDAT", idat);
        WriteChunk(png, "IEND", []);
        return png.ToArray();
    }

    private static byte[] Ihdr(int width, int height)
    {
        var data = new byte[13];
        WriteInt(data, 0, width);
        WriteInt(data, 4, height);
        data[8] = 8;
        data[9] = 2;
        return data;
    }

    private static void WriteChunk(Stream stream, string type, byte[] data)
    {
        var typeBytes = System.Text.Encoding.ASCII.GetBytes(type);
        var crcSource = new byte[typeBytes.Length + data.Length];
        typeBytes.CopyTo(crcSource, 0);
        data.CopyTo(crcSource, typeBytes.Length);

        Span<byte> length = stackalloc byte[4];
        System.Buffers.Binary.BinaryPrimitives.WriteInt32BigEndian(length, data.Length);
        stream.Write(length);
        stream.Write(typeBytes);
        stream.Write(data);

        Span<byte> crc = stackalloc byte[4];
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32BigEndian(crc, PngCrc(crcSource));
        stream.Write(crc);
    }

    private static void WriteInt(byte[] data, int offset, int value)
    {
        System.Buffers.Binary.BinaryPrimitives.WriteInt32BigEndian(data.AsSpan(offset, 4), value);
    }

    private static uint PngCrc(byte[] data)
    {
        uint crc = 0xFFFFFFFF;
        foreach (var value in data)
        {
            crc ^= value;
            for (var i = 0; i < 8; i++)
                crc = (crc & 1) != 0 ? (crc >> 1) ^ 0xEDB88320 : crc >> 1;
        }

        return ~crc;
    }
}
