namespace MapsetVerifier.Parser.Objects.HitObjects.Taiko
{
    public static class TaikoExtensions
    {
        /// <summary>
        ///     Returns whether the hit object is a don hit.
        /// </summary>
        public static bool IsDon(this HitObject hitObject) => !hitObject.HasHitSound(HitObject.HitSounds.Whistle) && !hitObject.HasHitSound(HitObject.HitSounds.Clap);

        /// <summary>
        ///     Returns whether the hit object is a finisher hit.
        /// </summary>
        public static bool IsFinisher(this HitObject hitObject) => hitObject.HasHitSound(HitObject.HitSounds.Finish);

        /// <summary>
        ///     Returns whether the hit object is the same color as the previous hit object.
        /// </summary>
        public static bool IsMono(this HitObject hitObject) => (hitObject.Prev()?.IsDon() ?? null) == hitObject.IsDon();

        /// <summary>
        ///     Returns whether the hit object is at the beginning of a pattern.
        ///     A pattern is defined by consecutive circles with similar spacing.
        /// </summary>
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

        /// <summary>
        ///     Returns whether the hit object is at the end of a pattern.
        ///     A pattern is defined by consecutive circles with similar spacing.
        /// </summary>
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

        /// <summary>
        ///     Returns whether the hit object is in the middle of a pattern.
        ///     A pattern is defined by consecutive circles with similar spacing.
        /// </summary>
        public static bool IsInMiddleOfPattern(this HitObject current)
        {
            return !current.IsAtBeginningOfPattern() && !current.IsAtEndOfPattern();
        }

        /// <summary>
        ///     Returns whether the hit object is not part of a pattern (i.e., it's both at the beginning and end of a pattern).
        ///     A pattern is defined by consecutive circles with similar spacing.
        /// </summary>
        public static bool IsNotInPattern(this HitObject current)
        {
            return current.IsAtBeginningOfPattern() && current.IsAtEndOfPattern();
        }
    }
}