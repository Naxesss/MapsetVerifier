using System.Globalization;
using MapsetVerifier.Checks.Utils;
using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Framework.Objects.Attributes;
using MapsetVerifier.Framework.Objects.Metadata;
using MapsetVerifier.Parser.Objects;
using MapsetVerifier.RankingCriteria;

namespace MapsetVerifier.Checks.AllModes.Compose;

[Check]
public class CheckNoteDensity : BeatmapCheck
{
    /// <summary>
    ///     For each difficulty the ranking criteria give a main snapping for, the snap a gap has to be
    ///     faster than to count against it, what the guideline allows, and the share of such gaps above
    ///     which the difficulty no longer reads as "mostly" that snapping.
    ///     <para>
    ///         The shares were picked just above what ranked difficulties of each level reach, measured with
    ///         scripts/rhythm-density-report.cs: ranked maps are far denser than "mostly" suggests (the median
    ///         Futsuu has 42% of its gaps faster than 1/1), so only clear outliers are reported.
    ///     </para>
    /// </summary>
    private static readonly Dictionary<(Beatmap.Mode, Beatmap.Difficulty), Limit> Limits = new()
    {
        [(Beatmap.Mode.Standard, Beatmap.Difficulty.Easy)] = new(
            1,
            0.20,
            "1/1, 2/1, or slower rhythms"
        ),
        [(Beatmap.Mode.Standard, Beatmap.Difficulty.Normal)] = new(
            1,
            0.50,
            "1/1, occasional 1/2, or slower rhythms"
        ),
        [(Beatmap.Mode.Standard, Beatmap.Difficulty.Hard)] = new(
            2,
            0.40,
            "1/2, occasional 1/4, or slower rhythms"
        ),
        [(Beatmap.Mode.Taiko, Beatmap.Difficulty.Easy)] = new(
            0.5,
            0.90,
            "2/1, 4/1, or slower rhythms"
        ),
        [(Beatmap.Mode.Taiko, Beatmap.Difficulty.Normal)] = new(
            1,
            0.70,
            "1/1, 2/1, or slower rhythms"
        ),
        [(Beatmap.Mode.Taiko, Beatmap.Difficulty.Hard)] = new(
            2,
            0.60,
            "1/2, 1/1, or slower rhythms"
        ),
        [(Beatmap.Mode.Taiko, Beatmap.Difficulty.Insane)] = new(
            2,
            0.80,
            "1/2 and occasional 1/1 rhythms"
        ),
        [(Beatmap.Mode.Taiko, Beatmap.Difficulty.Expert)] = new(4, 0.70, "1/2 and 1/4 rhythms"),
        [(Beatmap.Mode.Catch, Beatmap.Difficulty.Easy)] = new(1, 0.60, "a 1/1 pattern"),
        [(Beatmap.Mode.Catch, Beatmap.Difficulty.Normal)] = new(2, 0.35, "a 1/1 and 1/2 pattern"),
        [(Beatmap.Mode.Catch, Beatmap.Difficulty.Hard)] = new(3, 0.40, "a 1/2 and/or 1/3 pattern"),
        [(Beatmap.Mode.Catch, Beatmap.Difficulty.Insane)] = new(
            6,
            0.20,
            "a 1/2 + 1/4 and/or 1/3 + 1/6 pattern"
        ),
    };

    /// <summary> Fewer gaps than this is too little rhythm to judge, like a very short or mostly empty map. </summary>
    private const int MinGaps = 20;

