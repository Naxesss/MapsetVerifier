// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using MapsetVerifier.Parser.StarRating.Preprocessing;
using MapsetVerifier.Parser.StarRating.Skills;
using MapsetVerifier.Parser.StarRating.Taiko.Evaluators;

namespace MapsetVerifier.Parser.StarRating.Taiko.Skills
{
    /// <summary>
    ///     Calculates the stamina coefficient of taiko difficulty.
    /// </summary>
    public class Stamina : StrainDecaySkill
    {
        /// <summary>
        ///     Creates a <see cref="Stamina" /> skill.
        /// </summary>
        public Stamina() { }

        protected override double SkillMultiplier => 1.1;
        protected override double StrainDecayBase => 0.4;

        public override string SkillName() => "Stamina";

        protected override double StrainValueOf(DifficultyHitObject current) => StaminaEvaluator.EvaluateDifficultyOf(current);
    }
}