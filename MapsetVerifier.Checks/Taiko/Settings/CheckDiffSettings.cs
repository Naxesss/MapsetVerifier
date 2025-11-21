using MapsetVerifier.Parser.Objects;
using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Framework.Objects.Attributes;
using MapsetVerifier.Framework.Objects.Metadata;

namespace MapsetVerifier.Checks.Taiko.Settings;

[Check]
public class CheckDiffSettings : BeatmapCheck
{
    private readonly Beatmap.Difficulty[] difficulties =
    [
        Beatmap.Difficulty.Easy,
        Beatmap.Difficulty.Normal,
        Beatmap.Difficulty.Hard,
        Beatmap.Difficulty.Insane,
    ];

    private static readonly Dictionary<Beatmap.Difficulty, double> RecommendedOd = new()
    {
        { Beatmap.Difficulty.Easy, 3 },
        { Beatmap.Difficulty.Normal, 4 },
        { Beatmap.Difficulty.Hard, 5 },
        { Beatmap.Difficulty.Insane, 5.5 }
    };

    /// <summary>
    ///     HP values indexed by difficulty and drain time category:
    ///     <list type="bullet">
    ///         <item><description>Index 0: drain &lt;= 1:00</description></item>
    ///         <item><description>Index 1: 1:00 &lt; drain &lt; 3:45</description></item>
    ///         <item><description>Index 2: 3:45 &lt;= drain &lt; 4:45</description></item>
    ///         <item><description>Index 3: drain &gt;= 4:45</description></item>
    ///    </list>
    /// </summary>
    private static readonly Dictionary<Beatmap.Difficulty, double[]> RecommendedHp = new()
    {
        { Beatmap.Difficulty.Easy, [9, 8, 7, 6] },
        { Beatmap.Difficulty.Normal, [8, 7, 6, 5] },
        { Beatmap.Difficulty.Hard, [7, 6, 5, 4] },
        { Beatmap.Difficulty.Insane, [7, 5.5, 5, 4] }
    };

    private static int GetDrainIndex(double drain)
    {
        if (drain <= 60 * 1000) // <= 1:00
            return 0;
        if (drain >= (4 * 60 * 1000) + (45 * 1000)) // >= 4:45
            return 3;
        if (drain >= (3 * 60 * 1000) + (45 * 1000)) // >= 3:45
            return 2;
        return 1; // 1:00 < drain < 3:45
    }

    public override CheckMetadata GetMetadata() =>
        new BeatmapCheckMetadata()
        {
            Author = "Hivie",
            Category = "Settings",
            Message = "OD/HP values too high/low",
            Difficulties = difficulties,
            Modes = [Beatmap.Mode.Taiko],
            Documentation = new Dictionary<string, string>()
            {
                {
                    "Purpose",
                    @"
                        Preventing difficulties from going beyond or below the
                        recommended OD/HP values present in the ranking criteria.
                        "
                },
                {
                    "Reasoning",
                    @"
                    OD/HP values that are too high or low can cause gameplay imbalances.

                    > The recommended settings below are based on the current ranking criteria with adjustments based on mapping standards and drain time, so make sure you apply your own judgment as well.

### Recommended OD settings

| Difficulty | Overall Difficulty |
|---|---|
| Kantan | 3 |
| Futsuu | 4 |
| Muzukashii | 5 |
| Oni | 5.5 |

### Recommended HP settings

| Difficulty | Drain <= 1:00 | 1:00 < Drain < 3:45  | 3:45 <= Drain < 4:45 | Drain >= 4:45  |
| ---------- | ------------- | -------------------- | -------------------- | -------------- |
| Kantan     | 9~10          | 8~9                  | 7~8                  | 6~7            |
| Futsuu     | 8~9           | 7~8                  | 6~7                  | 5~6            |
| Muzukashii | 7~8           | 6~7                  | 5~6                  | 4~5            |
| Oni        | 7~8           | 5.5~6.5              | 5~6                  | 4~5            |"
                }
            }
        };

    public override Dictionary<string, IssueTemplate> GetTemplates() =>
        new()
        {
            {
                "hpMinor",
                new IssueTemplate(
                    Issue.Level.Minor,
                    "HP is different from suggested value {0}, currently {1}.",
                    "limit",
                    "current"
                ).WithCause("Current value is slightly different from the recommended limits.")
            },
            {
                "hpWarning",
                new IssueTemplate(
                    Issue.Level.Warning,
                    "HP is different from suggested value {0}, currently {1}. Ensure this makes sense.",
                    "limit",
                    "current"
                ).WithCause(
                    "Current value is considerably different from the recommended limits."
                )
            },
            {
                "odMinor",
                new IssueTemplate(
                    Issue.Level.Minor,
                    "OD is different from suggested value {0}, currently {1}.",
                    "limit",
                    "current"
                ).WithCause("Current value is slightly different from the recommended limits.")
            },
            {
                "odWarning",
                new IssueTemplate(
                    Issue.Level.Warning,
                    "OD is different from suggested value {0}, currently {1}. Ensure this makes sense.",
                    "limit",
                    "current"
                ).WithCause(
                    "Current value is considerably different from the recommended limits."
                )
            },
        };

    public override IEnumerable<Issue> GetIssues(Beatmap beatmap)
    {
        double hp = Math.Round(beatmap.DifficultySettings.hpDrain, 2, MidpointRounding.ToEven);
        double od = Math.Round(beatmap.DifficultySettings.overallDifficulty, 2, MidpointRounding.ToEven);

        foreach (var diff in difficulties)
        {
            double drain = beatmap.GetDrainTime(Beatmap.Mode.Taiko);
            int drainIndex = GetDrainIndex(drain);
            double recommendedHp = RecommendedHp[diff][drainIndex];

            if (Math.Abs(hp - recommendedHp) > 1)
            {
                yield return new Issue(
                    GetTemplate("hpWarning"),
                    beatmap,
                    recommendedHp,
                    hp
                ).ForDifficulties(diff);
            }
            else if (Math.Abs(hp - recommendedHp) > 0)
            {
                yield return new Issue(
                    GetTemplate("hpMinor"),
                    beatmap,
                    recommendedHp,
                    hp
                ).ForDifficulties(diff);
            }

            if (Math.Abs(od - RecommendedOd[diff]) > 0.5)
            {
                yield return new Issue(
                    GetTemplate("odWarning"),
                    beatmap,
                    RecommendedOd[diff],
                    od
                ).ForDifficulties(diff);
            }
            else if (Math.Abs(od - RecommendedOd[diff]) > 0)
            {
                yield return new Issue(
                    GetTemplate("odMinor"),
                    beatmap,
                    RecommendedOd[diff],
                    od
                ).ForDifficulties(diff);
            }
        }
    }
}