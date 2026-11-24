using MapsetVerifier.Checks.AllModes.General.Metadata;
using MapsetVerifier.Framework.Objects;
using Xunit;

namespace MapsetVerifier.Checks.Tests.AllModes.Metadata;

public class CheckInconsistentMetadataTests
{
    private static List<Issue> Run(Func<OsuBuilder, OsuBuilder> change)
    {
        OsuBuilder Build(string version) =>
            new OsuBuilder()
                .Artist("Artist")
                .Title("Title")
                .Creator("Creator")
                .Tags("one two three")
                .Source("Source")
                .Version(version)
                .WithDefaultTiming()
                .WithDefaultHitObject();

        using var context = CheckTestContext.CreateFromOsuFiles([
            ("a.osu", Build("A").Build()),
            ("b.osu", change(Build("B")).Build()),
        ]);

        return context.RunGeneralCheck<CheckInconsistentMetadata>();
    }

    [Fact]
    public void SameMetadata_IsFine()
    {
        Assert.Empty(Run(builder => builder));
    }

    public static TheoryData<string, Func<OsuBuilder, OsuBuilder>> Fields() =>
        new()
        {
            { "artist", builder => builder.Artist("Artist 2") },
            { "title", builder => builder.Title("title") },
            { "unicode title", builder => builder.TitleUnicode("タイトル") },
            { "source", builder => builder.Source("") },
            { "creator", builder => builder.Creator("creator") },
        };

    [Theory]
    [MemberData(nameof(Fields), DisableDiscoveryEnumeration = true)]
    public void Field_IsAProblem(string name, Func<OsuBuilder, OsuBuilder> change)
    {
        var issue = Assert.Single(
            Run(change),
            issue => issue.message.StartsWith($"Inconsistent {name} fields between [B] and [A]")
        );

        Assert.Equal(Issue.Level.Problem, issue.level);
    }

    [Fact]
    public void Tags_NameTheDifference()
    {
        var issue = Assert.Single(Run(builder => builder.Tags("one three four")));

        Assert.Equal(Issue.Level.Problem, issue.level);
        Assert.Contains("between B and A, difference being \"two four\"", issue.message);
    }

    [Fact]
    public void Tags_InAnotherOrder_AreFine()
    {
        Assert.Empty(Run(builder => builder.Tags("three one two")));
    }
}
