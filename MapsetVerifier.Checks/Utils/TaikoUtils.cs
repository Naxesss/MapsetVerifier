using MapsetVerifier.Parser.Objects;
using MapsetVerifier.Parser.Objects.HitObjects;
using MapsetVerifier.Parser.Objects.TimingLines;
using System;

using static MapsetVerifier.Checks.Utils.GeneralUtils;


namespace MapsetVerifier.Checks.Utils;

public static class TaikoUtils
    {
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
        
        public static List<TimingLine> FindKiaiToggles(this List<TimingLine> timingLines)
        {
            List<TimingLine> kiaiToggles = new List<TimingLine>();

            TimingLine previousTimingLine = null;

            foreach (TimingLine line in timingLines)
            {
                if ((previousTimingLine == null && line.Kiai) || (previousTimingLine != null && previousTimingLine.Kiai != line.Kiai))
                {
                    kiaiToggles.Add(line);
                }
                previousTimingLine = line;
            }
            return kiaiToggles;
        }

        public static List<TimingLine> FindKiaiToggles(this Beatmap beatmap) => beatmap.TimingLines.FindKiaiToggles();
        
        public static List<TimingLine> FindSvChanges(this List<TimingLine> timingLines)
        {
            List<TimingLine> svChanges = new List<TimingLine>();
            
            for (int i=0; i<timingLines.Count-1; i++)
            {
                TimingLine firstLine = timingLines[i];
                TimingLine secondLine = timingLines[i+1];

                if (firstLine.SvMult != secondLine.SvMult && firstLine.Offset != secondLine.Offset)
                {
                    svChanges.Add(secondLine);
                }
            }

            return svChanges;
        }

        public static List<TimingLine> FindSvChanges(this Beatmap beatmap) => beatmap.TimingLines.FindSvChanges();
        
        public static bool IsDon(this HitObject hitObject)
        {
            if (hitObject is not Circle)
            {
                return false;
            }
            return !hitObject.HasHitSound(HitObject.HitSounds.Whistle) && !hitObject.HasHitSound(HitObject.HitSounds.Clap);
        }
        
        public static bool IsFinisher(this HitObject hitObject)
        {
            return hitObject.HasHitSound(HitObject.HitSounds.Finish);
        }

        public static bool IsMono(this HitObject hitObject)
        {
            return (hitObject.Prev()?.IsDon() ?? null) == hitObject.IsDon();
        }

        public static bool IsAtBeginningOfPattern(this HitObject current)
        {
            var previous = current.Prev(true);
            var next = current.Next(true);

            // if there aren't circles immediately before this object, then this is the start of a pattern
            if (previous == null || previous is not Circle)
            {
                return true;
            }

            // if there are circles immediately before but not after this object, then this is not the start of a pattern
            if (next == null || next is not Circle)
            {
                return false;
            }

            var gapBeforeMs = current.time - previous.time;
            var gapAfterMs = next.time - current.time;

            // if there are circles both immediately before and after this object, then it's the start if the snap divisor after this note is lower than before it
            return gapAfterMs < gapBeforeMs;
        }

        public static bool IsAtEndOfPattern(this HitObject current)
        {
            var previous = current.Prev(true);
            var next = current.Next(true);

            // if there aren't circles immediately after this object, then this is the end of a pattern
            if (next == null || next is not Circle)
            {
                return true;
            }

            // if there are circles immediately after but not before this object, then this is not the end of a pattern
            if (previous == null || previous is not Circle)
            {
                return false;
            }

            var gapBeforeMs = current.time - previous.time;
            var gapAfterMs = next.time - current.time;

            // if there are circles both immediately before and after this object, then it's the end if the snap divisor before this note is lower than after it
            return gapBeforeMs < gapAfterMs;
        }

        public static bool IsInMiddleOfPattern(this HitObject current)
        {
            return !current.IsAtBeginningOfPattern() && !current.IsAtEndOfPattern();
        }

        public static bool IsNotInPattern(this HitObject current)
        {
            return current.IsAtBeginningOfPattern() && current.IsAtEndOfPattern();
        }

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

        public static double GetOffsetFromPrevBarlineMs(Beatmap beatmap, double time)
        {
            var timing = beatmap.GetTimingLine<UninheritedLine>(time);
            var barlineGap = timing.msPerBeat * timing.Meter;

            return (time - timing.Offset) % barlineGap;
        }

        public static double GetOffsetFromNextBarlineMs(Beatmap beatmap, double time)
        {
            var timing = beatmap.GetTimingLine<UninheritedLine>(time);
            var barlineGap = timing.msPerBeat * timing.Meter;

            var offsetFromNextImplicitBarline = ((time - timing.Offset) % barlineGap) - barlineGap;
            var nextPotentialRedLine = beatmap.GetTimingLine<UninheritedLine>(time + offsetFromNextImplicitBarline);
            var offsetFromNextPotentialRedline = time - nextPotentialRedLine.Offset;

            return TakeLowerAbsValue(offsetFromNextImplicitBarline, offsetFromNextPotentialRedline);
        }

        public static double GetOffsetFromNearestBarlineMs(Beatmap beatmap, double time)
        {
            return TakeLowerAbsValue(GetOffsetFromPrevBarlineMs(beatmap, time), GetOffsetFromNextBarlineMs(beatmap, time));
        }

        public static double GetHeadOffsetFromPrevBarlineMs(this HitObject current) => GetOffsetFromPrevBarlineMs(current.beatmap, current.time);

        public static double GetHeadOffsetFromNextBarlineMs(this HitObject current) => GetOffsetFromNextBarlineMs(current.beatmap, current.time);

        public static double GetHeadOffsetFromNearestBarlineMs(this HitObject current) => GetOffsetFromNearestBarlineMs(current.beatmap, current.time);

        public static double GetTailOffsetFromPrevBarlineMs(this HitObject current) => GetOffsetFromPrevBarlineMs(current.beatmap, current.GetEndTime());

        public static double GetTailOffsetFromNextBarlineMs(this HitObject current) => GetOffsetFromNextBarlineMs(current.beatmap, current.GetEndTime());

        public static double GetTailOffsetFromNearestBarlineMs(this HitObject current) => GetOffsetFromNearestBarlineMs(current.beatmap, current.GetEndTime());

        public static bool IsBottomDiffKantan(this BeatmapSet beatmapSet)
        {
            foreach (var beatmap in beatmapSet.Beatmaps)
            {
                if (beatmap.GetDifficulty() == Beatmap.Difficulty.Easy)
                {
                    return true;
                }
            }
            return false;
        }
    }