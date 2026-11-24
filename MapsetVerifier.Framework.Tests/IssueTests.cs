using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Framework.Objects.Metadata;
using MapsetVerifier.Parser.Objects;
using Xunit;

namespace MapsetVerifier.Framework.Tests;

public class IssueTests
{
    [Fact]
    public void Template_FillsInArguments()
    {
        var template = new IssueTemplate(Issue.Level.Warning, "{0} {1} px.", "timestamp - ", "n");

        var issue = new Issue(template, null, "00:01:000 (1) - ", 12);

        Assert.Equal("00:01:000 (1) - 12 px.", issue.message);
        Assert.Equal(Issue.Level.Warning, issue.level);
        Assert.Equal("timestamp - n px.", template.ToString());
    }

    [Fact]
    public void Template_KeepsDoubleSpacesWithinArguments()
    {
        var template = new IssueTemplate(Issue.Level.Problem, "\"{0}\" is missing.", "file");

        Assert.Equal("\"a  b.png\" is missing.", template.Format(["a  b.png"]));
    }

    [Fact]
    public void Template_MissingArgument_IsEmpty()
    {
        Assert.Equal("[]", new IssueTemplate(Issue.Level.Info, "[{0}]", "x").Format([null]));
    }

    [Fact]
    public void Template_WithMoreOrFewerPlacesThanArguments_IsRejected()
    {
        Assert.Throws<ArgumentException>(() =>
            new IssueTemplate(Issue.Level.Info, "No places.", "unused")
        );
        Assert.Throws<ArgumentException>(() =>
            new IssueTemplate(Issue.Level.Info, "{0} and {1}", "only one")
        );
    }

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    public void Format_WithAnotherAmountOfArguments_IsRejected(int count)
    {
        var template = new IssueTemplate(Issue.Level.Info, "{0}", "x");

        Assert.Throws<ArgumentException>(() => template.Format(new object?[count]));
    }

    [Fact]
    public void Template_KeepsCauseAndDistinctRules()
    {
        var template = new IssueTemplate(Issue.Level.Info, "Text.")
            .WithCause("Because.")
            .WithRule("a", "b")
            .WithRule("a");

        Assert.Equal("Because.", template.Cause);
        Assert.Equal(["a", "b"], template.RuleIds);
        Assert.Equal("Text.", template.FormatString);
        Assert.Empty(template.GetDefaultArguments());
    }

    [Fact]
    public void Interpretation_TakesNumbersAndLists_PerName()
    {
        var issue = NewIssue().WithInterpretation("difficulty", 0, new[] { 1, 2 }, "other", 3);

        Assert.Equal(
            [new("difficulty", 0), new("difficulty", 1), new("difficulty", 2), new("other", 3)],
            issue.InterpretationPairs
        );
    }

    [Fact]
    public void Issue_WithoutLimits_AppliesToEveryDifficulty()
    {
        Assert.All(
            Enum.GetValues<Beatmap.Difficulty>(),
            difficulty => Assert.True(NewIssue().AppliesToDifficulty(difficulty))
        );
    }

    [Fact]
    public void Issue_ForDifficulties_OnlyAppliesToThose()
    {
        var issue = NewIssue().ForDifficulties(Beatmap.Difficulty.Easy, Beatmap.Difficulty.Normal);

        Assert.True(issue.AppliesToDifficulty(Beatmap.Difficulty.Easy));
        Assert.True(issue.AppliesToDifficulty(Beatmap.Difficulty.Normal));
        Assert.False(issue.AppliesToDifficulty(Beatmap.Difficulty.Hard));
    }

    [Fact]
    public void Issue_OfACheckForCertainDifficulties_OnlyAppliesToThose()
    {
        var issue = NewIssue()
            .ForDifficulties(Beatmap.Difficulty.Easy, Beatmap.Difficulty.Hard)
            .WithOrigin(new HardCheck());

        Assert.True(issue.AppliesToDifficulty(Beatmap.Difficulty.Hard));
        // Neither limit is enough on its own.
        Assert.False(issue.AppliesToDifficulty(Beatmap.Difficulty.Easy));
        Assert.False(issue.AppliesToDifficulty(Beatmap.Difficulty.Insane));
    }

    [Fact]
    public void Metadata_NamesItsModes()
    {
        Assert.Equal("General", new CheckMetadata().GetMode());
        Assert.Equal("All Modes", new BeatmapCheckMetadata().GetMode());
        Assert.Equal("No Modes", new BeatmapCheckMetadata { Modes = [] }.GetMode());
        Assert.Equal(
            "Taiko Mania",
            new BeatmapCheckMetadata { Modes = [Beatmap.Mode.Taiko, Beatmap.Mode.Mania] }.GetMode()
        );
    }

    private static Issue NewIssue() => new(new IssueTemplate(Issue.Level.Problem, "Text."), null);

    private sealed class HardCheck : BeatmapCheck
    {
        public override Dictionary<string, IssueTemplate> GetTemplates() => [];

        public override CheckMetadata GetMetadata() =>
            new BeatmapCheckMetadata
            {
                Difficulties = [Beatmap.Difficulty.Hard, Beatmap.Difficulty.Insane],
            };

        public override IEnumerable<Issue> GetIssues(Beatmap beatmap) => [];
    }

    [Fact]
    public void Interpretation_OnlyOfDifficulties_Counts()
    {
        var issue = new Issue(
            new IssueTemplate(Issue.Level.Info, "Text."),
            null
        ).WithInterpretation(
            "other",
            (int)Beatmap.Difficulty.Easy,
            1.5,
            "difficulty",
            (int)Beatmap.Difficulty.Hard
        );

        Assert.False(issue.AppliesToDifficulty(Beatmap.Difficulty.Easy));
        Assert.True(issue.AppliesToDifficulty(Beatmap.Difficulty.Hard));
        Assert.Equal(2, issue.InterpretationPairs.Count);
    }

    private sealed class Nameless
    {
        public override string? ToString() => null;
    }

    [Fact]
    public void Template_ArgumentWithoutText_IsEmpty()
    {
        Assert.Equal(
            "[]",
            new IssueTemplate(Issue.Level.Info, "[{0}]", "x").Format([new Nameless()])
        );
    }
}
