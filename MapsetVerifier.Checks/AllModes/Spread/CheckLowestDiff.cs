using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Framework.Objects.Attributes;
using MapsetVerifier.Framework.Objects.Metadata;
using MapsetVerifier.Parser.Objects;
using MapsetVerifier.Parser.Statics;

namespace MapsetVerifier.Checks.AllModes.Spread
{
    [Check]
    public class CheckLowestDiff : BeatmapSetCheck
    {
        private const double BreakTimeLeniency = 30 * 1000;

        private static readonly double[] DefaultThresholds =
        [
            CreateThreshold(2, 30),
            CreateThreshold(3, 15),
            CreateThreshold(4, 0),
        ];

        private static readonly double[] ManiaThresholds =
        [
            CreateThreshold(2, 0),
            CreateThreshold(2, 45),
            CreateThreshold(3, 30),
        ];

        // Lowest difficulty being Hard is disallowed below the first threshold,
        // Insane below the second, and Expert or higher below the third.
        private static readonly Beatmap.Difficulty[][] DisallowedDifficulties =
        [
            [Beatmap.Difficulty.Hard],
            [Beatmap.Difficulty.Insane],
            [Beatmap.Difficulty.Expert, Beatmap.Difficulty.Ultra],
        ];

        public override CheckMetadata GetMetadata() =>
            new BeatmapCheckMetadata
            {
                Category = "Spread",
                Message = "Lowest difficulty too difficult for the given drain time(s).",
                Author = "Greaper",

                Documentation = new Dictionary<string, string>
                {
                    {
                        "Purpose",
                        @"
                        Ensuring that newer players still have new content to play at the same time as encouraging mappers to map longer songs.

                        ![](assets/checks/all-modes-spread-lowest-diff-1.png ""The drain time thresholds determining the highest difficulty level for the lowest difficulty in the set."")

                        The thresholds are as follows:
                        - osu!, osu!taiko, and osu!catch: below 2:30 at most Normal, below 3:15 at most Hard, below 4:00 at most Insane.
                        - osu!mania: below 2:00 at most Normal, below 2:45 at most Hard, below 3:30 at most Insane, for each key mode separately.

                        In osu!taiko and osu!catch, break times may be combined with drain time, limited to at most 30 seconds for the highest difficulty. This does not apply to difficulties with less than 30 seconds of drain time.

                        In osu!catch and osu!mania, a proper spread of at least 4, 3, or 2 difficulties (for each respective threshold) can be provided instead. Since whether a spread is proper cannot be determined automatically, this is left as a warning to verify manually."
                    },
                    {
                        "Reasoning",
                        @"
                        Newer players usually struggle with especially long songs, so encouraging them to try shorter songs first at lower difficulty levels allows them to learn the basics before trying to train their stamina or similar. This is done by requiring that shorter songs have lower difficulties, while longer songs can have less of them. This also reduces the workload on mappers and as such introduces a larger variety of songs into the game that otherwise wouldn't be so common due to their length."
                    },
                },
            };

        public override Dictionary<string, IssueTemplate> GetTemplates() =>
            new()
            {
                {
                    "Problem",
                    new IssueTemplate(
                        Issue.Level.Problem,
                        "With a lowest difficulty {0}, the {1} time of {2} must be at least {3}, currently {4}.",
                        "lowest diff",
                        "drain/drain + break",
                        "beatmap",
                        "lowest drain",
                        "current drain"
                    ).WithCause(
                        "The lowest difficulty of a beatmapset (or key mode, for osu!mania) is too high of a difficulty level considering the drain time of the difficulties, and there are not enough difficulties to rely on a proper spread instead."
                    )
                },
                {
                    "Spread",
                    new IssueTemplate(
                        Issue.Level.Warning,
                        "With a lowest difficulty {0}, the {1} time of {2} must be at least {3}, currently {4}. Ensure the {5} difficulties form a proper spread.",
                        "lowest diff",
                        "drain/drain + break",
                        "beatmap",
                        "lowest drain",
                        "current drain",
                        "difficulty count"
                    ).WithCause(
                        "Same as the problem, except there are enough difficulties that a proper spread (osu!catch and osu!mania only) could satisfy the requirement instead."
                    )
                },
            };

        public override IEnumerable<Issue> GetIssues(BeatmapSet beatmapSet)
        {
            // Beatmaps are sorted by mode and interpreted difficulty, so the first of each group is the lowest.
            // Hybrids need to apply to each mode's rules separately, and mania to each key mode separately.
            var groups = beatmapSet.Beatmaps.GroupBy(beatmap =>
                (
                    beatmap.GeneralSettings.mode,
                    keys: beatmap.GeneralSettings.mode == Beatmap.Mode.Mania
                        ? beatmap.DifficultySettings.circleSize
                        : 0
                )
            );

            foreach (var group in groups)
            {
                var mode = group.Key.mode;
                var groupBeatmaps = group.ToList();
                var lowestBeatmap = groupBeatmaps.First();
                var highestBeatmap = groupBeatmaps.Last();

                var thresholds = mode switch
                {
                    Beatmap.Mode.Standard or Beatmap.Mode.Taiko or Beatmap.Mode.Catch =>
                        DefaultThresholds,
                    Beatmap.Mode.Mania => ManiaThresholds,
                    _ => throw new ArgumentOutOfRangeException(nameof(mode)),
                };

                var allowsBreakTime = mode is Beatmap.Mode.Taiko or Beatmap.Mode.Catch;
                var allowsSpreadAlternative = mode is Beatmap.Mode.Catch or Beatmap.Mode.Mania;

                foreach (var beatmap in groupBeatmaps)
                {
                    var drainTime = beatmap.GetDrainTime(mode);
                    var effectiveTime = drainTime;
                    var usesBreakTime = allowsBreakTime && drainTime >= BreakTimeLeniency;

                    if (usesBreakTime)
                    {
                        var breakTime = beatmap.GetPlayTime() - drainTime;
                        if (beatmap == highestBeatmap)
                            breakTime = Math.Min(breakTime, BreakTimeLeniency);

                        effectiveTime += breakTime;
                    }

                    // Shorter drain time bands require larger spreads: 4 difficulties below the first
                    // threshold, 3 below the second, and 2 below the third.
                    var bandIndex = Array.FindIndex(
                        thresholds,
                        threshold => effectiveTime < threshold
                    );
                    if (bandIndex == -1)
                        continue;

                    var requiredSpreadCount = 4 - bandIndex;
                    var canRelyOnSpread =
                        allowsSpreadAlternative && groupBeatmaps.Count >= requiredSpreadCount;

                    for (var i = bandIndex; i < thresholds.Length; ++i)
                    {
                        var disallowedDifficulties = DisallowedDifficulties[i];

                        object[] arguments =
                        [
                            lowestBeatmap.GetModeDifficultyName(disallowedDifficulties[0]),
                            usesBreakTime ? "drain + break" : "drain",
                            beatmap,
                            Timestamp.Get(thresholds[i]),
                            Timestamp.Get(effectiveTime),
                        ];

                        var issue = canRelyOnSpread
                            ? new Issue(
                                GetTemplate("Spread"),
                                lowestBeatmap,
                                [.. arguments, groupBeatmaps.Count]
                            )
                            : new Issue(GetTemplate("Problem"), lowestBeatmap, arguments);

                        yield return issue.ForDifficulties(disallowedDifficulties);
                    }
                }
            }
        }

        private static int CreateThreshold(int minutes, int seconds)
        {
            // Thresholds need to be in milliseconds
            return (minutes * 60 + seconds) * 1000;
        }
    }
}
