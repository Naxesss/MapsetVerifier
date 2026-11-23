using MapsetVerifier.Checks.Utils;
using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Framework.Objects.Attributes;
using MapsetVerifier.Framework.Objects.Metadata;
using MapsetVerifier.Parser.Objects;
using MapsetVerifier.Parser.Objects.Events;
using MapsetVerifier.RankingCriteria;

namespace MapsetVerifier.Checks.AllModes.General.Storyboard;

[Check]
public class CheckStoryboardDenseCommands : GeneralCheck
{
    /// <summary> How many commands in a row need to be too close to the previous one to count as many. </summary>
    private const int MinCommands = 5;

    private const double MinInterval = 16;

    public override CheckMetadata GetMetadata() =>
        new()
        {
            Category = "Storyboard",
            Message = "Storyboard commands less than 16 ms apart.",
            Author = "Greaper",

            Documentation = new Dictionary<string, string>
            {
                {
                    "Purpose",
                    @"
                    Pointing out sprites with more storyboard commands than needed to look smooth."
                },
                {
                    "Reasoning",
                    @"
                    60 commands per second is often more than enough for any sprite to make smooth transitions. Having more than that
                    only adds to the file size and loading time."
                },
            },
        };

    public override Dictionary<string, IssueTemplate> GetTemplates() =>
        new()
        {
            {
                "Dense",
                new IssueTemplate(
                    Issue.Level.Minor,
                    "{0} with many commands of the same type less than 16 ms apart, see {1}.",
                    "sprites",
                    "sprites"
                )
                    .WithCause(
                        "A sprite or animation has at least 5 commands in a row that each start less than 16 ms after the previous one of the same type."
                    )
                    .WithRule(RC.General.Storyboarding_UsingManyCommandsSameType)
            },
        };

    public override IEnumerable<Issue> GetIssues(BeatmapSet beatmapSet)
    {
        foreach (var (beatmap, elements) in StoryboardPatterns.GetStoryboards(beatmapSet))
        {
            var places = elements
                .Select(element =>
                    (
                        Element: element,
                        Runs: StoryboardPatterns
                            .GetDenseRuns(element, MinInterval, MinCommands)
                            .ToList()
                    )
                )
                .Where(place => place.Runs.Count > 0)
                .Select(place => (place.Element, Time: (double?)place.Runs[0].StartTime))
                .ToList();

            if (places.Count > 0)
                yield return new Issue(
                    GetTemplate("Dense"),
                    beatmap,
                    places.Count == 1 ? "1 sprite" : $"{places.Count} sprites",
                    StoryboardPatterns.Describe(places)
                );
        }
    }
}
