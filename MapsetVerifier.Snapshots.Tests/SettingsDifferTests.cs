using MapsetVerifier.Snapshots.Compare;
using MapsetVerifier.Snapshots.Diffing;
using Xunit;

namespace MapsetVerifier.Snapshots.Tests;

public class SettingsDifferTests
{
    private static List<SettingChange> Diff(string section, string before, string after) =>
        SettingsDiffer.Diff($"[{section}]\n{before}", $"[{section}]\r\n{after}");

    [Theory]
    [InlineData("General", "PreviewTime: 1000", "PreviewTime: 2000")]
    [InlineData("Metadata", "Title:A", "Title:B")]
    [InlineData("Difficulty", "ApproachRate:9", "ApproachRate:9.2")]
    [InlineData("Colours", "Combo1 : 255,0,0", "Combo1 : 254,0,0")]
    public void ChangedValue_IsReportedAsWritten(string section, string before, string after)
    {
        var change = Assert.Single(Diff(section, before, after));

        Assert.Equal(section, change.Section);
        Assert.Equal(before.Split(':')[0].Trim(), change.Key);
        Assert.Equal(ChangeOp.Changed, change.Op);
        Assert.Equal(before.Split(':')[1].Trim(), change.Before);
        Assert.Equal(after.Split(':')[1].Trim(), change.After);
    }

    [Fact]
    public void AddedAndRemovedValues()
    {
        var added = Assert.Single(Diff("Colours", "", "Combo1 : 1,2,3"));
        Assert.Equal(ChangeOp.Added, added.Op);
        Assert.Equal("1,2,3", added.After);

        var removed = Assert.Single(Diff("Colours", "Combo1 : 1,2,3", ""));
        Assert.Equal(ChangeOp.Removed, removed.Op);
        Assert.Equal("1,2,3", removed.Before);
    }

    [Theory]
    // Lazer writes more decimals, and an alpha for colours.
    [InlineData("Difficulty", "ApproachRate:9.2", "ApproachRate:9.199999809265137")]
    [InlineData("General", "StackLeniency: 0.7", "StackLeniency: 0.700")]
    [InlineData("Colours", "Combo1 : 255,0,0", "Combo1 : 255,0,0,255")]
    // Ids appear on submission, and the view state of the editor isn't part of the map.
    [InlineData("Metadata", "BeatmapID:0", "BeatmapID:123")]
    [InlineData("Editor", "TimelineZoom: 1", "TimelineZoom: 2")]
    [InlineData("Editor", "BeatDivisor: 4", "BeatDivisor: 8")]
    public void SameMeaning_IsNoChange(string section, string before, string after)
    {
        Assert.Empty(Diff(section, before, after));
    }

    [Fact]
    public void TranslucentColour_IsAChange()
    {
        Assert.Single(Diff("Colours", "Combo1 : 255,0,0", "Combo1 : 255,0,0,128"));
    }

    [Theory]
    [InlineData("Metadata", "Tags:one two three", "Tags:three one four")]
    [InlineData("Editor", "Bookmarks: one,two,three", "Bookmarks: three,one,four")]
    public void Lists_SayWhatWasAddedAndRemoved(string section, string before, string after)
    {
        var change = Assert.Single(Diff(section, before, after));

        Assert.Equal(["four"], change.Added!);
        Assert.Equal(["two"], change.Removed!);
    }

    [Theory]
    [InlineData("0,0,\"a.jpg\",0,0", "0,0,\"b.jpg\",0,0", "Background", "a.jpg", "b.jpg")]
    [InlineData("1,0,\"a.mp4\"", "1,0,\"b.mp4\"", "Video", "a.mp4", "b.mp4")]
    [InlineData("0,0,\"a.jpg\",0,0", "0,0,\"a.jpg\",0,20", "Background position", "0, 0", "0, 20")]
    [InlineData("1,0,\"a.mp4\"", "1,-50,\"a.mp4\"", "Video start time", "0 ms", "-50 ms")]
    public void ReplacedFile_OrItsPlacement(
        string before,
        string after,
        string key,
        string oldValue,
        string newValue
    )
    {
        var change = Assert.Single(Diff("Events", before, after));

        Assert.Equal(key, change.Key);
        Assert.Equal(ChangeOp.Changed, change.Op);
        Assert.Equal(oldValue, change.Before);
        Assert.Equal(newValue, change.After);
    }

    [Fact]
    public void AddedVideo_NextToTheBackground()
    {
        var change = Assert.Single(
            Diff("Events", "0,0,\"a.jpg\",0,0", "0,0,\"a.jpg\",0,0\n1,0,\"a.mp4\"")
        );

        Assert.Equal("Video", change.Key);
        Assert.Equal(ChangeOp.Added, change.Op);
        Assert.Equal("a.mp4", change.After);
    }

    [Fact]
    public void VideoWrittenByName_IsAVideo()
    {
        // As the editor writes it.
        var change = Assert.Single(Diff("Events", "Video,0,\"a.mp4\"", "Video,0,\"b.mp4\""));

        Assert.Equal("Video", change.Key);
    }

    [Fact]
    public void Breaks_PairUpByAnEdge()
    {
        var changes = Diff("Events", "2,1000,5000\n2,8000,9000", "2,1200,5000\nBreak,20000,21000");

        Assert.Equal(
            [
                (ChangeOp.Changed, "1000-5000", "1200-5000"),
                (ChangeOp.Removed, "8000-9000", null),
                (ChangeOp.Added, null, "20000-21000"),
            ],
            changes.Select(change => (change.Op, change.Before, change.After))
        );
        Assert.All(changes, change => Assert.Equal("Break", change.Key));
    }

    [Fact]
    public void Storyboard_IsSummarised_WithoutItsCommands()
    {
        var change = Assert.Single(
            Diff(
                "Events",
                "Sprite,Pass,Centre,\"a.png\",0,0\n F,0,0,100,1",
                "Sprite,Pass,Centre,\"b.png\",0,0\n F,0,0,200,1\n"
                    + "Sprite,Pass,Centre,\"c.png\",0,0\n_F,0,0,200,1\n//comment"
            )
        );

        Assert.Equal("Storyboard", change.Key);
        Assert.Equal(["2 sprites, samples or animations added"], change.Added!);
        Assert.Equal(["1 removed"], change.Removed!);
    }
}
