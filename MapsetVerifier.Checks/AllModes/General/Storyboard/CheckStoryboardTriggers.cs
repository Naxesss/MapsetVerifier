using MapsetVerifier.Checks.Utils;
using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Framework.Objects.Attributes;
using MapsetVerifier.Framework.Objects.Metadata;
using MapsetVerifier.Parser.Objects;
using MapsetVerifier.Parser.Objects.Events;
using MapsetVerifier.RankingCriteria;

namespace MapsetVerifier.Checks.AllModes.General.Storyboard;

[Check]
public class CheckStoryboardTriggers : GeneralCheck
{
    public override CheckMetadata GetMetadata() =>
        new()
        {
            Category = "Storyboard",
            Message = "Triggered sprites that are not faded out.",
            Author = "Greaper",

            Documentation = new Dictionary<string, string>
            {
                {
                    "Purpose",
                    @"
                    Ensuring that sprites which become active from triggers do not stay around after use."
                },
                {
                    "Reasoning",
                    @"
                    Triggers will activate from their first possible command and stay active until the end of the difficulty. Unless the
                    sprite is faded out, it keeps taking up performance and remains visible."
                },
            },
        };

    public override Dictionary<string, IssueTemplate> GetTemplates() =>
        new()
        {
            {
                "Stays",
                new IssueTemplate(
                    Issue.Level.Warning,
                    "{0} not faded out after being activated by a trigger, see {1}.",
                    "sprites",
                    "sprites"
                )
                    .WithCause(
                        "A sprite or animation has a trigger, but neither its triggers nor its other commands end with it being invisible."
                    )
                    .WithRule(RC.General.Storyboarding_FadeOutSpritesActivatedTriggers)
            },
        };

    public override IEnumerable<Issue> GetIssues(BeatmapSet beatmapSet)
    {
        foreach (var (beatmap, elements) in StoryboardPatterns.GetStoryboards(beatmapSet))
        {
            var places = elements
                .Where(StoryboardPatterns.StaysVisibleAfterTrigger)
                .Select(element => (Element: element, Time: element.triggers[0].startTime))
                .ToList();

            if (places.Count > 0)
                yield return new Issue(
                    GetTemplate("Stays"),
                    beatmap,
                    places.Count == 1 ? "1 sprite is" : $"{places.Count} sprites are",
                    StoryboardPatterns.Describe(places)
                );
        }
    }
}
