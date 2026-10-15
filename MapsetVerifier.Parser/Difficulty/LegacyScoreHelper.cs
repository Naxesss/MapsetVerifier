using osu.Game.Beatmaps;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Scoring.Legacy;
using Beatmap = MapsetVerifier.Parser.Objects.Beatmap;

namespace MapsetVerifier.Parser.Difficulty;

/// <summary>
///     Computes maximum legacy (ScoreV1) scores using osu!'s legacy score simulators.
/// </summary>
public static class LegacyScoreHelper
{
    /// <summary>
    ///     Returns the maximum ScoreV1 total achievable on an HDHRDTFL SS (including bonus score).
    ///     Excludes osu!mania as it caps score at 1 million.-
    /// </summary>
    public static long GetMaximumLegacyScore(Beatmap mvBeatmap)
    {
        if (mvBeatmap.GeneralSettings.mode == Beatmap.Mode.Mania)
            throw new ArgumentException(
                "Legacy score overflow calculation does not apply to mania."
            );

        var workingBeatmap = new FlatWorkingBeatmap(
            Path.Combine(mvBeatmap.SongPath, mvBeatmap.MapPath)
        );
        var ruleset = workingBeatmap.BeatmapInfo.Ruleset.CreateInstance();

        if (ruleset is not ILegacyRuleset legacyRuleset)
            throw new InvalidOperationException(
                $"Ruleset {ruleset.RulesetInfo.ShortName} does not support legacy score simulation."
            );

        var mods = CreateMaxMultiplierMods(ruleset);
        var playableBeatmap = workingBeatmap.GetPlayableBeatmap(ruleset.RulesetInfo, mods);
        var simulator = legacyRuleset.CreateLegacyScoreSimulator();
        var attributes = simulator.Simulate(workingBeatmap, playableBeatmap);
        var difficulty = LegacyBeatmapConversionDifficultyInfo.FromBeatmap(workingBeatmap.Beatmap);
        double modMultiplier = simulator.GetLegacyScoreMultiplier(mods, difficulty);

        // ScoreV1 only applies the mod multiplier to the combo portion.
        return attributes.AccuracyScore
            + (long)Math.Round(attributes.ComboScore * modMultiplier)
            + attributes.BonusScore;
    }

    private static Mod[] CreateMaxMultiplierMods(Ruleset ruleset)
    {
        var hidden = ruleset.CreateMod<ModHidden>();
        var hardRock = ruleset.CreateMod<ModHardRock>();
        var doubleTime = ruleset.CreateMod<ModDoubleTime>();
        var flashlight = ruleset.CreateMod<ModFlashlight>();

        if (hidden == null || hardRock == null || doubleTime == null || flashlight == null)
            throw new InvalidOperationException(
                $"Ruleset {ruleset.RulesetInfo.ShortName} is missing HD/HR/DT/FL mods required for max ScoreV1."
            );

        return [hidden, hardRock, doubleTime, flashlight];
    }
}
