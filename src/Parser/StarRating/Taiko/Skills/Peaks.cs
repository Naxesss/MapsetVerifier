// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using MapsetVerifier.Parser.StarRating.Preprocessing;
using MapsetVerifier.Parser.StarRating.Skills;

namespace MapsetVerifier.Parser.StarRating.Taiko.Skills
{
    public class Peaks : StrainSkill
    {
        private const double rhythm_skill_multiplier = 0.2 * final_multiplier;
        private const double colour_skill_multiplier = 0.375 * final_multiplier;
        private const double stamina_skill_multiplier = 0.375 * final_multiplier;

        private const double final_multiplier = 0.0625;
        private readonly Colour colour;

        private readonly Rhythm rhythm;
        private readonly Stamina stamina;

        public Peaks()
        {
            rhythm = new Rhythm();
            colour = new Colour();
            stamina = new Stamina();
        }

        public double ColourDifficultyValue => colour.DifficultyValue() * colour_skill_multiplier;
        public double RhythmDifficultyValue => rhythm.DifficultyValue() * rhythm_skill_multiplier;
        public double StaminaDifficultyValue => stamina.DifficultyValue() * stamina_skill_multiplier;

        public override string SkillName() => "Peaks";

        /// <summary>
        ///     Returns the <i>p</i>-norm of an <i>n</i>-dimensional vector.
        /// </summary>
        /// <param name="p">The value of <i>p</i> to calculate the norm for.</param>
        /// <param name="values">The coefficients of the vector.</param>
        private double norm(double p, params double[] values) => Math.Pow(values.Sum(x => Math.Pow(x, p)), 1 / p);

        public override void Process(DifficultyHitObject current)
        {
            rhythm.Process(current);
            colour.Process(current);
            stamina.Process(current);
        }

        /// <summary>
        ///     Returns the combined star rating of the beatmap, calculated using peak strains from all sections of the map.
        /// </summary>
        /// <remarks>
        ///     For each section, the peak strains of all separate skills are combined into a single peak strain for the section.
        ///     The resulting partial rating of the beatmap is a weighted sum of the combined peaks (higher peaks are weighted
        ///     more).
        /// </remarks>
        public override double DifficultyValue()
        {
            var peaks = new List<double>();

            var colourPeaks = colour.GetCurrentStrainPeaks().ToList();
            var rhythmPeaks = rhythm.GetCurrentStrainPeaks().ToList();
            var staminaPeaks = stamina.GetCurrentStrainPeaks().ToList();

            for (var i = 0; i < colourPeaks.Count; i++)
            {
                var colourPeak = colourPeaks[i] * colour_skill_multiplier;
                var rhythmPeak = rhythmPeaks[i] * rhythm_skill_multiplier;
                var staminaPeak = staminaPeaks[i] * stamina_skill_multiplier;

                var peak = norm(1.5, colourPeak, staminaPeak);
                peak = norm(2, peak, rhythmPeak);

                // Sections with 0 strain are excluded to avoid worst-case time complexity of the following sort (e.g. /b/2351871).
                // These sections will not contribute to the difficulty.
                if (peak > 0)
                    peaks.Add(peak);
            }

            double difficulty = 0;
            double weight = 1;

            foreach (var strain in peaks.OrderByDescending(d => d))
            {
                difficulty += strain * weight;
                weight *= 0.9;
            }

            return difficulty;
        }

        public override double CalculateInitialStrain(double time, DifficultyHitObject current)
        {
            var colourRaw = colour.CalculateInitialStrain(time, current);
            var rhythmRaw = colour.CalculateInitialStrain(time, current);
            var staminaRaw = colour.CalculateInitialStrain(time, current);

            var colourWeighted = colourRaw * colour_skill_multiplier;
            var rhythmWeighted = rhythmRaw * rhythm_skill_multiplier;
            var staminaWeighted = staminaRaw * stamina_skill_multiplier;

            var initialStrain = norm(1.5, colourWeighted, staminaWeighted);
            initialStrain = norm(2, initialStrain, rhythmWeighted);

            return initialStrain;
        }

        public override double StrainValueAt(DifficultyHitObject current)
        {
            var colourRaw = colour.StrainValueAt(current);
            var rhythmRaw = colour.StrainValueAt(current);
            var staminaRaw = colour.StrainValueAt(current);

            var colourWeighted = colourRaw * colour_skill_multiplier;
            var rhythmWeighted = rhythmRaw * rhythm_skill_multiplier;
            var staminaWeighted = staminaRaw * stamina_skill_multiplier;

            var currentStrain = norm(1.5, colourWeighted, staminaWeighted);
            currentStrain = norm(2, currentStrain, rhythmWeighted);

            return currentStrain;
        }
    }
}