using MapsetVerifier.Parser.Objects;
using MapsetVerifier.Parser.Objects.HitObjects;
using MapsetVerifier.Parser.Objects.HitObjects.Taiko;
using MapsetVerifier.Parser.Objects.TimingLines;
using System;

using static MapsetVerifier.Checks.Utils.GeneralUtils;


namespace MapsetVerifier.Checks.Utils;

public static class TaikoUtils
    {
        /// <summary>
        /// Returns the normalized milliseconds per beat for the timing line.
        /// Normalizes the BPM using the following logic:
        /// <list type="bullet">
        /// <item><description>If the BPM is less than 110 BPM, the snap divisor is halved.</description></item>
        /// <item><description>If the BPM is greater than 270 BPM, the snap divisor is doubled.</description></item>
        /// <item><description>If the BPM is greater than 130 BPM, the snap divisor is multiplied by 1.5.</description></item>
        /// </list>
        /// </summary>
        public static double GetNormalizedMsPerBeat(this UninheritedLine line)
        {
            double result = line.msPerBeat;

            while (result <= 60000.0 / 270.0) // 270 BPM
                result *= 2;

            while (result >= (60000.0 / 110.0)) // 110 BPM
                result /= 2;

            while (result >= (60000.0 / 130.0)) // 130 BPM
                result /= 1.5;

            return result;
        }

        /// <summary>
        ///     Returns the pattern spacing in milliseconds for the hit object.
        ///     Pattern spacing represents the time gap between notes in a pattern.
        /// </summary>
        public static double GetPatternSpacingMs(this HitObject current)
        {
            var previous = current.Prev(true);
            var next = current.Next(true);

            var gapBeforeMs = current.time - (previous?.time ?? 0);
            var gapAfterMs = (next?.time ?? double.MaxValue) - current.time;

            if (current.IsNotInPattern())
            {
                return 0;
            }

            if (current.IsAtBeginningOfPattern())
            {
                return gapAfterMs;
            }

            if (current.IsAtEndOfPattern())
            {
                return gapBeforeMs;
            }

            // if in middle of pattern, pattern spacing is based on which side has the smaller snap divisor
            return Math.Min(gapBeforeMs, gapAfterMs);
        }

        /// <summary>
        ///     Returns the offset in milliseconds from the previous barline to the given time.
        /// </summary>
        public static double GetOffsetFromPrevBarlineMs(Beatmap beatmap, double time)
        {
            var timing = beatmap.GetTimingLine<UninheritedLine>(time);
            if (timing == null)
            {
                return 0;
            }
            
            var barlineGap = timing.msPerBeat * timing.Meter;

            return (time - timing.Offset) % barlineGap;
        }

        /// <summary>
        ///     Returns the offset in milliseconds from the next barline to the given time.
        /// </summary>
        public static double GetOffsetFromNextBarlineMs(Beatmap beatmap, double time)
        {
            var timing = beatmap.GetTimingLine<UninheritedLine>(time);
            if (timing == null)
            {
                return 0;
            }
            
            var barlineGap = timing.msPerBeat * timing.Meter;

            var offsetFromNextImplicitBarline = ((time - timing.Offset) % barlineGap) - barlineGap;
            var nextPotentialRedLine = beatmap.GetTimingLine<UninheritedLine>(time + offsetFromNextImplicitBarline);
            if (nextPotentialRedLine == null)
            {
                return 0;
            }

            var offsetFromNextPotentialRedline = time - nextPotentialRedLine.Offset;

            return TakeLowerAbsValue(offsetFromNextImplicitBarline, offsetFromNextPotentialRedline);
        }

        /// <summary>
        ///     Returns the offset in milliseconds from the nearest barline to the given time.
        /// </summary>
        public static double GetOffsetFromNearestBarlineMs(Beatmap beatmap, double time)
        {
            return TakeLowerAbsValue(GetOffsetFromPrevBarlineMs(beatmap, time), GetOffsetFromNextBarlineMs(beatmap, time));
        }

        /// <summary>
        ///     Returns the offset in milliseconds from the previous barline to the hit object's head (start time).
        /// </summary>
        public static double GetHeadOffsetFromPrevBarlineMs(this HitObject current) => GetOffsetFromPrevBarlineMs(current.beatmap, current.time);

        /// <summary>
        ///     Returns the offset in milliseconds from the next barline to the hit object's head (start time).
        /// </summary>
        public static double GetHeadOffsetFromNextBarlineMs(this HitObject current) => GetOffsetFromNextBarlineMs(current.beatmap, current.time);

        /// <summary>
        ///     Returns the offset in milliseconds from the nearest barline to the hit object's head (start time).
        /// </summary>
        public static double GetHeadOffsetFromNearestBarlineMs(this HitObject current) => GetOffsetFromNearestBarlineMs(current.beatmap, current.time);

        /// <summary>
        ///     Returns the offset in milliseconds from the previous barline to the hit object's tail (end time).
        /// </summary>
        public static double GetTailOffsetFromPrevBarlineMs(this HitObject current) => GetOffsetFromPrevBarlineMs(current.beatmap, current.GetEndTime());

        /// <summary>
        ///     Returns the offset in milliseconds from the next barline to the hit object's tail (end time).
        /// </summary>
        public static double GetTailOffsetFromNextBarlineMs(this HitObject current) => GetOffsetFromNextBarlineMs(current.beatmap, current.GetEndTime());

        /// <summary>
        ///     Returns the offset in milliseconds from the nearest barline to the hit object's tail (end time).
        /// </summary>
        public static double GetTailOffsetFromNearestBarlineMs(this HitObject current) => GetOffsetFromNearestBarlineMs(current.beatmap, current.GetEndTime());
    }