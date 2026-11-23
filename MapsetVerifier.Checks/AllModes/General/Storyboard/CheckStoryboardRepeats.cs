using MapsetVerifier.Checks.Utils;
using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Framework.Objects.Attributes;
using MapsetVerifier.Framework.Objects.Metadata;
using MapsetVerifier.Parser.Objects;
using MapsetVerifier.Parser.Objects.Events;
using MapsetVerifier.RankingCriteria;

namespace MapsetVerifier.Checks.AllModes.General.Storyboard;

[Check]
public class CheckStoryboardRepeats : GeneralCheck
{
    private const int MinRepeats = 8;

    public override CheckMetadata GetMetadata() =>
        new()
        {
            Category = "Storyboard",
            Message = "Repeated storyboard commands without a loop.",
            Author = "Greaper",

            Documentation = new Dictionary<string, string>
            {
                {
                    "Purpose",
                    @"
                    Pointing out storyboard commands which repeat themselves many times and can be a loop instead."
                },
                {
                    "Reasoning",
                    @"
                    Using the loop command will often reduce the amount of lines considerably, which in turn reduces the file size.
                    
                    Only commands that repeat with the exact same time in between are included, since a loop would slowly go out of sync
                    with the music otherwise."
                },
            },
        };

    public override Dictionary<string, IssueTemplate> GetTemplates() =>
        new()
        {
            {
                "Repeat",
                new IssueTemplate(
                    Issue.Level.Minor,
                    "{0} storyboard command(s) can be left out by using loops, see {1}.",
                    "amount",
                    "sprites"
                )
                    .WithCause(
                        "One or more commands of the same type repeat themselves at least 8 times in a row with the same time in between."
                    )
                    .WithRule(RC.General.Storyboarding_UseLoopsCommandsRepeatThemselves)
            },
        };

    public override IEnumerable<Issue> GetIssues(BeatmapSet beatmapSet)
    {
        foreach (var (beatmap, elements) in StoryboardPatterns.GetStoryboards(beatmapSet))
        {
            var repeats = elements
                .SelectMany(element =>
                    StoryboardPatterns
                        .GetRepeatedCommands(element, MinRepeats)
                        .Select(repeat => (Element: element, Repeat: repeat))
                )
                .ToList();

            if (repeats.Count > 0)
                yield return new Issue(
                    GetTemplate("Repeat"),
                    beatmap,
                    repeats.Sum(place => place.Repeat.Saved),
                    StoryboardPatterns.Describe(
                        repeats
                            .Select(place =>
                                (place.Element, (double?)place.Repeat.Command.StartTime)
                            )
                            .ToList()
                    )
                );
        }
    }
}
