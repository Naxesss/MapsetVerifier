// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using MapsetVerifier.Parser.Objects.HitObjects;
using MapsetVerifier.Parser.Objects.HitObjects.Taiko;
using MapsetVerifier.Parser.StarRating.Preprocessing;

namespace MapsetVerifier.Parser.StarRating.Taiko.Preprocessing.Colour.Data
{
    /// <summary>
    ///     Encode colour information for a sequence of <see cref="TaikoDifficultyHitObject" />s. Consecutive
    ///     <see cref="TaikoDifficultyHitObject" />s
    ///     of the same colour are encoded within the same <see cref="MonoStreak" />.
    /// </summary>
    public class MonoStreak
    {
        /// <summary>
        ///     Index of this <see cref="MonoStreak" /> within it's parent <see cref="AlternatingMonoPattern" />
        /// </summary>
        public int Index;

        /// <summary>
        ///     The parent <see cref="AlternatingMonoPattern" /> that contains this <see cref="MonoStreak" />
        /// </summary>
        public AlternatingMonoPattern Parent = null!;

        /// <summary>
        ///     List of <see cref="DifficultyHitObject" />s that are encoded within this <see cref="MonoStreak" />.
        /// </summary>
        public List<TaikoDifficultyHitObject> HitObjects { get; } = new List<TaikoDifficultyHitObject>();

        /// <summary>
        ///     The first <see cref="TaikoDifficultyHitObject" /> in this <see cref="MonoStreak" />.
        /// </summary>
        public TaikoDifficultyHitObject FirstHitObject => HitObjects[0];

        /// <summary>
        ///     The last <see cref="TaikoDifficultyHitObject" /> in this <see cref="MonoStreak" />.
        /// </summary>
        public TaikoDifficultyHitObject LastHitObject => HitObjects[^1];

        /// <summary>
        ///     Whether all objects encoded within this <see cref="MonoStreak" /> are circles.
        /// </summary>
        public bool AreCircles => HitObjects[0].BaseObject is Circle;

        /// <summary>
        ///     Whether all objects encoded within this <see cref="MonoStreak" /> are don hits.
        ///     Returns false if not a circle.
        /// </summary>
        public bool AreDons => (HitObjects[0].BaseObject as Circle)?.IsDon() ?? false;

        /// <summary>
        ///     How long the mono pattern encoded within is
        /// </summary>
        public int RunLength => HitObjects.Count;
    }
}