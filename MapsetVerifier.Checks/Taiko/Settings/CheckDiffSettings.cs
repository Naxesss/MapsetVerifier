using MapsetVerifier.Parser.Objects;
using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Framework.Objects.Attributes;
using MapsetVerifier.Framework.Objects.Metadata;

namespace MapsetVerifier.Checks.Taiko.Settings;

[Check]
public class CheckDiffSettings : BeatmapCheck
{
    // private const bool _DEBUG_SEE_ALL_HP = false;
    // private const bool _DEBUG_SEE_ALL_OD = false;
    private readonly Beatmap.Difficulty[] difficulties =
    [
        Beatmap.Difficulty.Easy,
        Beatmap.Difficulty.Normal,
        Beatmap.Difficulty.Hard,
        Beatmap.Difficulty.Insane,
    ];
    
    private static double NormalizeHpWithDrain(double hp, double drain)
    {
        if (drain <= 60 * 1000) // 1:00 or less gets an HP buff by 1
            return Math.Ceiling(hp + 1); // rounding up to avoid decimal values
        if (drain >= (4 * 60 * 1000) + (45 * 1000)) // 4:45 or more gets an HP nerf by 2
            return Math.Ceiling(hp - 2);
        if (drain >= (3 * 60 * 1000) + (45 * 1000)) // 3:45 or more gets an HP nerf by 1
            return Math.Ceiling(hp - 1);
        return hp;
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
                        <note>
                            The recommended settings below are based on the current ranking criteria with
                            adjustments based on mapping standards and drain time, 
                            so make sure you apply your own judgment as well.
                        </note>

                        <style type='text/css' scoped>
                            table, th, td {
                                border: 1px solid;
                                border-collapse: collapse;
                                padding: 5px;
                            }
                        </style>

                        <h3>Recommended OD settings</h3>
                        <table>
                            <tr>
                                <th>Difficulty</th>
                                <th>Value</th>
                            </tr>
                            <tr>
                                <td>Kantan OD</td>
                                <td>3</td>
                            </tr>
                            <tr>
                                <td>Futsuu OD</td>
                                <td>4</td>
                            </tr>
                            <tr>
                                <td>Muzukashii OD</td>
                                <td>5</td>
                            </tr>
                            <tr>
                                <td>Oni OD</td>
                                <td>5.5</td>
                            </tr>
                        </table>
                        
                        <h3>Recommended HP settings</h3>
                        <table>
                            <tr>
                                <th>Difficulty</th>
                                <th>Drain <= 1:00</th>
                                <th>1:00 < Drain < 3:45</th>
                                <th>3:45 <= Drain < 4:45</th>
                                <th>Drain >= 4:45</th>
                            </tr>
                            <tr>
                                <td>Kantan HP</td>
                                <td>9~10</td>
                                <td>8~9</td>
                                <td>7~8</td>
                                <td>6~7</td>
                            </tr>
                            <tr>
                                <td>Futsuu HP</td>
                                <td>8~9</td>
                                <td>7~8</td>
                                <td>6~7</td>
                                <td>5~6</td>
                            </tr>
                            <tr>
                                <td>Muzukashii HP</td>
                                <td>7~8</td>
                                <td>6~7</td>
                                <td>5~6</td>
                                <td>4~5</td>
                            </tr>
                            <tr>
                                <td>Oni HP</td>
                                <td>7~8</td>
                                <td>5.5~6.5</td>
                                <td>5~6</td>
                                <td>4~5</td>
                            </tr>
                        </table>
                        "
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
            /*{
                "debug",
                new IssueTemplate(
                    Issue.Level.Warning,
                    "{0} - limit: {1} - current: {2}",
                    "setting",
                    "limit",
                    "current"
                ).WithCause("Debug")
            }*/
        };

    public override IEnumerable<Issue> GetIssues(Beatmap beatmap)
    {
        double hp = Math.Round(beatmap.DifficultySettings.hpDrain, 2, MidpointRounding.ToEven);
        double od = Math.Round(
            beatmap.DifficultySettings.overallDifficulty,
            2,
            MidpointRounding.ToEven
        );

        var recommendedOd = new Dictionary<Beatmap.Difficulty, double>()
        {
            { Beatmap.Difficulty.Easy, 3 },
            { Beatmap.Difficulty.Normal, 4 },
            { Beatmap.Difficulty.Hard, 5 },
            { Beatmap.Difficulty.Insane, 5.5 }
        };

        var recommendedHp = new Dictionary<Beatmap.Difficulty, double>()
        {
            { Beatmap.Difficulty.Easy, 8 },
            { Beatmap.Difficulty.Normal, 7 },
            { Beatmap.Difficulty.Hard, 6 },
            { Beatmap.Difficulty.Insane, 5.5 }
        };

        foreach (var diff in difficulties)
        {
            double drain = beatmap.GetDrainTime(Beatmap.Mode.Taiko);
            double normalizedRecommendedHp = NormalizeHpWithDrain(
                recommendedHp[diff],
                drain
            );

            /*if (_DEBUG_SEE_ALL_HP)
            {
                yield return new Issue(
                    GetTemplate("debug"),
                    beatmap,
                    "HP",
                    normalizedRecommendedHp,
                    HP
                ).ForDifficulties(diff);
            };*/

            if (Math.Abs(hp - normalizedRecommendedHp) > 1)
            {
                yield return new Issue(
                    GetTemplate("hpWarning"),
                    beatmap,
                    normalizedRecommendedHp,
                    hp
                ).ForDifficulties(diff);
            }
            else if (Math.Abs(hp - normalizedRecommendedHp) > 0)
            {
                Console.WriteLine(hp);
                yield return new Issue(
                    GetTemplate("hpMinor"),
                    beatmap,
                    normalizedRecommendedHp,
                    hp
                ).ForDifficulties(diff);
            }

            ;

            /*if (_DEBUG_SEE_ALL_OD)
            {
                yield return new Issue(
                    GetTemplate("debug"),
                    beatmap,
                    "OD",
                    recommendedOd[diff],
                    OD
                ).ForDifficulties(diff);
            };*/

            if (Math.Abs(od - recommendedOd[diff]) > 0.5)
            {
                yield return new Issue(
                    GetTemplate("odWarning"),
                    beatmap,
                    recommendedOd[diff],
                    od
                ).ForDifficulties(diff);
            }
            else if (Math.Abs(od - recommendedOd[diff]) > 0)
            {
                yield return new Issue(
                    GetTemplate("odMinor"),
                    beatmap,
                    recommendedOd[diff],
                    od
                ).ForDifficulties(diff);
            };
        }
    }
}
