// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using MapsetVerifier.Parser.StarRating.Skills;

namespace MapsetVerifier.Parser.StarRating
{
    public class DifficultyAttributes
    {
        public int MaxCombo;
        public Skill[] Skills;

        public double StarRating;

        public DifficultyAttributes() { }

        public DifficultyAttributes(Skill[] skills, double starRating)
        {
            Skills = skills;
            StarRating = starRating;
        }
    }
}