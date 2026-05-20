using System.Globalization;
using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Framework.Objects.Attributes;
using MapsetVerifier.Framework.Objects.Metadata;
using MapsetVerifier.Parser.Objects;
using MapsetVerifier.Parser.Objects.TimingLines;
using MapsetVerifier.Parser.Statics;
using static MapsetVerifier.Checks.Utils.TaikoUtils;
using static MapsetVerifier.Checks.Utils.TimingUtils;

namespace MapsetVerifier.Checks.Taiko.Timing
{
    [Check]
    public class CheckLowDiffScrollSpeed : BeatmapCheck
    {
        /// <summary>
        ///     Matches <see cref="AllModes.Timing.CheckBeforeLine" /> — effective scroll speeds within this
        ///     margin (in BPM·multiplier units) are treated as identical.
        /// </summary>
        private const double EffectiveScrollTolerance = 1;

        public override CheckMetadata GetMetadata() =>
            new BeatmapCheckMetadata
            {
                Modes = [Beatmap.Mode.Taiko],
                Difficulties = [Beatmap.Difficulty.Easy, Beatmap.Difficulty.Normal],
                Category = "Timing",
                Message = "Scroll speed changes on lower difficulties.",
                Author = "Hivie",
                Documentation = new Dictionary<string, string>
                {
                    {
                        "Purpose",
                        @"
                    Kantan and Futsuu osu!taiko difficulties should keep a consistent scroll speed."
                    },
                    {
                        "Reasoning",
                        @"
                    Slider velocity changes on Kantan and Futsuu can be disorienting for newer players. Inherited timing lines that only normalize variable BPM (keeping the same effective scroll speed) are ignored, as are sections with no hit objects and no visible barlines. The check compares each segment's effective scroll speed — slider velocity multiplied by the current uninherited BPM — against the speed that covers the most time in the difficulty."
                    },
                },
            };

        public override Dictionary<string, IssueTemplate> GetTemplates() =>
            new()
            {
                {
                    "Inconsistent",
                    new IssueTemplate(
                        Issue.Level.Warning,
                        "{0} Slider velocity multiplier {1}x differs from the dominant scroll speed ({2}x at this BPM).",
                        "timestamp -",
                        "currentMultiplier",
                        "dominantMultiplier"
                    ).WithCause(
                        "An inherited timing line changes scroll speed away from the speed used for most of the beatmap."
                    )
                },
            };

        public override IEnumerable<Issue> GetIssues(Beatmap beatmap)
        {
            if (beatmap.GeneralSettings.mode != Beatmap.Mode.Taiko)
                yield break;

            var difficulty = beatmap.GetDifficulty();
            if (difficulty is not Beatmap.Difficulty.Easy and not Beatmap.Difficulty.Normal)
                yield break;

            if (beatmap.HitObjects.Count == 0)
                yield break;

            var segments = BuildEffectiveScrollSegments(beatmap);
            if (segments.Count == 0)
                yield break;

            var dominantScroll = GetDominantEffectiveScroll(segments);
            if (dominantScroll == null)
                yield break;

            foreach (var segment in segments)
            {
                if (EffectiveScrollAlmostEqual(segment.EffectiveScroll, dominantScroll.Value))
                    continue;

                if (segment.Line is not InheritedLine)
                    continue;

                var bpm = beatmap.GetTimingLine<UninheritedLine>(segment.Offset)!.bpm;
                var dominantMultiplier = dominantScroll.Value / bpm;

                yield return new Issue(
                    GetTemplate("Inconsistent"),
                    beatmap,
                    Timestamp.Get(segment.Offset),
                    Math.Round(segment.Line.SvMult, 2).ToString(CultureInfo.InvariantCulture),
                    Math.Round(dominantMultiplier, 2).ToString(CultureInfo.InvariantCulture)
                );
            }
        }

        private static double? GetDominantEffectiveScroll(List<EffectiveScrollSegment> segments)
        {
            var durationByScroll = new Dictionary<double, double>();

            foreach (var segment in segments)
            {
                var key = Math.Round(segment.EffectiveScroll, 2);
                durationByScroll.TryAdd(key, 0);
                durationByScroll[key] += segment.DurationMs;
            }

            return durationByScroll
                .OrderByDescending(pair => pair.Value)
                .Select(pair => (double?)pair.Key)
                .FirstOrDefault();
        }

        private static List<EffectiveScrollSegment> BuildEffectiveScrollSegments(Beatmap beatmap)
        {
            var offsets = beatmap
                .TimingLines.Select(line => line.Offset)
                .Distinct()
                .OrderBy(offset => offset)
                .ToList();

            if (offsets.Count == 0)
                return [];

            var endTime = beatmap.HitObjects.Last().GetEndTime();
            var segments = new List<EffectiveScrollSegment>();

            for (var i = 0; i < offsets.Count; i++)
            {
                var offset = offsets[i];
                var nextOffset = i + 1 < offsets.Count ? offsets[i + 1] : endTime;
                var duration = nextOffset - offset;

                if (duration <= 0)
                    continue;

                var line = beatmap.GetTimingLine(offset);
                if (line == null || !SectionAffectsScrollSpeed(beatmap, line, nextOffset))
                    continue;

                segments.Add(
                    new EffectiveScrollSegment(
                        offset,
                        duration,
                        GetEffectiveScrollSpeed(beatmap, offset),
                        line
                    )
                );
            }

            return segments;
        }

        private static double GetEffectiveScrollSpeed(Beatmap beatmap, double time)
        {
            var line = beatmap.GetTimingLine(time);
            var uninheritedLine = beatmap.GetTimingLine<UninheritedLine>(time);

            if (line == null || uninheritedLine == null)
                return 0;

            return line.SvMult * uninheritedLine.bpm;
        }

        private static bool EffectiveScrollAlmostEqual(double a, double b) =>
            Math.Abs(a - b) <= EffectiveScrollTolerance;

        private static bool SectionAffectsScrollSpeed(
            Beatmap beatmap,
            TimingLine line,
            double sectionEnd
        )
        {
            if (sectionEnd <= line.Offset)
                return false;

            if (SectionContainsObject<HitObject>(beatmap, line))
                return true;

            // The timing line may be off-barline; still count visible barlines before sectionEnd.
            var time = line.Offset;

            while (time < sectionEnd)
            {
                if (IsOnBarline(beatmap, time) && IsBarlineVisible(beatmap, time))
                    return true;

                var untilNext = GetMsUntilNextBarline(beatmap, time);
                if (untilNext <= Common.ROUNDING_ERROR_MARGIN)
                    break;

                var nextBarline = time + untilNext;
                if (
                    nextBarline < sectionEnd
                    && IsOnBarline(beatmap, nextBarline)
                    && IsBarlineVisible(beatmap, nextBarline)
                )
                    return true;

                time = nextBarline;
            }

            return false;
        }

        private sealed record EffectiveScrollSegment(
            double Offset,
            double DurationMs,
            double EffectiveScroll,
            TimingLine Line
        );
    }
}
