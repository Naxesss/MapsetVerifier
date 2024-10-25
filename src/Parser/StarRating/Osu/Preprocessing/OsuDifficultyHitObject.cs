// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Numerics;
using MapsetVerifier.Parser.Objects;
using MapsetVerifier.Parser.Objects.HitObjects;
using MapsetVerifier.Parser.StarRating.Preprocessing;

namespace MapsetVerifier.Parser.StarRating.Osu.Preprocessing
{
    public class OsuDifficultyHitObject : DifficultyHitObject
    {
        private const int normalized_radius = 52;

        private readonly HitObject lastLastObject;
        private readonly HitObject lastObject;

        /// <summary>
        ///     Milliseconds elapsed since the start time of the previous <see cref="OsuDifficultyHitObject" />, with a minimum of
        ///     50ms.
        /// </summary>
        public readonly double StrainTime;

        public OsuDifficultyHitObject(HitObject hitObject, HitObject lastLastObject, HitObject lastObject) : base(hitObject, lastObject, null, 0)
        {
            this.lastLastObject = lastLastObject;
            this.lastObject = lastObject;

            SetDistances();

            // Every strain interval is hard capped at the equivalent of 375 BPM streaming speed as a safety measure
            StrainTime = Math.Max(50, DeltaTime);
        }

        protected new HitObject BaseObject => base.BaseObject;

        /// <summary>
        ///     Normalized distance from the end position of the previous <see cref="OsuDifficultyHitObject" /> to the start
        ///     position of this <see cref="OsuDifficultyHitObject" />.
        /// </summary>
        public double JumpDistance { get; private set; }

        /// <summary>
        ///     Normalized distance between the start and end position of the previous <see cref="OsuDifficultyHitObject" />.
        /// </summary>
        public double TravelDistance { get; private set; }

        /// <summary>
        ///     Angle the player has to take to hit this <see cref="OsuDifficultyHitObject" />.
        ///     Calculated as the angle between the circles (current-2, current-1, current).
        /// </summary>
        public double? Angle { get; private set; }

        private void SetDistances()
        {
            double radius = BaseObject.beatmap.DifficultySettings.GetCircleRadius();

            // We will scale distances by this factor, so we can assume a uniform CircleSize among beatmaps.
            var scalingFactor = normalized_radius / (float)radius;

            if (radius < 30)
            {
                var smallCircleBonus = Math.Min(30 - (float)radius, 5) / 50;
                scalingFactor *= 1 + smallCircleBonus;
            }

            if (lastObject is Slider lastSlider)
            {
                ComputeSliderCursorPosition(lastSlider);
                TravelDistance = lastSlider.LazyTravelDistance * scalingFactor;
            }

            var lastCursorPosition = GetEndCursorPosition(lastObject);

            // Don't need to jump to reach spinners
            if (!(BaseObject is Spinner))
                JumpDistance = (BaseObject.Position * scalingFactor - lastCursorPosition * scalingFactor).Length();

            if (lastLastObject != null)
            {
                var lastLastCursorPosition = GetEndCursorPosition(lastLastObject);

                var v1 = lastLastCursorPosition - lastObject.Position;
                var v2 = BaseObject.Position - lastCursorPosition;

                var dot = Vector2.Dot(v1, v2);
                var det = v1.X * v2.Y - v1.Y * v2.X;

                Angle = Math.Abs(Math.Atan2(det, dot));
            }
        }

        private void ComputeSliderCursorPosition(Slider slider)
        {
            if (slider.LazyEndPosition != null)
                return;

            slider.LazyEndPosition = slider.Position;

            var approxFollowCircleRadius = slider.beatmap.DifficultySettings.circleSize * 3;

            var computeVertex = new Action<double>(t =>
            {
                // ReSharper disable once PossibleInvalidOperationException (bugged in current r# version)
                var diff = slider.Position + slider.GetPathPosition(t) - slider.LazyEndPosition;
                var dist = diff.Length();

                if (dist > approxFollowCircleRadius)
                {
                    // The cursor would be outside the follow circle, we need to move it
                    diff = Vector2.Normalize(diff); // Obtain direction of diff
                    dist -= approxFollowCircleRadius;
                    slider.LazyEndPosition += diff * dist;
                    slider.LazyTravelDistance += dist;
                }
            });

            // Skip the head circle
            foreach (var time in slider.GetSliderTickTimes())
                computeVertex(time);

            computeVertex(slider.EndTime);
        }

        private Vector2 GetEndCursorPosition(HitObject hitObject)
        {
            var pos = hitObject.Position;

            if (hitObject is Slider slider)
            {
                ComputeSliderCursorPosition(slider);
                pos = slider.LazyEndPosition;
            }

            return pos;
        }
    }
}