using MapsetVerifier.Checks.AllModes.Settings;
using MapsetVerifier.Framework.Objects.Attributes;
using MapsetVerifier.Framework.Objects.Metadata;
using MapsetVerifier.Parser.Objects;

namespace MapsetVerifier.Checks.Standard.Settings;

[Check]
public class CheckDiffSettingsStandard : MinorRangeDifficultySettingsCheck
{
    private static readonly Dictionary<Beatmap.Difficulty, SettingRange> ApproachRateRanges = new()
    {
        { Beatmap.Difficulty.Easy, new SettingRange(null, 5) },
        { Beatmap.Difficulty.Normal, new SettingRange(4, 6) },
        { Beatmap.Difficulty.Hard, new SettingRange(6, 8) },
        { Beatmap.Difficulty.Insane, new SettingRange(7, 9.3) },
        { Beatmap.Difficulty.Expert, new SettingRange(8, null) },
    };

    private static readonly Dictionary<Beatmap.Difficulty, SettingRange> OverallDifficultyRanges =
        new()
        {
            { Beatmap.Difficulty.Easy, new SettingRange(1, 3) },
            { Beatmap.Difficulty.Normal, new SettingRange(3, 5) },
            { Beatmap.Difficulty.Hard, new SettingRange(5, 7) },
            { Beatmap.Difficulty.Insane, new SettingRange(7, 9) },
            { Beatmap.Difficulty.Expert, new SettingRange(8, null) },
        };

    private static readonly Dictionary<Beatmap.Difficulty, SettingRange> HpDrainRanges = new()
    {
        { Beatmap.Difficulty.Easy, new SettingRange(1, 3) },
        { Beatmap.Difficulty.Normal, new SettingRange(3, 5) },
        { Beatmap.Difficulty.Hard, new SettingRange(4, 6) },
        { Beatmap.Difficulty.Insane, new SettingRange(5, 8) },
        { Beatmap.Difficulty.Expert, new SettingRange(5, null) },
    };

    private static readonly Dictionary<Beatmap.Difficulty, SettingRange> CircleSizeRanges = new()
    {
        { Beatmap.Difficulty.Easy, new SettingRange(null, 4) },
        { Beatmap.Difficulty.Normal, new SettingRange(null, 5) },
        { Beatmap.Difficulty.Hard, new SettingRange(null, 6) },
        { Beatmap.Difficulty.Insane, new SettingRange(null, 7) },
        { Beatmap.Difficulty.Expert, new SettingRange(null, 7) },
    };

    protected override IReadOnlyList<SettingDefinition> Settings { get; } =
    [
        new SettingDefinition(
            "AR",
            beatmap => beatmap.DifficultySettings.approachRate,
            ApproachRateRanges
        ),
        new SettingDefinition(
            "OD",
            beatmap => beatmap.DifficultySettings.overallDifficulty,
            OverallDifficultyRanges
        ),
        new SettingDefinition("HP", beatmap => beatmap.DifficultySettings.hpDrain, HpDrainRanges),
        new SettingDefinition(
            "CS",
            beatmap => beatmap.DifficultySettings.circleSize,
            CircleSizeRanges
        ),
    ];

    public override CheckMetadata GetMetadata() =>
        new BeatmapCheckMetadata
        {
            Author = "Greaper",
            Category = "Settings",
            Message = "Difficulty settings outside of the guideline range.",
            Difficulties =
            [
                Beatmap.Difficulty.Easy,
                Beatmap.Difficulty.Normal,
                Beatmap.Difficulty.Hard,
                Beatmap.Difficulty.Insane,
                Beatmap.Difficulty.Expert,
            ],
            Modes = [Beatmap.Mode.Standard],
            Documentation = new Dictionary<string, string>
            {
                {
                    "Purpose",
                    @"
                        Preventing difficulties from straying far outside of the recommended
                        AR/OD/HP/CS guideline ranges present in the ranking criteria.
                        "
                },
                {
                    "Reasoning",
                    @"
                    Settings that are far outside of the guideline range for a difficulty's
                    name can cause gameplay expectations to not line up with the actual
                    difficulty of the map, so make sure to apply your own judgment as well.

### Recommended settings

| Difficulty | AR | OD | HP | CS |
|---|---|---|---|---|
| Easy | 5 or lower | 1~3 | 1~3 | 4 or lower |
| Normal | 4~6 | 3~5 | 3~5 | 5 or lower |
| Hard | 6~8 | 5~7 | 4~6 | 6 or lower |
| Insane | 7~9.3 | 7~9 | 5~8 | 7 or lower |
| Expert | 8 or higher | 8 or higher | 5 or higher | 7 or lower |"
                },
            },
        };
}
