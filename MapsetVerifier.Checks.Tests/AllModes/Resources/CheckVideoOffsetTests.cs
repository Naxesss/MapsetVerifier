using MapsetVerifier.Checks.AllModes.General.Resources;
using MapsetVerifier.Framework.Objects;
using Xunit;

namespace MapsetVerifier.Checks.Tests.AllModes.Resources;

public class CheckVideoOffsetTests
{
    [Theory]
    [InlineData(new[] { 100, 100 }, 0)]
    [InlineData(new[] { 100, 101 }, 2)]
    [InlineData(new int[0], 0)]
    public void DifferentOffsets_AreAProblem(int[] offsets, int expected)
    {
        using var context = CheckTestContext.CreateFromOsuFiles(
            offsets
                .Select(offset => $"Video,{offset},\"a.mp4\"")
                // Difficulties without a video don't count.
                .Append("//")
                .Select(
                    (line, i) =>
                        (
                            $"{i}.osu",
                            new OsuBuilder()
                                .Version($"Diff {i}")
                                .Events(line)
                                .WithDefaultTiming()
                                .WithDefaultHitObject()
                                .Build()
                        )
                )
        );

        var issues = context.RunGeneralCheck<CheckVideoOffset>();

        Assert.Equal(expected, issues.Count);
        Assert.All(issues, issue => Assert.Equal(Issue.Level.Problem, issue.level));
    }
}
