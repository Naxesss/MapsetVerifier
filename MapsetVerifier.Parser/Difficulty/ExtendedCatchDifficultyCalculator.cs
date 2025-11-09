using osu.Game.Beatmaps;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Difficulty;
using osu.Game.Rulesets.Difficulty.Preprocessing;
using osu.Game.Rulesets.Difficulty.Skills;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Catch.Difficulty;

namespace MapsetVerifier.Parser.Difficulty;

/// <summary>
/// In regular difficulty calculators, skills are not exposed publicly, which <c>SkillChartRenderer</c> depends on.<br/>
/// This extended class serves to provide access to the calculated skills via the <see cref="GetSkills"/> method.<br/>
/// Sourced from <a href="https://github.com/ppy/osu-tools/blob/ab97b64f60901952926b2121ddffb8976d7f8775/PerformanceCalculatorGUI/ExtendedCatchDifficultyCalculator.cs"><c>osu-tools</c>' approach of tackling the same problem</a>.
/// </summary>
public class ExtendedCatchDifficultyCalculator : CatchDifficultyCalculator, IExtendedDifficultyCalculator
{
    private Skill[] skills;

    public ExtendedCatchDifficultyCalculator(IRulesetInfo ruleset, IWorkingBeatmap beatmap)
        : base(ruleset, beatmap)
    {
    }

    public Skill[] GetSkills() => skills;
    public DifficultyHitObject[] GetDifficultyHitObjects(IBeatmap beatmap, double clockRate) => CreateDifficultyHitObjects(beatmap, clockRate).ToArray();

    protected override DifficultyAttributes CreateDifficultyAttributes(IBeatmap beatmap, Mod[] mods, Skill[] skills, double clockRate)
    {
        this.skills = skills;
        return base.CreateDifficultyAttributes(beatmap, mods, skills, clockRate);
    }
}
