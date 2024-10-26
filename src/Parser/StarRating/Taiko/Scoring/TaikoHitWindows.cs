// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using MapsetVerifier.Parser.Scoring;

namespace MapsetVerifier.Parser.StarRating.Taiko.Scoring
{
    public class TaikoHitWindows : HitWindows
    {
        private static readonly DifficultyRange[] taiko_ranges =
        {
            new(HitResult.Great, 50, 35, 20),
            new(HitResult.Ok, 120, 80, 50),
            new(HitResult.Miss, 135, 95, 70)
        };

        public override bool IsHitResultAllowed(HitResult result)
        {
            switch (result)
            {
                case HitResult.Great:
                case HitResult.Ok:
                case HitResult.Miss:
                    return true;
            }

            return false;
        }

        protected override DifficultyRange[] GetRanges() => taiko_ranges;
    }
}