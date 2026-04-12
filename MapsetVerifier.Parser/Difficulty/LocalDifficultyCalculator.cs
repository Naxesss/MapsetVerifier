using osu.Game.Beatmaps;
using osu.Game.Rulesets.Difficulty;
using Beatmap = MapsetVerifier.Parser.Objects.Beatmap;

namespace MapsetVerifier.Parser.Difficulty;

public class LocalDifficultyCalculator
{
    public DifficultyAttributes CalculateAttributes(Beatmap mvBeatmap)
        => CreateCalculator(mvBeatmap).Calculate();

    public List<TimedDifficultyAttributes> CalculateTimedAttributes(Beatmap mvBeatmap)
        => CreateCalculator(mvBeatmap).CalculateTimed();

    private DifficultyCalculator CreateCalculator(Beatmap mvBeatmap)
    {
        var workingBeatmap = new FlatWorkingBeatmap(Path.Combine(mvBeatmap.SongPath, mvBeatmap.MapPath));
        var ruleset = workingBeatmap.BeatmapInfo.Ruleset.CreateInstance();

        return mvBeatmap.GeneralSettings.mode switch
        {
            Beatmap.Mode.Standard => new ExtendedOsuDifficultyCalculator(ruleset.RulesetInfo, workingBeatmap, mvBeatmap),
            Beatmap.Mode.Taiko => new ExtendedTaikoDifficultyCalculator(ruleset.RulesetInfo, workingBeatmap, mvBeatmap),
            Beatmap.Mode.Catch => new ExtendedCatchDifficultyCalculator(ruleset.RulesetInfo, workingBeatmap, mvBeatmap),
            Beatmap.Mode.Mania => new ExtendedManiaDifficultyCalculator(ruleset.RulesetInfo, workingBeatmap, mvBeatmap),
            _ => throw new ArgumentException("Invalid mode"),
        };
    }
}
