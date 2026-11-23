using MapsetVerifier.Checks.Utils;
using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Framework.Objects.Attributes;
using MapsetVerifier.Framework.Objects.Metadata;
using MapsetVerifier.Parser.Objects;
using MapsetVerifier.Parser.Objects.HitObjects;
using MapsetVerifier.Parser.Statics;
using MapsetVerifier.RankingCriteria;

namespace MapsetVerifier.Checks.Standard.Spread;

[Check]
public class CheckStackRhythmSwitching : BeatmapCheck
{
    /// <summary> Stacks further apart than this (about four measures) are no longer read together. </summary>
    private const double MaxBeatsBetweenStacks = 16;

    private const int MinSwitches = 2;

    /// <summary> Same distance within which the game stacks objects. </summary>
    private const float StackDistance = 3;

    /// <summary> 1/1 is twice as long as 1/2, so gaps closer to each other are something else, such as 3/4. </summary>
    private const double MinGapRatio = 1.8;

    public override CheckMetadata GetMetadata() =>
        new BeatmapCheckMetadata
        {
            Modes = [Beatmap.Mode.Standard],
            Difficulties = [Beatmap.Difficulty.Normal],
            Category = "Spread",
            Message = "Switching between 1/1 and 1/2 stacks.",
            Author = "Greaper",

            Documentation = new Dictionary<string, string>
            {
                {
                    "Purpose",
                    @"
                    Pointing out places in Normals that keep switching between 1/1 and 1/2 stacks."
                },
                {
                    "Reasoning",
                    @"
                    Stacks look the same whether their objects are 1/1 or 1/2 apart. Switching back and forth between the two makes
                    newer players guess the rhythm.

                    A place is reported once the stacks switch from one to the other and back again, with at most 16 beats between
                    one stack and the next. The rhythm is measured as if the song were 180 BPM, which is what the ranking criteria
                    are written for: 1/2 at 120 BPM is slow enough to count as 1/1."
                },
            },
        };

    public override Dictionary<string, IssueTemplate> GetTemplates() =>
        new()
        {
            {
                "Switching",
                new IssueTemplate(
                    Issue.Level.Warning,
                    "{0} Stacks switch between 1/1 and 1/2 {1} times.",
                    "timestamp -",
                    "count"
                )
                    .WithCause(
                        "Stacks close to each other switch between 1/1 and 1/2, and then back again."
                    )
                    .WithRule(RC.Osu.Normal_StacksAcceptableButAvoidSwitching)
            },
        };

    public override IEnumerable<Issue> GetIssues(Beatmap beatmap)
    {
        if (beatmap.IsDifficultyGuessed())
            yield break;

        Stack? previous = null;
        var switches = 0;
        var switching = new List<Stack>();

        foreach (var stack in GetStacks(beatmap))
        {
            if (previous is { } last)
            {
                var beats =
                    (stack.First.GetEndTime() - last.Second.time) / RhythmUtils.ReferenceBeatMs;

                if (beats > MaxBeatsBetweenStacks)
                {
                    if (switches >= MinSwitches)
                        yield return Report(beatmap, switching, switches);

                    switching.Clear();
                    switches = 0;
                }
                else if (IsSwitch(last, stack))
                {
                    switching.Add(last);
                    switching.Add(stack);
                    switches++;
                }
            }

            previous = stack;
        }

        if (switches >= MinSwitches)
            yield return Report(beatmap, switching, switches);
    }

    private static bool IsSwitch(Stack previous, Stack next) =>
        previous.IsHalf != next.IsHalf
        && Math.Max(previous.Gap, next.Gap) >= Math.Min(previous.Gap, next.Gap) * MinGapRatio;

    /// <summary> Reports the stacks on either side of each switch. </summary>
    private Issue Report(Beatmap beatmap, List<Stack> switching, int switches) =>
        new(
            GetTemplate("Switching"),
            beatmap,
            Timestamp.Get(
                switching
                    .SelectMany(stack => new HitObject[] { stack.First, stack.Second })
                    .Distinct()
                    .ToArray()
            ),
            switches
        );

    /// <summary> Every two objects following each other in the same place, 1/1 or 1/2 apart. </summary>
    private static IEnumerable<Stack> GetStacks(Beatmap beatmap)
    {
        for (var i = 1; i < beatmap.HitObjects.Count; i++)
        {
            if (
                beatmap.HitObjects[i - 1] is not Stackable first
                || beatmap.HitObjects[i] is not Stackable second
            )
                continue;

            var end = first is Slider slider
                ? slider.UnstackedEndPosition
                : first.UnstackedPosition;

            if ((second.UnstackedPosition - end).Length() >= StackDistance)
                continue;

            var gap = second.time - first.GetEndTime();
            var isHalf = OsuPatterns.IsHalf(gap);

            // Gaps of 2/1 or longer at 180 BPM are neither.
            if (isHalf || (OsuPatterns.IsRest(gap) && !OsuPatterns.IsRest(gap / 2)))
                yield return new Stack(first, second, gap, isHalf);
        }
    }

    private readonly record struct Stack(
        Stackable First,
        Stackable Second,
        double Gap,
        bool IsHalf
    );
}
