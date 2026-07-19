using MapsetVerifier.Checks.AllModes.Settings;
using MapsetVerifier.Framework.Objects.Attributes;
using MapsetVerifier.Framework.Objects.Metadata;
using MapsetVerifier.Parser.Objects;

namespace MapsetVerifier.Checks.Catch.Settings;

[Check]
public class CheckDiffSettings : MinorRangeDifficultySettingsCheck
{
    private static readonly Dictionary<Beatmap.Difficulty, SettingRange> ApproachRateRanges = new()
    {
        { Beatmap.Difficulty.Easy, new SettingRange(null, 6) },
        { Beatmap.Difficulty.Normal, new SettingRange(null, 7) },
        { Beatmap.Difficulty.Hard, new SettingRange(null, 8) },
        { Beatmap.Difficulty.Insane, new SettingRange(null, 9) },
        { Beatmap.Difficulty.Expert, new SettingRange(9, null) },
    };

    private static readonly Dictionary<Beatmap.Difficulty, SettingRange> OverallDifficultyRanges =
        new()
        {
            { Beatmap.Difficulty.Easy, new SettingRange(null, 6) },
            { Beatmap.Difficulty.Normal, new SettingRange(null, 7) },
            { Beatmap.Difficulty.Hard, new SettingRange(null, 8) },
            { Beatmap.Difficulty.Insane, new SettingRange(null, 9) },
            { Beatmap.Difficulty.Expert, new SettingRange(9, null) },
        };

    private static readonly Dictionary<Beatmap.Difficulty, SettingRange> HpDrainRanges = new()
    {
        { Beatmap.Difficulty.Easy, new SettingRange(2, 3) },
        { Beatmap.Difficulty.Normal, new SettingRange(3, 4) },
        { Beatmap.Difficulty.Hard, new SettingRange(4, 5) },
        { Beatmap.Difficulty.Insane, new SettingRange(5, 6) },
        { Beatmap.Difficulty.Expert, new SettingRange(5, null) },
    };

    private static readonly Dictionary<Beatmap.Difficulty, SettingRange> CircleSizeRanges = new()
    {
        { Beatmap.Difficulty.Easy, new SettingRange(null, 2.5) },
        { Beatmap.Difficulty.Normal, new SettingRange(null, 3) },
        { Beatmap.Difficulty.Hard, new SettingRange(null, 3.5) },
        { Beatmap.Difficulty.Insane, new SettingRange(null, 4) },
        { Beatmap.Difficulty.Expert, new SettingRange(4, null) },
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
            Modes = [Beatmap.Mode.Catch],
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

| Difficulty | AR/OD | HP | CS |
|---|---|---|---|
| Easy | 6 or lower | 2~3 | 2.5 or lower |
| Normal | 7 or lower | 3~4 | 3 or lower |
| Hard | 8 or lower | 4~5 | 3.5 or lower |
| Insane | 9 or lower | 5~6 | 4 or lower |
| Expert | 9 or higher | 5 or higher | 4 or higher |"
                },
            },
        };
}
