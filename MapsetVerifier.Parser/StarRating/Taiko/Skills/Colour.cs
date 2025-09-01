// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using MapsetVerifier.Parser.StarRating.Preprocessing;
using MapsetVerifier.Parser.StarRating.Skills;
using MapsetVerifier.Parser.StarRating.Taiko.Evaluators;

namespace MapsetVerifier.Parser.StarRating.Taiko.Skills
{
    /// <summary>
    ///     Calculates the colour coefficient of taiko difficulty.
    /// </summary>
    public class Colour : StrainDecaySkill
    {
        protected override double SkillMultiplier => 0.12;

        // This is set to decay slower than other skills, due to the fact that only the first note of each encoding class
        //  having any difficulty values, and we want to allow colour difficulty to be able to build up even on
        // slower maps.
        protected override double StrainDecayBase => 0.8;

        public override string SkillName() => "Colour";

        protected override double StrainValueOf(DifficultyHitObject current) => ColourEvaluator.EvaluateDifficultyOf(current);
    }
}