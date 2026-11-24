using MapsetVerifier.Checks.AllModes.General.Resources;
using MapsetVerifier.Framework.Objects;
using Xunit;

namespace MapsetVerifier.Checks.Tests.AllModes.Resources;

public class CheckBgPresenceTests
{
    private static List<Issue> Run(string?[] backgrounds, params string[] files)
    {
        using var context = CheckTestContext.CreateFromOsuFiles(
            backgrounds.Select(
                (background, i) =>
                    (
                        $"{i}.osu",
                        new OsuBuilder()
                            .Version($"Diff {i}")
                            .Events(background == null ? [] : [$"0,0,\"{background}\",0,0"])
                            .WithDefaultTiming()
                            .WithDefaultHitObject()
                            .Build()
                    )
            ),
            files
        );

        return context.RunGeneralCheck<CheckBgPresence>();
    }

    [Fact]
    public void ExistingBackgrounds_AreFine()
    {
        Assert.Empty(Run(["bg.jpg", "sb/bg2.jpg"], "bg.jpg", "sb/bg2.jpg"));
    }

    [Fact]
    public void NoneAtAll_IsASingleProblem()
    {
        Assert.Equal(Issue.Level.Problem, Assert.Single(Run([null, null])).level);
    }

    [Fact]
    public void NoneInOneDifficulty_IsAProblem()
    {
        Assert.Contains("Diff 1", Assert.Single(Run(["bg.jpg", null], "bg.jpg")).message);
    }

    [Fact]
    public void MissingFile_IsAProblem()
    {
        var issue = Assert.Single(Run(["bg.jpg"]));

        Assert.Equal(Issue.Level.Problem, issue.level);
        Assert.Contains("missing its background file, \"bg.jpg\"", issue.message);
    }
}
