using MapsetVerifier.Checks.Utils;
using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Framework.Objects.Attributes;
using MapsetVerifier.Framework.Objects.Metadata;
using MapsetVerifier.Parser.Objects;
using MapsetVerifier.Parser.Objects.Events;
using MapsetVerifier.RankingCriteria;

namespace MapsetVerifier.Checks.AllModes.General.Storyboard;

[Check]
public class CheckStoryboardConflicts : GeneralCheck
{
    /// <summary> How long commands can overlap, in ms, as times are rounded when storyboards are generated. </summary>
    private const double OverlapTolerance = 2;

    public override CheckMetadata GetMetadata() =>
        new()
        {
            Category = "Storyboard",
            Message = "Conflicting or obsolete storyboard commands.",
            Author = "Greaper",

            Documentation = new Dictionary<string, string>
            {
                {
                    "Purpose",
                    @"
                    Pointing out storyboard commands that cannot work as intended."
                },
                {
                    "Reasoning",
                    @"
                    A command that ends before it starts, or is part of a trigger that can never happen, either has a mistake in it or
                    isn't needed. When commands of the same type overlap, the sprite jumps from one to the other rather than doing both,
                    so one of them isn't fully visible.
                    
                    What triggers do at the same time as other commands is not included, since when that happens depends on the player."
                },
            },
        };

    public override Dictionary<string, IssueTemplate> GetTemplates() =>
        new()
        {
            {
                "Backwards",
                new IssueTemplate(
                    Issue.Level.Warning,
                    "{0} storyboard command(s) end before they start, see {1}.",
                    "amount",
                    "sprites"
                )
                    .WithCause(
                        "A command in the storyboard has an end time that is earlier than its start time."
                    )
                    .WithRule(RC.General.Storyboarding_AvoidIllogicalConflictingObsoleteCommands)
            },
            {
                "Impossible Trigger",
                new IssueTemplate(
                    Issue.Level.Warning,
                    "{0} storyboard trigger(s) can never happen, see {1}.",
                    "amount",
                    "sprites"
                )
                    .WithCause(
                        "A trigger in the storyboard has an unknown name, ends before it starts, or reacts to hit sounds at a time without any objects."
                    )
                    .WithRule(RC.General.Storyboarding_AvoidIllogicalConflictingObsoleteCommands)
            },
            {
                "Overlap",
                new IssueTemplate(
                    Issue.Level.Minor,
                    "{0} storyboard command(s) overlap with another of the same type, see {1}.",
                    "amount",
                    "sprites"
                )
                    .WithCause(
                        "A command in the storyboard starts while another of the same type on the same sprite is still in progress."
                    )
                    .WithRule(RC.General.Storyboarding_AvoidIllogicalConflictingObsoleteCommands)
            },
        };

    public override IEnumerable<Issue> GetIssues(BeatmapSet beatmapSet)
    {
        foreach (var (beatmap, elements) in StoryboardPatterns.GetStoryboards(beatmapSet))
        {
            var backwards = elements
                .SelectMany(element =>
                    StoryboardPatterns
                        .GetBackwardsCommands(element)
                        .Select(command => (Element: element, Time: (double?)command.StartTime))
                )
                .ToList();

            if (backwards.Count > 0)
                yield return new Issue(
                    GetTemplate("Backwards"),
                    beatmap,
                    backwards.Count,
                    StoryboardPatterns.Describe(backwards)
                );

            IReadOnlyCollection<Beatmap> beatmaps =
                beatmap == null ? beatmapSet.Beatmaps : [beatmap];

            var impossible = elements
                .SelectMany(element =>
                    element
                        .triggers.Where(trigger =>
                            StoryboardPatterns.IsImpossibleTrigger(trigger, beatmaps)
                        )
                        .Select(trigger => (Element: element, Time: trigger.startTime))
                )
                .ToList();

            if (impossible.Count > 0)
                yield return new Issue(
                    GetTemplate("Impossible Trigger"),
                    beatmap,
                    impossible.Count,
                    StoryboardPatterns.Describe(impossible)
                );

            var overlaps = elements
                .SelectMany(element =>
                    StoryboardPatterns
                        .GetOverlappingCommands(element, OverlapTolerance)
                        .Select(command => (Element: element, Time: (double?)command.StartTime))
                )
                .ToList();

            if (overlaps.Count > 0)
                yield return new Issue(
                    GetTemplate("Overlap"),
                    beatmap,
                    overlaps.Count,
                    StoryboardPatterns.Describe(overlaps)
                );
        }
    }
}
