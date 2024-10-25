// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using MapsetVerifier.Parser.Objects;
using MapsetVerifier.Parser.Objects.HitObjects;
using MapsetVerifier.Parser.Scoring;
using MapsetVerifier.Parser.StarRating.Osu.Preprocessing;
using MapsetVerifier.Parser.StarRating.Osu.Scoring;
using MapsetVerifier.Parser.StarRating.Osu.Skills;
using MapsetVerifier.Parser.StarRating.Preprocessing;
using MapsetVerifier.Parser.StarRating.Skills;

namespace MapsetVerifier.Parser.StarRating.Osu
{
    public class OsuDifficultyCalculator : DifficultyCalculator
    {
        private const double difficulty_multiplier = 0.0675;

        public OsuDifficultyCalculator(Beatmap beatmap) : base(beatmap) { }

        protected override DifficultyAttributes CreateDifficultyAttributes(Beatmap beatmap, Skill[] skills)
        {
            if (beatmap.HitObjects.Count == 0)
                return new OsuDifficultyAttributes { Skills = skills };

            var aimRating = Math.Sqrt(skills[0].DifficultyValue()) * difficulty_multiplier;
            var speedRating = Math.Sqrt(skills[1].DifficultyValue()) * difficulty_multiplier;
            var starRating = aimRating + speedRating + Math.Abs(aimRating - speedRating) / 2;

            HitWindows hitWindows = new OsuHitWindows();
            hitWindows.SetDifficulty(beatmap.DifficultySettings.overallDifficulty);

            // Todo: These int casts are temporary to achieve 1:1 results with osu!stable, and should be removed in the future
            double hitWindowGreat = (int)hitWindows.WindowFor(HitResult.Great);
            double preempt = (int)beatmap.DifficultySettings.GetPreemptTime();

            var maxCombo = beatmap.HitObjects.Count;
            maxCombo += beatmap.HitObjects.OfType<Slider>().Sum(s => s.GetSliderTickTimes().Count);

            var hitCirclesCount = beatmap.HitObjects.Count(h => h is Circle);

            return new OsuDifficultyAttributes
            {
                StarRating = starRating,
                AimStrain = aimRating,
                SpeedStrain = speedRating,
                ApproachRate = preempt > 1200 ? (1800 - preempt) / 120 : (1200 - preempt) / 150 + 5,
                OverallDifficulty = (80 - hitWindowGreat) / 6,
                MaxCombo = maxCombo,
                HitCircleCount = hitCirclesCount,
                Skills = skills
            };
        }

        protected override IEnumerable<DifficultyHitObject> CreateDifficultyHitObjects(Beatmap beatmap)
        {
            // The first jump is formed by the first two hitobjects of the map.
            // If the map has less than two OsuHitObjects, the enumerator will not return anything.
            for (var i = 1; i < beatmap.HitObjects.Count; i++)
            {
                var lastLast = i > 1 ? beatmap.HitObjects[i - 2] : null;
                var last = beatmap.HitObjects[i - 1];
                var current = beatmap.HitObjects[i];

                yield return new OsuDifficultyHitObject(current, lastLast, last);
            }
        }

        protected override Skill[] CreateSkills(Beatmap beatmap) =>
            new Skill[]
            {
                new Aim(),
                new Speed()
            };
    }
}