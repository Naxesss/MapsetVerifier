using osu.Game.Beatmaps;
using osu.Game.Rulesets.Difficulty;
using osu.Game.Rulesets.Mods;
using Beatmap = MapsetVerifier.Parser.Objects.Beatmap;

namespace MapsetVerifier.Parser.Difficulty;

public class LocalDifficultyCalculator
{
    // osu! replaces a non-cancelable token with its own 10s timeout inside Calculate/CalculateTimed.
    // Pass a cancelable token we never fire so long maps can finish.
    private static readonly CancellationToken UncancelledToken =
        new CancellationTokenSource().Token;

    public DifficultyAttributes CalculateAttributes(Beatmap mvBeatmap) =>
        CreateCalculator(mvBeatmap).Calculate(Array.Empty<Mod>(), UncancelledToken);

    public List<TimedDifficultyAttributes> CalculateTimedAttributes(Beatmap mvBeatmap) =>
        CreateCalculator(mvBeatmap).CalculateTimed(Array.Empty<Mod>(), UncancelledToken);

    private DifficultyCalculator CreateCalculator(Beatmap mvBeatmap)
    {
        var workingBeatmap = new FlatWorkingBeatmap(
            Path.Combine(mvBeatmap.SongPath, mvBeatmap.MapPath)
        );
        var ruleset = workingBeatmap.BeatmapInfo.Ruleset.CreateInstance();

        return mvBeatmap.GeneralSettings.mode switch
        {
            Beatmap.Mode.Standard => new ExtendedOsuDifficultyCalculator(
                ruleset.RulesetInfo,
                workingBeatmap,
                mvBeatmap
            ),
            Beatmap.Mode.Taiko => new ExtendedTaikoDifficultyCalculator(
                ruleset.RulesetInfo,
                workingBeatmap,
                mvBeatmap
            ),
            Beatmap.Mode.Catch => new ExtendedCatchDifficultyCalculator(
                ruleset.RulesetInfo,
                workingBeatmap,
                mvBeatmap
            ),
            Beatmap.Mode.Mania => new ExtendedManiaDifficultyCalculator(
                ruleset.RulesetInfo,
                workingBeatmap,
                mvBeatmap
            ),
            _ => throw new ArgumentException("Invalid mode"),
        };
    }
}
