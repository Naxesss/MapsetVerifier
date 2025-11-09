using osu.Game.Beatmaps;
using osu.Game.Rulesets.Difficulty;
using Beatmap = MapsetVerifier.Parser.Objects.Beatmap;

namespace MapsetVerifier.Parser.Difficulty;

public class LocalDifficultyCalculator
{
    public DifficultyAttributes CalculateAttributes(string osuPath, Beatmap.Mode mode)
    {
        var workingBeatmap = new FlatWorkingBeatmap(osuPath);
        var ruleset = workingBeatmap.BeatmapInfo.Ruleset.CreateInstance();

        return mode switch
        {
            Beatmap.Mode.Standard => new ExtendedOsuDifficultyCalculator(ruleset.RulesetInfo, workingBeatmap).Calculate(),
            Beatmap.Mode.Taiko => new ExtendedTaikoDifficultyCalculator(ruleset.RulesetInfo, workingBeatmap).Calculate(),
            Beatmap.Mode.Catch => new ExtendedCatchDifficultyCalculator(ruleset.RulesetInfo, workingBeatmap).Calculate(),
            Beatmap.Mode.Mania => new ExtendedManiaDifficultyCalculator(ruleset.RulesetInfo, workingBeatmap).Calculate(),
            _ => throw new ArgumentException("Invalid mode"),
        };
    }
}
