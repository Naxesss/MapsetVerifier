using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Framework.Objects.Attributes;
using MapsetVerifier.Framework.Objects.Metadata;
using MapsetVerifier.Parser.Objects;
using MapsetVerifier.Parser.Objects.HitObjects;
using MapsetVerifier.Parser.Objects.HitObjects.Catch;
using MapsetVerifier.Parser.Statics;
using MapsetVerifier.RankingCriteria;
using static MapsetVerifier.Parser.Objects.Beatmap;

namespace MapsetVerifier.Checks.Catch.Compose;

[Check]
public class CheckSpinnerTrap : BeatmapCheck
{
    /// <summary> The first object after a spinner is at the outer edge when closer to the border than this. </summary>
    private const int EdgeDistance = 48;

    private const int PlayfieldWidth = 512;

    /// <summary> Pixels per millisecond the catcher moves while walking and dashing. </summary>
    private const double WalkSpeed = 0.5;

    private const double DashSpeed = 1;

    /// <summary> Marks positions the catcher can't be at. </summary>
    private const int Unreachable = -1_000_000;

    private static readonly Difficulty[] Levels =
    [
        Difficulty.Normal,
        Difficulty.Hard,
        Difficulty.Insane,
    ];

    public override CheckMetadata GetMetadata() =>
        new BeatmapCheckMetadata
        {
            Category = "Compose",
            Message = "Spinner traps.",
            Modes = [Mode.Catch],
            Difficulties = Levels,
            Author = "Greaper",
            Documentation = new Dictionary<string, string>
            {
                {
                    "Purpose",
                    @"
                    Pointing out objects directly after a spinner that are hard to get to for a player catching bananas:

- Objects that can't be reached in time, even by dashing, after catching as many bananas as possible.
- Objects closer than 48 pixels to the left or right border, without enough time to walk there from the other side.

                    Difficulties whose name doesn't say their level are left out unless a level is chosen."
                },
                {
                    "Reasoning",
                    @"
                    Bananas are optional, but players go after them. If doing that well makes them miss the next fruit, the map
                    punishes them for something they can't know on their first try.

                    The bananas fall in the same places every time, so the best way to catch them can be worked out. If there
                    are several, one of them leading to the next object is enough. With mods like Hard Rock the bananas can
                    fall elsewhere, which isn't considered.

                    Bananas covering the next object is the third kind of spinner trap. That happens when the gap after the
                    spinner is too short, which has a check of its own."
                },
            },
        };

    public override Dictionary<string, IssueTemplate> GetTemplates() =>
        new()
        {
            {
                "Unreachable",
                WithRules(
                    new IssueTemplate(
                        Issue.Level.Warning,
                        "{0} Spinner trap, {1} is {2} px out of reach.",
                        "timestamp -",
                        "object",
                        "distance"
                    ).WithCause(
                        "The object after a spinner can't be reached by dashing from anywhere the catcher can end up after catching as many bananas as possible."
                    )
                )
            },
            {
                "Edge",
                WithRules(
                    new IssueTemplate(
                        Issue.Level.Warning,
                        "{0} Spinner trap, {1} {2} px from the border after {3} ms.",
                        "timestamp -",
                        "object",
                        "distance",
                        "gap"
                    ).WithCause(
                        "The object after a spinner is closer than 48 pixels to the border, without enough time to walk there from the other side."
                    )
                )
            },
        };

    private static IssueTemplate WithRules(IssueTemplate template) =>
        template.WithRule(
            RC.Catch.Salad_SpinnerTrapsNotUsed,
            RC.Catch.Platter_SpinnerTrapsNotUsed,
            RC.Catch.Rain_SpinnerTrapsNotUsed
        );

