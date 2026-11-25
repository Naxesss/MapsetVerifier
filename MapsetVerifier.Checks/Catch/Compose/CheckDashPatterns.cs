using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Framework.Objects.Attributes;
using MapsetVerifier.Framework.Objects.Metadata;
using MapsetVerifier.Parser.Objects;
using MapsetVerifier.Parser.Objects.HitObjects.Catch;
using MapsetVerifier.RankingCriteria;
using static MapsetVerifier.Parser.Objects.Beatmap;

namespace MapsetVerifier.Checks.Catch.Compose;

[Check]
public class CheckDashPatterns : BeatmapCheck
{
    public override CheckMetadata GetMetadata() =>
        new BeatmapCheckMetadata
        {
            Category = "Compose",
            Message = "Dash patterns too hard for the difficulty.",
            Modes = [Mode.Catch],
            Difficulties = [Difficulty.Normal, Difficulty.Hard],
            Author = "Greaper",
            Documentation = new Dictionary<string, string>
            {
                {
                    "Purpose",
                    @"
                    Pointing out dashes in Salads and Platters that break the dash rules of the ranking criteria, and Platter
                    hyperdashes of different snaps after each other.

| | Salad | Platter |
|---|---|---|
| Shortest dash | 125 ms | 62 ms |
| Basic-snapped dashes in a row | 2 | 4 |
| Higher-snapped dashes | must be followed by a walk, and not by antiflow | up to 2 in a row, in one direction |
| Basic-snapped dashes of different snaps in a row | should not be used | |
| Hyperdashes of different snaps in a row | | must not be used |"
                },
                {
                    "Reasoning",
                    @"
                    Players of these difficulties are still learning to dash. Every dash in a row, every change of snap and
                    every change of direction makes the pattern harder to read and to time.

                    Basic-snapped and higher-snapped follow the snapping reference table of the ranking criteria, by the time
                    between the two objects of a dash. A step back only counts as antiflow when the catcher has to move for it,
                    so further than half its width, and not when it follows the path of a slider. Ranked Salads often step back
                    after a higher-snapped dash, so that is only a minor issue."
                },
            },
        };

    public override Dictionary<string, IssueTemplate> GetTemplates() =>
        new()
        {
            {
                "Gap",
                new IssueTemplate(
                    Issue.Level.Problem,
                    "{0} Dash of {1} ms, less than {2} ms.",
                    "timestamp -",
                    "ms",
                    "limit"
                )
                    .WithCause(
                        "The two objects of a dash are too close in time for the difficulty."
                    )
                    .WithRule(
                        RC.Catch.Salad_DashesLeast125MsGap,
                        RC.Catch.Platter_DashesLeast62MsGap
                    )
            },
            {
                "Basic",
                new IssueTemplate(
                    Issue.Level.Problem,
                    "{0} {1} basic-snapped dashes in a row, more than {2}.",
                    "timestamp -",
                    "count",
                    "limit"
                )
                    .WithCause("Too many basic-snapped dashes follow each other.")
                    .WithRule(
                        RC.Catch.Salad_DashesBasicSnappedNotUsed,
                        RC.Catch.Platter_DashesBasicSnappedNotUsed
                    )
            },
            {
                "HigherNotWalk",
                new IssueTemplate(
                    Issue.Level.Problem,
                    "{0} Higher-snapped dash not followed by a walk.",
                    "timestamp -"
                )
                    .WithCause(
                        "A higher-snapped dash in a Salad is followed by another dash or a hyperdash."
                    )
                    .WithRule(RC.Catch.Salad_DashesHigherSnappedAlwaysFollowed)
            },
            {
                "HigherPlatter",
                new IssueTemplate(Issue.Level.Problem, "{0} {1}.", "timestamp -", "reason")
                    .WithCause(
                        "More than two dashes in a row in a Platter include a higher-snapped one, or they change direction."
                    )
                    .WithRule(RC.Catch.Platter_DashesHigherSnappedUsedUp)
            },
            {
                "HigherAntiflow",
                new IssueTemplate(
                    Issue.Level.Minor,
                    "{0} Higher-snapped dash followed by antiflow.",
                    "timestamp -"
                )
                    .WithCause(
                        "The movement after a higher-snapped dash in a Salad goes back the other way."
                    )
                    .WithRule(RC.Catch.Salad_DashesHigherSnappedNotFollowed)
            },
            {
                "BasicSnaps",
                new IssueTemplate(
                    Issue.Level.Warning,
                    "{0} Basic-snapped dashes of different snaps in a row.",
                    "timestamp -"
                )
                    .WithCause(
                        "Basic-snapped dashes of different snaps follow each other in a Salad."
                    )
                    .WithRule(RC.Catch.Salad_DashesBasicSnappedNotUsed2)
            },
            {
                "HyperSnaps",
                new IssueTemplate(
                    Issue.Level.Problem,
                    "{0} Hyperdashes of different snaps in a row.",
                    "timestamp -"
                )
                    .WithCause("Hyperdashes of different snaps follow each other in a Platter.")
                    .WithRule(RC.Catch.Platter_HyperdashesDifferentBeatSnapNot)
            },
        };

