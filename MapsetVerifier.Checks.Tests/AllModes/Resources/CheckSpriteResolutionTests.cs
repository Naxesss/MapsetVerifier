using MapsetVerifier.Checks.AllModes.General.Resources;
using MapsetVerifier.Framework.Objects;
using Xunit;

namespace MapsetVerifier.Checks.Tests.AllModes.Resources;

public class CheckSpriteResolutionTests
{
    private static List<Issue> Run(string line, string file, byte[]? content, bool osb = false)
    {
        var builder = new OsuBuilder()
            .Artist("Artist")
            .Title("Title")
            .Creator("Creator")
            .WithDefaultTiming()
            .WithDefaultHitObject();
        if (!osb)
            builder.Events(line);

        using var context = CheckTestContext.CreateFromOsuFiles(
            [("test.osu", builder.Build())],
            extraFileContents: osb ? [("Artist - Title (Creator).osb", "[Events]\n" + line)] : null,
            extraBinaryFiles: content == null ? null : [(file, content)]
        );

        return context.RunGeneralCheck<CheckSpriteResolution>();
    }

    private const string Sprite = "Sprite,Pass,Centre,\"a.png\",0,0";
    private const string Animation = "Animation,Pass,Centre,\"a.png\",0,0,1,100,LoopOnce";

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void AboveSeventeenMillionPixels_IsAProblem(bool osb)
    {
        Assert.Empty(Run(Sprite, "a.png", TestMedia.Png(17000, 1000), osb));
        Assert.Empty(Run(Animation, "a0.png", TestMedia.Png(17000, 1000), osb));

        var sprite = Assert.Single(Run(Sprite, "a.png", TestMedia.Png(17000, 1001), osb));
        Assert.Equal(Issue.Level.Problem, sprite.level);
        Assert.Equal("\"a.png\"", sprite.message);

        var frame = Assert.Single(Run(Animation, "a0.png", TestMedia.Png(17000, 1001), osb));
        Assert.Equal("\"a0.png\" (Animation Frame)", frame.message);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void MissingFile_IsAWarning(bool osb)
    {
        var issue = Assert.Single(Run(Sprite, "a.png", null, osb));

        Assert.Equal(Issue.Level.Warning, issue.level);
        Assert.Contains("missing", issue.message);
    }
}
