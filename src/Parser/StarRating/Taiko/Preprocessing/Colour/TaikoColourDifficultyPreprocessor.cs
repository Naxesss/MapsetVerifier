// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable enable
using System.Collections.Generic;
using MapsetVerifier.Parser.Objects.HitObjects;
using MapsetVerifier.Parser.Objects.HitObjects.Taiko;
using MapsetVerifier.Parser.StarRating.Preprocessing;
using MapsetVerifier.Parser.StarRating.Taiko.Preprocessing.Colour.Data;

namespace MapsetVerifier.Parser.StarRating.Taiko.Preprocessing.Colour
{
    /// <summary>
    ///     Utility class to perform various encodings.
    /// </summary>
    public static class TaikoColourDifficultyPreprocessor
    {
        /// <summary>
        ///     Processes and encodes a list of <see cref="TaikoDifficultyHitObject" />s into a list of
        ///     <see cref="TaikoDifficultyHitObjectColour" />s,
        ///     assigning the appropriate <see cref="TaikoDifficultyHitObjectColour" />s to each
        ///     <see cref="TaikoDifficultyHitObject" />.
        /// </summary>
        public static void ProcessAndAssign(List<DifficultyHitObject> hitObjects)
        {
            var hitPatterns = encode(hitObjects);

            // Assign indexing and encoding data to all relevant objects.
            foreach (var repeatingHitPattern in hitPatterns)
                // The outermost loop is kept a ForEach loop since it doesn't need index information, and we want to
                // keep i and j for AlternatingMonoPattern's and MonoStreak's index respectively, to keep it in line with
                // documentation.
                for (var i = 0; i < repeatingHitPattern.AlternatingMonoPatterns.Count; ++i)
                {
                    var monoPattern = repeatingHitPattern.AlternatingMonoPatterns[i];
                    monoPattern.Parent = repeatingHitPattern;
                    monoPattern.Index = i;

                    for (var j = 0; j < monoPattern.MonoStreaks.Count; ++j)
                    {
                        var monoStreak = monoPattern.MonoStreaks[j];
                        monoStreak.Parent = monoPattern;
                        monoStreak.Index = j;

                        foreach (var hitObject in monoStreak.HitObjects)
                        {
                            hitObject.Colour.RepeatingHitPattern = repeatingHitPattern;
                            hitObject.Colour.AlternatingMonoPattern = monoPattern;
                            hitObject.Colour.MonoStreak = monoStreak;
                        }
                    }
                }
        }

        /// <summary>
        ///     Encodes a list of <see cref="TaikoDifficultyHitObject" />s into a list of <see cref="RepeatingHitPatterns" />s.
        /// </summary>
        private static List<RepeatingHitPatterns> encode(List<DifficultyHitObject> data)
        {
            var monoStreaks = encodeMonoStreak(data);
            var alternatingMonoPatterns = encodeAlternatingMonoPattern(monoStreaks);
            var repeatingHitPatterns = encodeRepeatingHitPattern(alternatingMonoPatterns);

            return repeatingHitPatterns;
        }

        /// <summary>
        ///     Encodes a list of <see cref="TaikoDifficultyHitObject" />s into a list of <see cref="MonoStreak" />s.
        /// </summary>
        private static List<MonoStreak> encodeMonoStreak(List<DifficultyHitObject> data)
        {
            var monoStreaks = new List<MonoStreak>();
            MonoStreak? currentMonoStreak = null;

            for (var i = 0; i < data.Count; i++)
            {
                var taikoObject = (TaikoDifficultyHitObject)data[i];

                // This ignores all non-note objects, which may or may not be the desired behaviour
                var previousObject = taikoObject.PreviousNote(0);

                // If this is the first object in the list or the colour changed, create a new mono streak
                if (currentMonoStreak == null || previousObject == null || (taikoObject.BaseObject as Circle)?.IsDon() != (previousObject.BaseObject as Circle)?.IsDon())
                {
                    currentMonoStreak = new MonoStreak();
                    monoStreaks.Add(currentMonoStreak);
                }

                // Add the current object to the encoded payload.
                currentMonoStreak.HitObjects.Add(taikoObject);
            }

            return monoStreaks;
        }

        /// <summary>
        ///     Encodes a list of <see cref="MonoStreak" />s into a list of <see cref="AlternatingMonoPattern" />s.
        /// </summary>
        private static List<AlternatingMonoPattern> encodeAlternatingMonoPattern(List<MonoStreak> data)
        {
            var monoPatterns = new List<AlternatingMonoPattern>();
            AlternatingMonoPattern? currentMonoPattern = null;

            for (var i = 0; i < data.Count; i++)
            {
                // Start a new AlternatingMonoPattern if the previous MonoStreak has a different mono length, or if this is the first MonoStreak in the list.
                if (currentMonoPattern == null || data[i].RunLength != data[i - 1].RunLength)
                {
                    currentMonoPattern = new AlternatingMonoPattern();
                    monoPatterns.Add(currentMonoPattern);
                }

                // Add the current MonoStreak to the encoded payload.
                currentMonoPattern.MonoStreaks.Add(data[i]);
            }

            return monoPatterns;
        }

        /// <summary>
        ///     Encodes a list of <see cref="AlternatingMonoPattern" />s into a list of <see cref="RepeatingHitPatterns" />s.
        /// </summary>
        private static List<RepeatingHitPatterns> encodeRepeatingHitPattern(List<AlternatingMonoPattern> data)
        {
            var hitPatterns = new List<RepeatingHitPatterns>();
            RepeatingHitPatterns? currentHitPattern = null;

            for (var i = 0; i < data.Count; i++)
            {
                // Start a new RepeatingHitPattern. AlternatingMonoPatterns that should be grouped together will be handled later within this loop.
                currentHitPattern = new RepeatingHitPatterns(currentHitPattern);

                // Determine if future AlternatingMonoPatterns should be grouped.
                var isCoupled = i < data.Count - 2 && data[i].IsRepetitionOf(data[i + 2]);

                if (!isCoupled)
                {
                    // If not, add the current AlternatingMonoPattern to the encoded payload and continue.
                    currentHitPattern.AlternatingMonoPatterns.Add(data[i]);
                }
                else
                {
                    // If so, add the current AlternatingMonoPattern to the encoded payload and start repeatedly checking if the
                    // subsequent AlternatingMonoPatterns should be grouped by increasing i and doing the appropriate isCoupled check.
                    while (isCoupled)
                    {
                        currentHitPattern.AlternatingMonoPatterns.Add(data[i]);
                        i++;
                        isCoupled = i < data.Count - 2 && data[i].IsRepetitionOf(data[i + 2]);
                    }

                    // Skip over viewed data and add the rest to the payload
                    currentHitPattern.AlternatingMonoPatterns.Add(data[i]);
                    currentHitPattern.AlternatingMonoPatterns.Add(data[i + 1]);
                    i++;
                }

                hitPatterns.Add(currentHitPattern);
            }

            // Final pass to find repetition intervals
            for (var i = 0; i < hitPatterns.Count; i++) hitPatterns[i].FindRepetitionInterval();

            return hitPatterns;
        }
    }
}