    public override IEnumerable<Issue> GetIssues(Beatmap beatmap)
    {
        var objects = beatmap.GetCatchHitObjects(includeJuiceStreamParts: true);

        foreach (var issue in GapIssues(beatmap, objects))
            yield return issue;

        foreach (var chain in Chains(objects, CatchMovementType.Dash))
        {
            foreach (var issue in SaladIssues(beatmap, objects, chain))
                yield return issue;

            foreach (var issue in PlatterIssues(beatmap, objects, chain))
                yield return issue;
        }

        foreach (var chain in Chains(objects, CatchMovementType.Hyperdash))
            if (FirstSnapChange(objects, chain) is { } change)
                yield return new Issue(
                    GetTemplate("HyperSnaps"),
                    beatmap,
                    CatchExtensions.GetTimestamps(change)
                ).ForDifficulties(Difficulty.Hard);
    }

    private IEnumerable<Issue> GapIssues(Beatmap beatmap, List<ICatchHitObject> objects)
    {
        for (var i = 0; i < objects.Count - 1; i++)
        {
            if (objects[i].MovementType != CatchMovementType.Dash)
                continue;

            var ms = objects[i + 1].Time - objects[i].Time;

            foreach (
                var (limit, difficulty) in new[] { (125, Difficulty.Normal), (62, Difficulty.Hard) }
            )
                if (ms < limit)
                    yield return new Issue(
                        GetTemplate("Gap"),
                        beatmap,
                        CatchExtensions.GetTimestamps(objects[i], objects[i + 1]),
                        Math.Round(ms),
                        limit
                    ).ForDifficulties(difficulty);
        }
    }

    private IEnumerable<Issue> SaladIssues(
        Beatmap beatmap,
        List<ICatchHitObject> objects,
        List<int> chain
    )
    {
        var basic = chain.Where(i => !IsHigher(objects, i, Difficulty.Normal)).ToList();

        if (basic.Count > 2)
            yield return Chain("Basic", beatmap, objects, chain, basic.Count, 2)
                .ForDifficulties(Difficulty.Normal);

        if (FirstSnapChange(objects, basic.Count == chain.Count ? chain : []) is { } change)
            yield return new Issue(
                GetTemplate("BasicSnaps"),
                beatmap,
                CatchExtensions.GetTimestamps(change)
            ).ForDifficulties(Difficulty.Normal);

        foreach (var i in chain.Where(i => IsHigher(objects, i, Difficulty.Normal)))
        {
            var next = Get(objects, i + 1);
            var afterNext = Get(objects, i + 2);

            if (next == null || afterNext == null)
                continue;

            if (next.MovementType != CatchMovementType.Walk)
                yield return new Issue(
                    GetTemplate("HigherNotWalk"),
                    beatmap,
                    CatchExtensions.GetTimestamps(objects[i], next, afterNext)
                ).ForDifficulties(Difficulty.Normal);
            else if (
                !InSameSlider(next, afterNext)
                && IsAntiflow(
                    objects[i],
                    next,
                    afterNext,
                    HitObjectDistanceCalculator.GetCatcherHalfWidth(
                        beatmap.DifficultySettings.circleSize
                    )
                )
            )
                yield return new Issue(
                    GetTemplate("HigherAntiflow"),
                    beatmap,
                    CatchExtensions.GetTimestamps(objects[i], next, afterNext)
                ).ForDifficulties(Difficulty.Normal);
        }
    }

