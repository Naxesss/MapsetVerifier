// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using MapsetVerifier.Parser.Objects;
using MapsetVerifier.Parser.StarRating.Preprocessing;
using MapsetVerifier.Parser.StarRating.Skills;

namespace MapsetVerifier.Parser.StarRating
{
    public abstract class DifficultyCalculator
    {
        private readonly Beatmap beatmap;

        protected DifficultyCalculator(Beatmap beatmap) => this.beatmap = beatmap;

        /// <summary>
        ///     The length of each strain section.
        /// </summary>
        protected virtual int SectionLength => 400;

        /// <summary>
        ///     Calculates the difficulty of the beatmap.
        /// </summary>
        /// <returns>A structure describing the difficulty of the beatmap.</returns>
        public DifficultyAttributes Calculate() => Calculate(beatmap);

        private DifficultyAttributes Calculate(Beatmap beatmap)
        {
            var skills = CreateSkills(beatmap);

            if (!beatmap.HitObjects.Any())
                return CreateDifficultyAttributes(beatmap, skills);

            var difficultyHitObjects = SortObjects(CreateDifficultyHitObjects(beatmap)).ToList();

            double sectionLength = SectionLength;

            // The first object doesn't generate a strain, so we begin with an incremented section end
            var currentSectionEnd = Math.Ceiling(beatmap.HitObjects.First().time / sectionLength) * sectionLength;

            foreach (var h in difficultyHitObjects)
            {
                while (h.BaseObject.time > currentSectionEnd)
                {
                    foreach (var s in skills)
                    {
                        (s as StrainSkill)?.saveCurrentPeak();
                        (s as StrainSkill)?.startNewSectionFrom(currentSectionEnd, h);
                    }

                    currentSectionEnd += sectionLength;
                }

                foreach (var s in skills)
                    s.Process(h);
            }

            // The peak strain will not be saved for the last section in the above loop
            foreach (var s in skills)
                (s as StrainSkill)?.saveCurrentPeak();

            return CreateDifficultyAttributes(beatmap, skills);
        }

        /// <summary>
        ///     Sorts a given set of <see cref="DifficultyHitObject" />s.
        /// </summary>
        /// <param name="input">The <see cref="DifficultyHitObject" />s to sort.</param>
        /// <returns>The sorted <see cref="DifficultyHitObject" />s.</returns>
        protected virtual IEnumerable<DifficultyHitObject> SortObjects(IEnumerable<DifficultyHitObject> input) => input.OrderBy(h => h.BaseObject.time);

        /// <summary>
        ///     Creates <see cref="DifficultyAttributes" /> to describe beatmap's calculated difficulty.
        /// </summary>
        /// <param name="beatmap">The <see cref="Beatmap" /> whose difficulty was calculated.</param>
        /// <param name="skills">The skills which processed the beatmap.</param>
        protected abstract DifficultyAttributes CreateDifficultyAttributes(Beatmap beatmap, Skill[] skills);

        /// <summary>
        ///     Enumerates <see cref="DifficultyHitObject" />s to be processed from <see cref="HitObject" />s in the
        ///     <see cref="Beatmap" />.
        /// </summary>
        /// <param name="beatmap">The <see cref="Beatmap" /> providing the <see cref="HitObject" />s to enumerate.</param>
        /// <returns>The enumerated <see cref="DifficultyHitObject" />s.</returns>
        protected abstract IEnumerable<DifficultyHitObject> CreateDifficultyHitObjects(Beatmap beatmap);

        /// <summary>
        ///     Creates the <see cref="Skill" />s to calculate the difficulty of an <see cref="Beatmap" />.
        /// </summary>
        /// <param name="beatmap">The <see cref="Beatmap" /> whose difficulty will be calculated.</param>
        /// <returns>The <see cref="Skill" />s.</returns>
        protected abstract Skill[] CreateSkills(Beatmap beatmap);
    }
}