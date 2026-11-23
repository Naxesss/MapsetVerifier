using MapsetVerifier.Checks.Utils;
using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Framework.Objects.Attributes;
using MapsetVerifier.Framework.Objects.Metadata;
using MapsetVerifier.Parser.Objects;
using MapsetVerifier.Parser.Objects.Events;
using MapsetVerifier.RankingCriteria;

namespace MapsetVerifier.Checks.AllModes.General.Storyboard;

[Check]
public class CheckStoryboardSplitMoves : GeneralCheck
{
    public override CheckMetadata GetMetadata() =>
        new()
        {
            Category = "Storyboard",
            Message = "Storyboard moves split into two commands.",
            Author = "Greaper",

            Documentation = new Dictionary<string, string>
            {
                {
                    "Purpose",
                    @"
                    Pointing out horizontal and vertical moves which can be combined into one command."
                },
                {
                    "Reasoning",
                    @"
                    A `MX` and `MY` command with the same start time, end time and easing do the same as a single `M` command. Using one
                    command instead of two means less overall file size."
                },
            },
        };

    public override Dictionary<string, IssueTemplate> GetTemplates() =>
        new()
        {
            {
                "Split",
                new IssueTemplate(
                    Issue.Level.Minor,
                    "{0} pair(s) of MX and MY commands can be a single M command, see {1}.",
                    "amount",
                    "sprites"
                )
                    .WithCause(
                        "A sprite or animation has a `MX` and a `MY` command with the same start time, end time and easing."
                    )
                    .WithRule(RC.General.Storyboarding_AvoidUsingTwoAxisSpecific)
            },
        };

    public override IEnumerable<Issue> GetIssues(BeatmapSet beatmapSet)
    {
        foreach (var (beatmap, elements) in StoryboardPatterns.GetStoryboards(beatmapSet))
        {
            var places = elements
                .SelectMany(element =>
                    StoryboardPatterns
                        .GetSplitMoves(element)
                        .Select(command => (Element: element, Time: (double?)command.StartTime))
                )
                .ToList();

            if (places.Count > 0)
                yield return new Issue(
                    GetTemplate("Split"),
                    beatmap,
                    places.Count,
                    StoryboardPatterns.Describe(places)
                );
        }
    }
}
