// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using MapsetVerifier.Parser.Objects;
using MapsetVerifier.Parser.Objects.HitObjects;
using MapsetVerifier.Parser.Scoring;
using MapsetVerifier.Parser.StarRating.Preprocessing;
using MapsetVerifier.Parser.StarRating.Skills;
using MapsetVerifier.Parser.StarRating.Taiko.Preprocessing;
using MapsetVerifier.Parser.StarRating.Taiko.Preprocessing.Colour;
using MapsetVerifier.Parser.StarRating.Taiko.Scoring;
using MapsetVerifier.Parser.StarRating.Taiko.Skills;

namespace MapsetVerifier.Parser.StarRating.Taiko
{
    public class TaikoDifficultyCalculator : DifficultyCalculator
    {
        private const double difficulty_multiplier = 1.35;

        public TaikoDifficultyCalculator(Beatmap beatmap) : base(beatmap) { }

        protected override Skill[] CreateSkills(Beatmap beatmap) =>
        [
            new Peaks()
        ];

        protected override IEnumerable<DifficultyHitObject> CreateDifficultyHitObjects(Beatmap beatmap)
        {
            var difficultyHitObjects = new List<DifficultyHitObject>();
            var centreObjects = new List<TaikoDifficultyHitObject>();
            var rimObjects = new List<TaikoDifficultyHitObject>();
            var noteObjects = new List<TaikoDifficultyHitObject>();

            for (var i = 2; i < beatmap.HitObjects.Count; i++)
                difficultyHitObjects.Add(new TaikoDifficultyHitObject(beatmap.HitObjects[i], beatmap.HitObjects[i - 1], beatmap.HitObjects[i - 2], difficultyHitObjects, centreObjects, rimObjects, noteObjects, difficultyHitObjects.Count));

            TaikoColourDifficultyPreprocessor.ProcessAndAssign(difficultyHitObjects);

            return difficultyHitObjects;
        }

        protected override DifficultyAttributes CreateDifficultyAttributes(Beatmap beatmap, Skill[] skills)
        {
            if (beatmap.HitObjects.Count == 0)
                return new TaikoDifficultyAttributes();

            var combined = (Peaks)skills[0];

            var colourRating = combined.ColourDifficultyValue * difficulty_multiplier;
            var rhythmRating = combined.RhythmDifficultyValue * difficulty_multiplier;
            var staminaRating = combined.StaminaDifficultyValue * difficulty_multiplier;

            var combinedRating = combined.DifficultyValue() * difficulty_multiplier;
            var starRating = rescale(combinedRating * 1.4);

            HitWindows hitWindows = new TaikoHitWindows();
            hitWindows.SetDifficulty(beatmap.DifficultySettings.overallDifficulty);

            var attributes = new TaikoDifficultyAttributes
            {
                StarRating = starRating,
                StaminaDifficulty = staminaRating,
                RhythmDifficulty = rhythmRating,
                ColourDifficulty = colourRating,
                PeakDifficulty = combinedRating,
                GreatHitWindow = hitWindows.WindowFor(HitResult.Great),
                MaxCombo = beatmap.HitObjects.Count(h => h is Circle),
                Skills = skills
            };

            return attributes;
        }

        /// <summary>
        ///     Applies a final re-scaling of the star rating.
        /// </summary>
        /// <param name="sr">The raw star rating value before re-scaling.</param>
        private double rescale(double sr)
        {
            if (sr < 0) return sr;

            return 10.43 * Math.Log(sr / 8 + 1);
        }
    }
}