// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using MapsetVerifier.Parser.StarRating.Preprocessing;

namespace MapsetVerifier.Parser.StarRating.Skills
{
    /// <summary>
    ///     A bare minimal abstract skill for fully custom skill implementations.
    /// </summary>
    /// <remarks>
    ///     This class should be considered a "processing" class and not persisted.
    /// </remarks>
    public abstract class Skill
    {
        /// <summary>
        ///     Process a <see cref="DifficultyHitObject" />.
        /// </summary>
        /// <param name="current">The <see cref="DifficultyHitObject" /> to process.</param>
        public abstract void Process(DifficultyHitObject current);

        public abstract string SkillName();

        public override string ToString() => SkillName();

        public override bool Equals(object? obj)
        {
            if (obj is not Skill skill)
                return false;

            return skill.SkillName() == SkillName();
        }

        public override int GetHashCode() => SkillName().GetHashCode();

        /// <summary>
        ///     Returns the calculated difficulty value representing all <see cref="DifficultyHitObject" />s that have been
        ///     processed up to this point.
        /// </summary>
        public abstract double DifficultyValue();
    }
}