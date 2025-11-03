using System;
using System.Globalization;
using System.Linq;

namespace MapsetVerifier.Parser.Settings
{
    public class DifficultySettings
    {
        public float approachRate;

        public float circleSize;
        /*
            HPDrainRate:6
            CircleSize:4.2
            OverallDifficulty:9
            ApproachRate:9.7
            SliderMultiplier:2.5
            SliderTickRate:2
         */
        // key:value

        public float hpDrain;
        public float overallDifficulty;

        public float sliderMultiplier;
        public float sliderTickRate;

        public DifficultySettings(string[] lines)
        {
            hpDrain = GetValue(lines, "HPDrainRate", 0f, 10f);
            circleSize = GetValue(lines, "CircleSize", 0f, 18f);
            overallDifficulty = GetValue(lines, "OverallDifficulty", 0f, 10f);
            approachRate = GetValue(lines, "ApproachRate", 0f, 10f);

            sliderMultiplier = GetValue(lines, "SliderMultiplier", 0.4f, 3.6f);
            sliderTickRate = GetValue(lines, "SliderTickRate", 0.5f, 8f);
        }

        private float GetValue(string[] lines, string key, float? min = null, float? max = null)
        {
            var line = lines.FirstOrDefault(otherLine => otherLine.StartsWith(key));

            if (line == null)
                return 0;

            var value = float.Parse(line.Substring(line.IndexOf(":", StringComparison.Ordinal) + 1).Trim(), CultureInfo.InvariantCulture);

            if (value < min) value = min.GetValueOrDefault();
            if (value > max) value = max.GetValueOrDefault();

            return value;
        }

        /// <summary> Returns the radius of a circle or slider from the circle size. </summary>
        public float GetCircleRadius() => 32.0f * (1.0f - 0.7f * (circleSize - 5) / 5);

        /// <summary>
        ///     Returns a value between the upper, middle, and lower ranges,
        ///     based on the given difficulty value (0-10).
        /// </summary>
        public static double DifficultyRange(double difficulty, (double min, double average, double max) range) =>
            difficulty < 5 ? range.average + (range.max - range.average) * (5 - difficulty) / 5 : range.average - (range.average - range.min) * (difficulty - 5) / 5;

        /// <summary> Returns the time from where the object begins fading in to where it is fully opaque. </summary>
        public double GetFadeInTime() => DifficultyRange(approachRate, (450, 1200, 1800));

        /// <summary> Returns the time from where the object is fully opaque to where it is on the timeline. </summary>
        public double GetPreemptTime() => DifficultyRange(approachRate, (300, 800, 1200));
    }
}