using System.Globalization;
using MapsetVerifier.Parser.Statics;

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

        /// <summary> The settings as written, rather than limited to what the game plays. </summary>
        public Unlimited unlimited;

        public readonly record struct Unlimited(
            float HpDrain,
            float CircleSize,
            float OverallDifficulty,
            float ApproachRate
        );

        public DifficultySettings(string[] lines)
        {
            hpDrain = GetValue(lines, "HPDrainRate", 5, 0f, 10f);
            circleSize = GetValue(lines, "CircleSize", 5, 0f, 18f);
            overallDifficulty = GetValue(lines, "OverallDifficulty", 5, 0f, 10f);
            // Before file version 8, the overall difficulty was the approach rate as well.
            approachRate = GetValue(lines, "ApproachRate", overallDifficulty, 0f, 10f);

            unlimited = new Unlimited(
                GetValue(lines, "HPDrainRate") ?? hpDrain,
                GetValue(lines, "CircleSize") ?? circleSize,
                GetValue(lines, "OverallDifficulty") ?? overallDifficulty,
                GetValue(lines, "ApproachRate") ?? approachRate
            );

            sliderMultiplier = GetValue(lines, "SliderMultiplier", 1.4f, 0.4f, 3.6f);
            sliderTickRate = GetValue(lines, "SliderTickRate", 1, 0.5f, 8f);
        }

        private static float GetValue(
            string[] lines,
            string key,
            float defaultValue,
            float min,
            float max
        ) => GetValue(lines, key) is { } value ? Math.Clamp(value, min, max) : defaultValue;

        private static float? GetValue(string[] lines, string key) =>
            ParserStatic.GetValue(lines, key) is { } value
                ? float.Parse(value, CultureInfo.InvariantCulture)
                : null;

        /// <summary> Returns the radius of a circle or slider from the circle size. </summary>
        public float GetCircleRadius() => GetCircleRadius(circleSize);

        public static float GetCircleRadius(float circleSize) =>
            32.0f * (1.0f - 0.7f * (circleSize - 5) / 5);

        /// <summary>
        ///     Returns a value between the upper, middle, and lower ranges,
        ///     based on the given difficulty value (0-10).
        /// </summary>
        public static double DifficultyRange(
            double difficulty,
            (double min, double average, double max) range
        ) =>
            difficulty < 5
                ? range.average + (range.max - range.average) * (5 - difficulty) / 5
                : range.average - (range.average - range.min) * (difficulty - 5) / 5;

        /// <summary> Returns the time from where the object begins fading in to where it is fully opaque. </summary>
        public double GetFadeInTime() => DifficultyRange(approachRate, (450, 1200, 1800));

        /// <summary> Returns the time from where the object is fully opaque to where it is on the timeline. </summary>
        public double GetPreemptTime() => DifficultyRange(approachRate, (300, 800, 1200));
    }
}