    private IEnumerable<Issue> PlatterIssues(
        Beatmap beatmap,
        List<ICatchHitObject> objects,
        List<int> chain
    )
    {
        var anyHigher = chain.Any(i => IsHigher(objects, i, Difficulty.Hard));

        if (!anyHigher)
        {
            if (chain.Count > 4)
                yield return Chain("Basic", beatmap, objects, chain, chain.Count, 4)
                    .ForDifficulties(Difficulty.Hard);

            yield break;
        }

        var turns = chain.Skip(1).Any(i => IsAntiflow(objects[i - 1], objects[i], objects[i + 1]));

        if (chain.Count > 2 || turns)
            yield return Chain(
                    "HigherPlatter",
                    beatmap,
                    objects,
                    chain,
                    chain.Count > 2
                        ? $"{chain.Count} dashes in a row with a higher-snapped one, more than 2"
                        : "Dashes with a higher-snapped one change direction"
                )
                .ForDifficulties(Difficulty.Hard);
    }

    private Issue Chain(
        string template,
        Beatmap beatmap,
        List<ICatchHitObject> objects,
        List<int> chain,
        params object[] arguments
    ) =>
        new(
            GetTemplate(template),
            beatmap,
            [
                CatchExtensions.GetTimestamps(
                    chain.Append(chain[^1] + 1).Select(i => objects[i]).ToArray()
                ),
                .. arguments,
            ]
        );

    /// <summary> Indices of objects starting consecutive movements of the given type. </summary>
    private static IEnumerable<List<int>> Chains(
        List<ICatchHitObject> objects,
        CatchMovementType type
    )
    {
        var chain = new List<int>();

        for (var i = 0; i < objects.Count - 1; i++)
        {
            if (objects[i].MovementType == type)
            {
                chain.Add(i);
                continue;
            }

            if (chain.Count > 0)
                yield return chain;

            chain = [];
        }

        if (chain.Count > 0)
            yield return chain;
    }

    /// <summary> The three objects around the first change of snap in a chain of consecutive ones, if any. </summary>
    private static ICatchHitObject[]? FirstSnapChange(
        List<ICatchHitObject> objects,
        List<int> chain
    )
    {
        for (var j = 1; j < chain.Count; j++)
        {
            var (first, second, third) = (
                objects[chain[j - 1]],
                objects[chain[j]],
                objects[chain[j] + 1]
            );

            if (!CatchExtensions.IsSameSnap(first, second, third))
                return [first, second, third];
        }

        return null;
    }

    private static bool IsHigher(List<ICatchHitObject> objects, int i, Difficulty difficulty) =>
        objects[i].IsHigherSnapped(objects[i + 1], difficulty);

    /// <summary>
    ///     Whether the movement from b to c goes the opposite way of the movement from a to b, by more than
    ///     <paramref name="minBack" /> pixels. Small steps back that the catcher reaches without moving aren't antiflow.
    /// </summary>
    public static bool IsAntiflow(
        ICatchHitObject a,
        ICatchHitObject b,
        ICatchHitObject c,
        double minBack = 0
    )
    {
        // Some broken sliders have parts without a position; there is no direction to compare then.
        if (float.IsNaN(a.Position.X) || float.IsNaN(b.Position.X) || float.IsNaN(c.Position.X))
            return false;

        var first = Math.Sign(b.Position.X - a.Position.X);
        var second = Math.Sign(c.Position.X - b.Position.X);

        return first != 0
            && second != 0
            && first != second
            && Math.Abs(c.Position.X - b.Position.X) > minBack;
    }

    /// <summary> Whether two objects belong to the same slider, so the slider's own path sets the movement. </summary>
    public static bool InSameSlider(ICatchHitObject current, ICatchHitObject next)
    {
        var slider = current as JuiceStream ?? (current as JuiceStream.JuiceStreamPart)?.Parent;
        return slider != null && next is JuiceStream.JuiceStreamPart part && part.Parent == slider;
    }

    private static ICatchHitObject? Get(List<ICatchHitObject> objects, int i) =>
        i < objects.Count ? objects[i] : null;
}