    public override CheckMetadata GetMetadata() =>
        new BeatmapCheckMetadata
        {
            Modes = [Beatmap.Mode.Standard, Beatmap.Mode.Taiko, Beatmap.Mode.Catch],
            Category = "Compose",
            Message = "Too dense rhythm for the difficulty.",
            Author = "Greaper",

            Documentation = new Dictionary<string, string>
            {
                {
                    "Purpose",
                    @"
                    Pointing out lower difficulties whose rhythm is a lot denser than the ranking criteria describe for
                    their level, such as a Normal made up mostly of 1/2."
                },
                {
                    "Reasoning",
                    @"
                    Each difficulty level has a main snapping that players of that level can keep up with. The rhythm
                    is measured as if the song were 180 BPM, which is what the ranking criteria are written for: a 1/2 at
                    120 BPM is as long as a 1/3 at 180 BPM, so slower songs can use denser snaps and faster songs should
                    use fewer of them. A snap only counts as faster once it is shorter than it would be at 240 BPM, the
                    point where the Scaling BPM page halves the rhythm.

                    Ranked difficulties are often denser than ""mostly"" suggests, so this only points out difficulties
                    well beyond what ranked maps of the same level do. Short dense sections are left to the other
                    checks and to your own judgement.

                    Difficulties whose name doesn't say their level, such as hitsound difficulties, are skipped unless
                    you pick their level yourself.

| Difficulty | Counts as dense | Reported above |
|---|---|---|
| osu! Easy | faster than 1/1 | 20% |
| osu! Normal | faster than 1/1 | 50% |
| osu! Hard | faster than 1/2 | 40% |
| osu!taiko Kantan | faster than 2/1 | 90% |
| osu!taiko Futsuu | faster than 1/1 | 70% |
| osu!taiko Muzukashii | faster than 1/2 | 60% |
| osu!taiko Oni | faster than 1/2 | 80% |
| osu!taiko Inner Oni | faster than 1/4 | 70% |
| osu!catch Cup | faster than 1/1 | 60% |
| osu!catch Salad | faster than 1/2 | 35% |
| osu!catch Platter | faster than 1/3 | 40% |
| osu!catch Rain | faster than 1/6 | 20% |"
                },
            },
        };

    public override Dictionary<string, IssueTemplate> GetTemplates() =>
        new()
        {
            {
                "Standard",
                Template()
                    .WithRule(
                        RC.Osu.Easy_NoteDensityConsistMostly1,
                        RC.Osu.Normal_NoteDensityConsistMostly1,
                        RC.Osu.Hard_NoteDensityConsistMostly1
                    )
            },
            {
                "Taiko",
                Template()
                    .WithRule(
                        RC.Taiko.Kantan_MainSnappingConsistMostly2,
                        RC.Taiko.Futsuu_MainSnappingConsistMostly1,
                        RC.Taiko.Muzukashii_MainSnappingConsistMostly1,
                        RC.Taiko.Oni_MainSnappingConsistMostly1,
                        RC.Taiko.InnerOni_MainSnappingConsistMostly1
                    )
            },
            {
                "Catch",
                Template()
                    .WithRule(
                        RC.Catch.Cup_NoteDensityFollowMostly1,
                        RC.Catch.Salad_NoteDensityFollowMostly1,
                        RC.Catch.Platter_NoteDensityFollowMostly1,
                        RC.Catch.Rain_NoteDensityFollowMostly1
                    )
            },
        };

    private static IssueTemplate Template() =>
        new IssueTemplate(
            Issue.Level.Minor,
            "{0}% of the rhythm is faster than {1}, while a {2} should mostly use {3}.",
            "percentage",
            "snap",
            "difficulty",
            "rhythms"
        ).WithCause(
            "Most of the gaps between notes are faster than the main snapping of the difficulty level, measured at 180 BPM."
        );

    public override IEnumerable<Issue> GetIssues(Beatmap beatmap)
    {
        // A level guessed from star rating says little here, since star rating mostly follows density
        // itself. It also keeps hitsound difficulties, which tend to read as the lowest level, out.
        if (beatmap.IsDifficultyGuessed())
            yield break;

        var mode = beatmap.GeneralSettings.mode;
        var gaps = RhythmUtils.GetGaps(beatmap);

        if (gaps.Count < MinGaps)
            yield break;

        foreach (var ((limitMode, difficulty), limit) in Limits)
        {
            if (limitMode != mode)
                continue;

            var share = RhythmUtils.GetShareFasterThan(gaps, limit.Divisor);

            if (share <= limit.MaxShare)
                continue;

            yield return new Issue(
                GetTemplate(mode.ToString()),
                beatmap,
                (share * 100).ToString("0", CultureInfo.InvariantCulture),
                FormatSnap(limit.Divisor),
                beatmap.GetModeDifficultyName(difficulty),
                limit.Rhythms
            ).ForDifficulties(difficulty);
        }
    }

    private static string FormatSnap(double divisor) =>
        divisor < 1
            ? $"{(1 / divisor).ToString("0", CultureInfo.InvariantCulture)}/1"
            : $"1/{divisor.ToString("0", CultureInfo.InvariantCulture)}";

    private readonly record struct Limit(double Divisor, double MaxShare, string Rhythms);
}