    public override IEnumerable<Issue> GetIssues(Beatmap beatmap)
    {
        // The guideline is for specific levels, and a level guessed from star rating isn't reliable enough.
        if (beatmap.IsDifficultyGuessed())
            yield break;

        var halfWidth = HitObjectDistanceCalculator.GetCatcherHalfWidth(
            beatmap.DifficultySettings.circleSize
        );
        Dictionary<double, List<BananaShowers.FallingBanana>>? showers = null;

        foreach (var spinner in beatmap.HitObjects.OfType<Bananas>())
        {
            var following = spinner.Next();

            if (following is not ICatchHitObject next || following is Bananas)
                continue;

            var gap = next.Time - spinner.endTime;
            var farthest = Math.Max(next.Position.X, PlayfieldWidth - next.Position.X);

            // Objects during the spinner have other problems, and with time to walk there from anywhere it's no trap.
            if (gap < 0 || !float.IsFinite(farthest) || farthest - halfWidth <= gap * WalkSpeed)
                continue;

            var edgeDistance = PlayfieldWidth - farthest;

            if (edgeDistance < EdgeDistance)
                yield return new Issue(
                    GetTemplate("Edge"),
                    beatmap,
                    Timestamp.Get(spinner, following),
                    next.GetNoteTypeName().ToLower(),
                    Math.Round(Math.Max(0, edgeDistance)),
                    Math.Round(gap)
                ).ForDifficulties(Levels);

            if (farthest - halfWidth <= gap * DashSpeed)
                continue;

            showers ??= BananaShowers.GetBananas(beatmap);

            var bananas =
                showers
                    .Where(shower => Math.Abs(shower.Key - spinner.time) < 1)
                    .Select(shower => shower.Value)
                    .FirstOrDefault()
                ?? [];

            if (bananas.Count == 0)
                continue;

            var tooFar = GetTooFar(spinner, next, bananas, halfWidth);

            if (tooFar > 0)
                yield return new Issue(
                    GetTemplate("Unreachable"),
                    beatmap,
                    Timestamp.Get(spinner, following),
                    next.GetNoteTypeName().ToLower(),
                    Math.Ceiling(tooFar)
                ).ForDifficulties(Levels);
        }
    }

    /// <summary>
    ///     How many pixels the next object is out of reach from the closest position the catcher can end up at
    ///     after catching as many bananas as possible. Zero or less if it can be reached.
    /// </summary>
    private static double GetTooFar(
        Bananas spinner,
        ICatchHitObject next,
        List<BananaShowers.FallingBanana> bananas,
        float halfWidth
    )
    {
        // The most bananas that can have been caught with the catcher at each position, as time goes on.
        var caught = new int[PlayfieldWidth + 1];
        var time = spinner.time;

        // The catcher comes from the previous object, if any.
        if (spinner.Prev() is { } prev and not Spinner)
        {
            var end = (prev as JuiceStream)?.Parts.LastOrDefault()?.Position ?? prev.Position;
            var reach = halfWidth + Math.Max(0, time - prev.GetEndTime()) * DashSpeed;

            for (var x = 0; x < caught.Length; x++)
                if (Math.Abs(x - end.X) > reach)
                    caught[x] = Unreachable;
        }

        foreach (var banana in bananas)
        {
            caught = Move(caught, (banana.Time - time) * DashSpeed);
            time = banana.Time;

            for (var x = 0; x < caught.Length; x++)
                if (Math.Abs(x - banana.X) <= halfWidth)
                    caught[x]++;
        }

        var most = caught.Max();
        var distance = Enumerable
            .Range(0, caught.Length)
            .Where(x => caught[x] == most)
            .Min(x => Math.Abs(x - next.Position.X));

        return distance - halfWidth - (next.Time - time) * DashSpeed;
    }

    /// <summary> The highest value within the given distance of each position. </summary>
    private static int[] Move(int[] values, double distance)
    {
        var steps = (int)Math.Clamp(distance, 0, PlayfieldWidth);
        var result = new int[values.Length];

        for (var x = 0; x < values.Length; x++)
        {
            var low = Math.Max(0, x - steps);
            var high = Math.Min(values.Length - 1, x + steps);
            var highest = values[low];

            for (var other = low + 1; other <= high; other++)
                highest = Math.Max(highest, values[other]);

            result[x] = highest;
        }

        return result;
    }
}
