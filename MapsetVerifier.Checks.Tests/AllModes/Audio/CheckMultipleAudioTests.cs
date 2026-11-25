using MapsetVerifier.Checks.AllModes.General.Audio;
using MapsetVerifier.Framework.Objects;
using Xunit;

namespace MapsetVerifier.Checks.Tests.AllModes.Audio;

public class CheckMultipleAudioTests
{
    private static List<Issue> Run(string[] audioNames, params string[] files)
    {
        using var context = CheckTestContext.CreateFromOsuFiles(
            audioNames.Select(
                (audio, i) =>
                    (
                        $"{i}.osu",
                        new OsuBuilder()
                            .AudioFilename(audio)
                            .Version($"Diff {i}")
                            .WithDefaultTiming()
                            .WithDefaultHitObject()
                            .Build()
                    )
            ),
            files
        );

        return context.RunGeneralCheck<CheckMultipleAudio>();
    }

    [Fact]
    public void SameAudio_IsFine()
    {
        Assert.Empty(Run(["a.mp3", "a.mp3"], "a.mp3"));
    }

    [Fact]
    public void DifferentAudio_ListsTheDifficultiesOfEach()
    {
        var issues = Run(["a.mp3", "b.mp3", "a.mp3"], "a.mp3", "b.mp3");

        Assert.All(issues, issue => Assert.Equal(Issue.Level.Problem, issue.level));
        Assert.Equal(
            ["a.mp3 : [Diff 0] [Diff 2]", "b.mp3 : [Diff 1]"],
            issues.Select(issue => issue.message).Order()
        );
    }

    [Fact]
    public void MissingFromOne_CountsAsDifferent()
    {
        Assert.Contains(
            Run(["a.mp3", "b.mp3"], "a.mp3"),
            issue => issue.message == "None : [Diff 1]"
        );
    }

    [Fact]
    public void MissingFromAll_IsAProblemForEach()
    {
        Assert.Equal(2, Run(["a.mp3", "a.mp3"]).Count);
    }
}
