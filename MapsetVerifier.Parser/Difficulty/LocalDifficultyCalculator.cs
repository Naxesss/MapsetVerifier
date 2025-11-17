using osu.Game.Beatmaps;
using osu.Game.Rulesets.Difficulty;
using Beatmap = MapsetVerifier.Parser.Objects.Beatmap;

namespace MapsetVerifier.Parser.Difficulty;

public class LocalDifficultyCalculator
{
    public DifficultyAttributes CalculateAttributes(Beatmap mvBeatmap)
    {
        var workingBeatmap = new FlatWorkingBeatmap(mvBeatmap.SongPath + "\\" + mvBeatmap.MapPath);
        var ruleset = workingBeatmap.BeatmapInfo.Ruleset.CreateInstance();

        return mvBeatmap.GeneralSettings.mode switch
        {
            Beatmap.Mode.Standard => new ExtendedOsuDifficultyCalculator(ruleset.RulesetInfo, workingBeatmap, mvBeatmap).Calculate(),
            Beatmap.Mode.Taiko => new ExtendedTaikoDifficultyCalculator(ruleset.RulesetInfo, workingBeatmap, mvBeatmap).Calculate(),
            Beatmap.Mode.Catch => new ExtendedCatchDifficultyCalculator(ruleset.RulesetInfo, workingBeatmap, mvBeatmap).Calculate(),
            Beatmap.Mode.Mania => new ExtendedManiaDifficultyCalculator(ruleset.RulesetInfo, workingBeatmap, mvBeatmap).Calculate(),
            _ => throw new ArgumentException("Invalid mode"),
        };
    }
}
