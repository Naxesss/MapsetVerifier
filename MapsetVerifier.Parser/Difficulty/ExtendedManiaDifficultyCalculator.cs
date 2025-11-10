using osu.Game.Beatmaps;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Difficulty;
using osu.Game.Rulesets.Difficulty.Preprocessing;
using osu.Game.Rulesets.Difficulty.Skills;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Mania.Difficulty;
using Beatmap = MapsetVerifier.Parser.Objects.Beatmap;

namespace MapsetVerifier.Parser.Difficulty;

/// <summary>
/// In regular difficulty calculators, skills are not exposed publicly, which <c>SkillChartRenderer</c> depends on.<br/>
/// This extended class serves to provide access to the calculated skills via the <see cref="GetSkills"/> method.<br/>
/// Sourced from <a href="https://github.com/ppy/osu-tools/blob/ab97b64f60901952926b2121ddffb8976d7f8775/PerformanceCalculatorGUI/ExtendedManiaDifficultyCalculator.cs"><c>osu-tools</c>' approach of tackling the same problem</a>.
/// </summary>
public class ExtendedManiaDifficultyCalculator : ManiaDifficultyCalculator, IExtendedDifficultyCalculator
{
    private Beatmap mvBeatmap;

    public ExtendedManiaDifficultyCalculator(IRulesetInfo ruleset, IWorkingBeatmap beatmap, Beatmap mvBeatmap)
        : base(ruleset, beatmap)
    {
        this.mvBeatmap = mvBeatmap;
    }

    public Skill[] GetSkills() => mvBeatmap.Skills;
    public DifficultyHitObject[] GetDifficultyHitObjects(IBeatmap beatmap, double clockRate) => CreateDifficultyHitObjects(beatmap, clockRate).ToArray();

    protected override DifficultyAttributes CreateDifficultyAttributes(IBeatmap beatmap, Mod[] mods, Skill[] skills, double clockRate)
    {
        mvBeatmap.Skills = skills;
        return base.CreateDifficultyAttributes(beatmap, mods, mvBeatmap.Skills, clockRate);
    }
}
