using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Numerics;
using MapsetVerifier.Parser.Objects;
using MapsetVerifier.Parser.StarRating.Skills;
using MapsetVerifier.Rendering.Objects;

namespace MapsetVerifier.Rendering
{
    public abstract class SkillChartRenderer : ChartRenderer
    {
        private const int MS_PER_PEAK = 400;

        /// <summary>
        ///     Used to keep track of the amount of times a specific difficulty is drawn,
        ///     such that the color of all series in a chart are unique.
        /// </summary>
        private static readonly Dictionary<LineChart, List<Beatmap>> mapsUsedInChart = new Dictionary<LineChart, List<Beatmap>>();

        private static readonly Dictionary<Beatmap.Difficulty, Color> difficultyColor = new Dictionary<Beatmap.Difficulty, Color>
        {
            { Beatmap.Difficulty.Easy, Color.FromArgb(125, 180, 0) },
            { Beatmap.Difficulty.Normal, Color.FromArgb(80, 215, 255) },
            { Beatmap.Difficulty.Hard, Color.FromArgb(255, 215, 0) },
            { Beatmap.Difficulty.Insane, Color.FromArgb(255, 80, 170) },
            { Beatmap.Difficulty.Expert, Color.FromArgb(125, 80, 255) }
        };

        public new static string Render(BeatmapSet beatmapSet)
        {
            var skillCharts = GetSkillCharts(beatmapSet);
            var srChart = GetStarRatingChart(beatmapSet);

            if (srChart.Data.Count == 0)
                return "";

            return
                RenderContainer("Difficulty",
                    Div("skill-charts",
                        Render(srChart),
                        string.Concat(skillCharts.Select(pair => Render(pair.Value)))));
        }

        private static LineChart GetStarRatingChart(BeatmapSet beatmapSet)
        {
            var srChart = new LineChart("Star Rating", "Time (Seconds)", "");

            foreach (var beatmap in beatmapSet.Beatmaps)
            {
                if (beatmap.DifficultyAttributes == null)
                    continue;

                srChart.Data.Add(GetStarRatingSeries(beatmap, srChart));

                if (!mapsUsedInChart.ContainsKey(srChart))
                    mapsUsedInChart[srChart] = new List<Beatmap> { beatmap };
                else
                    mapsUsedInChart[srChart].Add(beatmap);
            }

            return srChart;
        }

        private static Dictionary<Skill, LineChart> GetSkillCharts(BeatmapSet beatmapSet)
        {
            var skillCharts = new Dictionary<Skill, LineChart>();

            foreach (var beatmap in beatmapSet.Beatmaps)
            {
                if (beatmap.DifficultyAttributes == null)
                    continue;

                foreach (var skill in beatmap.DifficultyAttributes.Skills)
                {
                    if (!(skill is StrainSkill strainSkill))
                        continue;

                    if (!skillCharts.ContainsKey(skill))
                        skillCharts[skill] = new LineChart($"{skill}", "Time (Seconds)", "");

                    var skillSeries = GetSkillSeries(beatmap, strainSkill, skillCharts[skill]);
                    skillCharts[skill].Data.Add(skillSeries);

                    if (!mapsUsedInChart.ContainsKey(skillCharts[skill]))
                        mapsUsedInChart[skillCharts[skill]] = new List<Beatmap> { beatmap };
                    else
                        mapsUsedInChart[skillCharts[skill]].Add(beatmap);
                }
            }

            return skillCharts;
        }

        private static Series GetSkillSeries(Beatmap beatmap, StrainSkill strainSkill, LineChart chart) => GetPeakSeries(beatmap, strainSkill.GetCurrentStrainPeaks().ToList(), peak => (float)peak, chart);

        private static Series GetStarRatingSeries(Beatmap beatmap, LineChart chart)
        {
            if (beatmap.DifficultyAttributes == null)
                throw new ArgumentException($"Cannot get star rating series of {beatmap}, as `difficultyAttributes` is null.");

            var accumulatedPeaks = new Dictionary<int, List<float>>();

            foreach (var skill in beatmap.DifficultyAttributes.Skills)
            {
                if (!(skill is StrainSkill strainSkill))
                    continue;

                var strainPeaks = strainSkill.GetCurrentStrainPeaks().ToList();

                for (var index = 0; index < strainPeaks.Count; ++index)
                    if (accumulatedPeaks.ContainsKey(index))
                        accumulatedPeaks[index].Add((float)strainPeaks[index]);
                    else
                        accumulatedPeaks[index] = new List<float> { (float)strainPeaks[index] };
            }

            return GetPeakSeries(beatmap, accumulatedPeaks, peak =>
                // TODO: Is this the same for t/c/m?
                peak.Value.Sum() + Math.Abs(peak.Value[0] - peak.Value[1]) * 2, chart);
        }

        private static Series GetPeakSeries<T>(Beatmap beatmap, IEnumerable<T> data, Func<T, float> Value, LineChart chart)
        {
            if (data == null)
                return null;
            
            data = data.ToArray();

            var series = new Series(beatmap.MetadataSettings.version, color: GetGraphColor(beatmap, chart));

            for (var i = 0; i < data.Count(); ++i)
            {
                float time = i * MS_PER_PEAK;
                var value = Value(data.ElementAt(i));

                series.Points.Add(new Vector2(time / 1000f, // Show time in seconds, rather than milliseconds.
                    value));
            }

            return series;
        }

        private static Beatmap.Difficulty DifficultyOf(Beatmap map)
        {
            var diff = map.GetDifficulty(true);

            // Ultra uses a completely black color, which blends into the background too much, hence treat like Expert instead.
            return diff == Beatmap.Difficulty.Ultra ? Beatmap.Difficulty.Expert : diff;
        }

        private static Color GetGraphColor(Beatmap beatmap, LineChart chart)
        {
            var diff = DifficultyOf(beatmap);
            var diffColor = difficultyColor[diff];

            if (!mapsUsedInChart.ContainsKey(chart))
                return diffColor;

            float red = diffColor.R;
            float green = diffColor.G;
            float blue = diffColor.B;

            var sameDiffsInChart = mapsUsedInChart.GetValueOrDefault(chart).Count(map => DifficultyOf(map) == DifficultyOf(beatmap));

            for (var i = 0; i < sameDiffsInChart; ++i)
            {
                // Duplicate difficulties are not distinguishable without changing their color.
                // Idea is to change their shade, but not in a way where it can be confused with other difficulties' lines.

                var mult = 0.7f;
                var multInverse = 1f / mult;

                red *= mult;
                green *= mult;
                blue *= mult;

                switch (diff)
                {
                    case Beatmap.Difficulty.Easy:
                        green *= multInverse;

                        break;

                    case Beatmap.Difficulty.Normal:
                        blue *= multInverse;

                        break;

                    case Beatmap.Difficulty.Hard:
                    case Beatmap.Difficulty.Insane:
                    case Beatmap.Difficulty.Expert:
                        red *= multInverse;

                        break;
                }
            }

            return Color.FromArgb((int)red, (int)green, (int)blue);
        }
    }
}