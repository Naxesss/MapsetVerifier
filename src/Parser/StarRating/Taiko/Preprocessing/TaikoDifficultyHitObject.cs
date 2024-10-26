// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using MapsetVerifier.Parser.Objects;
using MapsetVerifier.Parser.Objects.HitObjects;
using MapsetVerifier.Parser.Objects.HitObjects.Taiko;
using MapsetVerifier.Parser.StarRating.Preprocessing;
using MapsetVerifier.Parser.StarRating.Taiko.Preprocessing.Colour;
using MapsetVerifier.Parser.StarRating.Taiko.Preprocessing.Rhythm;

namespace MapsetVerifier.Parser.StarRating.Taiko.Preprocessing
{
    /// <summary>
    ///     Represents a single hit object in taiko difficulty calculation.
    /// </summary>
    public class TaikoDifficultyHitObject : DifficultyHitObject
    {
        /// <summary>
        ///     List of most common rhythm changes in taiko maps.
        /// </summary>
        /// <remarks>
        ///     The general guidelines for the values are:
        ///     <list type="bullet">
        ///         <item>rhythm changes with ratio closer to 1 (that are <i>not</i> 1) are harder to play,</item>
        ///         <item>
        ///             speeding up is <i>generally</i> harder than slowing down (with exceptions of rhythm changes requiring a
        ///             hand switch).
        ///         </item>
        ///     </list>
        /// </remarks>
        private static readonly TaikoDifficultyHitObjectRhythm[] common_rhythms =
        [
            new(1, 1, 0.0),
            new(2, 1, 0.3),
            new(1, 2, 0.5),
            new(3, 1, 0.3),
            new(1, 3, 0.35),
            new(3, 2, 0.6), // purposefully higher (requires hand switch in full alternating gameplay style)
            new(2, 3, 0.4),
            new(5, 4, 0.5),
            new(4, 5, 0.7)
        ];

        /// <summary>
        ///     Colour data for this hit object. This is used by colour evaluator to calculate colour difficulty, but can be used
        ///     by other skills in the future.
        /// </summary>
        public readonly TaikoDifficultyHitObjectColour Colour;

        /// <summary>
        ///     The list of all <see cref="TaikoDifficultyHitObject" /> of the same colour as this
        ///     <see cref="TaikoDifficultyHitObject" /> in the beatmap.
        /// </summary>
        private readonly IReadOnlyList<TaikoDifficultyHitObject>? monoDifficultyHitObjects;

        /// <summary>
        ///     The index of this <see cref="TaikoDifficultyHitObject" /> in <see cref="monoDifficultyHitObjects" />.
        /// </summary>
        public readonly int MonoIndex;

        /// <summary>
        ///     The list of all <see cref="TaikoDifficultyHitObject" /> that is either a regular note or finisher in the beatmap
        /// </summary>
        private readonly IReadOnlyList<TaikoDifficultyHitObject> noteDifficultyHitObjects;

        /// <summary>
        ///     The index of this <see cref="TaikoDifficultyHitObject" /> in <see cref="noteDifficultyHitObjects" />.
        /// </summary>
        public readonly int NoteIndex;

        /// <summary>
        ///     The rhythm required to hit this hit object.
        /// </summary>
        public readonly TaikoDifficultyHitObjectRhythm Rhythm;

        /// <summary>
        ///     Creates a new difficulty hit object.
        /// </summary>
        /// <param name="hitObject">The gameplay <see cref="HitObject" /> associated with this difficulty object.</param>
        /// <param name="lastObject">The gameplay <see cref="HitObject" /> preceding <paramref name="hitObject" />.</param>
        /// <param name="lastLastObject">The gameplay <see cref="HitObject" /> preceding <paramref name="lastObject" />.</param>
        /// <param name="objects">The list of all <see cref="DifficultyHitObject" />s in the current beatmap.</param>
        /// <param name="centreHitObjects">The list of centre (don) <see cref="DifficultyHitObject" />s in the current beatmap.</param>
        /// <param name="rimHitObjects">The list of rim (kat) <see cref="DifficultyHitObject" />s in the current beatmap.</param>
        /// <param name="noteObjects">
        ///     The list of <see cref="DifficultyHitObject" />s that is a hit (i.e. not a drumroll or swell)
        ///     in the current beatmap.
        /// </param>
        /// <param name="index">The position of this <see cref="DifficultyHitObject" /> in the <paramref name="objects" /> list.</param>
        public TaikoDifficultyHitObject(HitObject hitObject, HitObject lastObject, HitObject lastLastObject, List<DifficultyHitObject> objects, List<TaikoDifficultyHitObject> centreHitObjects, List<TaikoDifficultyHitObject> rimHitObjects, List<TaikoDifficultyHitObject> noteObjects, int index) : base(hitObject, lastObject, objects, index)
        {
            noteDifficultyHitObjects = noteObjects;

            // Create the Colour object, its properties should be filled in by TaikoDifficultyPreprocessor
            Colour = new TaikoDifficultyHitObjectColour();
            Rhythm = getClosestRhythm(lastObject, lastLastObject);

            if (hitObject is Circle circle)
            {
                if (circle.IsDon())
                {
                    MonoIndex = centreHitObjects.Count;
                    centreHitObjects.Add(this);
                    monoDifficultyHitObjects = centreHitObjects;
                }
                else
                {
                    MonoIndex = rimHitObjects.Count;
                    rimHitObjects.Add(this);
                    monoDifficultyHitObjects = rimHitObjects;
                }

                NoteIndex = noteObjects.Count;
                noteObjects.Add(this);
            }
        }

        /// <summary>
        ///     Returns the closest rhythm change from <see cref="common_rhythms" /> required to hit this object.
        /// </summary>
        /// <param name="lastObject">The gameplay <see cref="HitObject" /> preceding this one.</param>
        /// <param name="lastLastObject">The gameplay <see cref="HitObject" /> preceding <paramref name="lastObject" />.</param>
        private TaikoDifficultyHitObjectRhythm getClosestRhythm(HitObject lastObject, HitObject lastLastObject)
        {
            var prevLength = lastObject.time - lastLastObject.time;
            var ratio = DeltaTime / prevLength;

            return common_rhythms.OrderBy(x => Math.Abs(x.Ratio - ratio)).First();
        }

        public TaikoDifficultyHitObject? PreviousMono(int backwardsIndex) => monoDifficultyHitObjects?.ElementAtOrDefault(MonoIndex - (backwardsIndex + 1));

        public TaikoDifficultyHitObject? NextMono(int forwardsIndex) => monoDifficultyHitObjects?.ElementAtOrDefault(MonoIndex + forwardsIndex + 1);

        public TaikoDifficultyHitObject? PreviousNote(int backwardsIndex) => noteDifficultyHitObjects.ElementAtOrDefault(NoteIndex - (backwardsIndex + 1));

        public TaikoDifficultyHitObject? NextNote(int forwardsIndex) => noteDifficultyHitObjects.ElementAtOrDefault(NoteIndex + forwardsIndex + 1);
    }
}