using System.Globalization;
using MapsetVerifier.Parser.Objects;
using MapsetVerifier.Parser.Objects.HitObjects;
using MapsetVerifier.Parser.Objects.HitObjects.Mania;
using MapsetVerifier.Parser.Objects.TimingLines;
using MapsetVerifier.Parser.Statics;
using MapsetVerifier.Server.Model.BeatmapAnalysis;
using Serilog;

namespace MapsetVerifier.Server.Service;

public static class BeatmapAnalysisService
{
    private static readonly int[] SupportedSnapDivisors = [1, 2, 3, 4, 5, 6, 7, 8, 9, 12, 16];
    private const double TimelineMarginMs = 2000;

    public static BeatmapAnalysisResult Analyze(string beatmapSetFolder)
    {
        try
        {
            var beatmapSet = new BeatmapSet(beatmapSetFolder);

            if (beatmapSet.Beatmaps.Count == 0)
                return BeatmapAnalysisResult.CreateError("No beatmaps found in folder.");

            var statistics = GetStatistics(beatmapSet);
            var generalSettings = GetGeneralSettings(beatmapSet);
            var difficultySettings = GetDifficultySettings(beatmapSet);

            return BeatmapAnalysisResult.CreateSuccess(statistics, generalSettings, difficultySettings);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to analyze beatmap for {Folder}", beatmapSetFolder);
            return BeatmapAnalysisResult.CreateError($"Analysis failed: {ex.Message}");
        }
    }

    public static ObjectsOverviewResult AnalyzeObjects(string beatmapSetFolder)
    {
        try
        {
            var beatmapSet = new BeatmapSet(beatmapSetFolder);

            if (beatmapSet.Beatmaps.Count == 0)
                return ObjectsOverviewResult.CreateError("No beatmaps found in folder.");

            var startTimeMs = GetTimelineStartTime(beatmapSet);
            var endTimeMs = GetTimelineEndTime(beatmapSet);
            var difficulties = beatmapSet.Beatmaps.Select(GetObjectsOverviewDifficulty).ToList();

            return ObjectsOverviewResult.CreateSuccess(startTimeMs, endTimeMs, difficulties);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to analyze objects overview for {Folder}", beatmapSetFolder);
            return ObjectsOverviewResult.CreateError($"Objects overview analysis failed: {ex.Message}");
        }
    }

    private static List<DifficultyStatistics> GetStatistics(BeatmapSet beatmapSet)
    {
        return beatmapSet.Beatmaps.Select(beatmap =>
        {
            var mode = beatmap.GeneralSettings.mode;
            var isMania = mode == Beatmap.Mode.Mania;
            var keys = (int)beatmap.DifficultySettings.circleSize;

            var stats = new DifficultyStatistics
            {
                Version = beatmap.MetadataSettings.version,
                Mode = mode.ToString(),
                StarRating = mode == Beatmap.Mode.Standard || mode == Beatmap.Mode.Taiko
                    ? beatmap.StarRating : null,
                CircleCount = beatmap.HitObjects.OfType<Circle>().Count(),
                SliderCount = isMania ? null : beatmap.HitObjects.OfType<Slider>().Count(),
                SpinnerCount = isMania ? null : beatmap.HitObjects.OfType<Spinner>().Count(),
                HoldNoteCount = isMania ? beatmap.HitObjects.OfType<HoldNote>().Count() : null,
                ColumnCount = isMania ? keys : 0,
                NewComboCount = beatmap.HitObjects.Count(o => o.type.HasFlag(HitObject.Types.NewCombo)),
                BreakCount = beatmap.Breaks.Count,
                UninheritedLineCount = beatmap.TimingLines.OfType<UninheritedLine>().Count(),
                InheritedLineCount = beatmap.TimingLines.OfType<InheritedLine>().Count(),
                DrainTimeMs = beatmap.GetDrainTime(mode),
                DrainTimeFormatted = Timestamp.Get(beatmap.GetDrainTime(mode)),
                PlayTimeMs = beatmap.GetPlayTime(),
                PlayTimeFormatted = Timestamp.Get(beatmap.GetPlayTime())
            };

            // Mania column distribution
            if (isMania && keys > 0)
            {
                stats.ObjectsPerColumn = Enumerable.Range(0, keys)
                    .Select(col => beatmap.HitObjects.Count(o => ManiaExtensions.GetColumn(o, keys) == col))
                    .ToList();
            }

            // Kiai time calculation
            var kiaiMs = CalculateKiaiTime(beatmap);
            stats.KiaiTimeMs = kiaiMs;
            stats.KiaiTimeFormatted = Timestamp.Get(kiaiMs);

            return stats;
        }).ToList();
    }

    private static double CalculateKiaiTime(Beatmap beatmap)
    {
        var lines = beatmap.TimingLines.OrderBy(l => l.Offset).ToList();
        if (lines.Count == 0) return 0;

        double totalKiai = 0;
        double? kiaiStart = null;

        foreach (var line in lines)
        {
            if (line.Kiai && kiaiStart == null)
            {
                kiaiStart = line.Offset;
            }
            else if (!line.Kiai && kiaiStart != null)
            {
                totalKiai += line.Offset - kiaiStart.Value;
                kiaiStart = null;
            }
        }

        // If kiai extends to end of map
        if (kiaiStart != null && beatmap.HitObjects.Count > 0)
        {
            var lastObjectTime = beatmap.HitObjects.Max(o => o.GetEndTime());
            totalKiai += lastObjectTime - kiaiStart.Value;
        }

        return totalKiai;
    }

    private static List<DifficultyGeneralSettings> GetGeneralSettings(BeatmapSet beatmapSet)
    {
        return beatmapSet.Beatmaps.Select(beatmap =>
        {
            var mode = beatmap.GeneralSettings.mode;
            var hasStoryboard = beatmap.HasDifficultySpecificStoryboard() || (beatmapSet.Osb?.IsUsed() ?? false);
            var hasVideoOrStoryboard = beatmap.Videos.Any() || hasStoryboard;
            var hasCountdown = beatmap.GetCountdownStartBeat() >= 0 && 
                               beatmap.GeneralSettings.countdown != Parser.Settings.GeneralSettings.Countdown.None;

            return new DifficultyGeneralSettings
            {
                Version = beatmap.MetadataSettings.version,
                Mode = mode.ToString(),
                AudioFileName = beatmap.GeneralSettings.audioFileName,
                AudioLeadIn = beatmap.GeneralSettings.audioLeadIn,
                StackLeniency = mode == Beatmap.Mode.Standard
                    ? beatmap.GeneralSettings.stackLeniency.ToString(CultureInfo.InvariantCulture)
                    : null,
                HasCountdown = hasCountdown,
                CountdownSpeed = hasCountdown ? beatmap.GeneralSettings.countdown.ToString() : null,
                CountdownOffset = hasCountdown ? beatmap.GeneralSettings.countdownBeatOffset : null,
                LetterboxInBreaks = beatmap.GeneralSettings.letterbox,
                WidescreenStoryboard = beatmap.GeneralSettings.widescreenSupport,
                PreviewTime = beatmap.GeneralSettings.previewTime,
                PreviewTimeFormatted = Timestamp.Get(beatmap.GeneralSettings.previewTime),
                UseSkinSprites = hasStoryboard ? beatmap.GeneralSettings.useSkinSprites.ToString() : null,
                SkinPreference = beatmap.GeneralSettings.skinPreference,
                EpilepsyWarning = hasVideoOrStoryboard ? beatmap.GeneralSettings.epilepsyWarning.ToString() : null
            };
        }).ToList();
    }

    private static List<DifficultyDifficultySettings> GetDifficultySettings(BeatmapSet beatmapSet)
    {
        return beatmapSet.Beatmaps.Select(beatmap =>
        {
            var mode = beatmap.GeneralSettings.mode;
            var isTaiko = mode == Beatmap.Mode.Taiko;
            var isMania = mode == Beatmap.Mode.Mania;

            return new DifficultyDifficultySettings
            {
                Version = beatmap.MetadataSettings.version,
                Mode = mode.ToString(),
                HpDrain = beatmap.DifficultySettings.hpDrain,
                CircleSize = isTaiko ? null : beatmap.DifficultySettings.circleSize.ToString(CultureInfo.InvariantCulture),
                OverallDifficulty = beatmap.DifficultySettings.overallDifficulty,
                ApproachRate = isMania ? null : beatmap.DifficultySettings.approachRate.ToString(CultureInfo.InvariantCulture),
                SliderTickRate = isMania ? null : beatmap.DifficultySettings.sliderTickRate.ToString(CultureInfo.InvariantCulture),
                SliderVelocity = isMania ? null : beatmap.DifficultySettings.sliderMultiplier.ToString(CultureInfo.InvariantCulture)
            };
        }).ToList();
    }

    private static double GetTimelineStartTime(BeatmapSet beatmapSet)
    {
        var minTimes = new List<double>();

        foreach (var beatmap in beatmapSet.Beatmaps)
        {
            if (beatmap.HitObjects.Count > 0)
                minTimes.Add(beatmap.HitObjects.Min(hitObject => hitObject.time));

            if (beatmap.TimingLines.Count > 0)
                minTimes.Add(beatmap.TimingLines.Min(timingLine => timingLine.Offset));
        }

        return minTimes.Count == 0 ? 0 : minTimes.Min() - TimelineMarginMs;
    }

    private static double GetTimelineEndTime(BeatmapSet beatmapSet)
    {
        var maxTimes = new List<double>();

        foreach (var beatmap in beatmapSet.Beatmaps)
        {
            if (beatmap.HitObjects.Count > 0)
                maxTimes.Add(beatmap.HitObjects.Max(hitObject => hitObject.GetEndTime()));

            if (beatmap.TimingLines.Count > 0)
                maxTimes.Add(beatmap.TimingLines.Max(timingLine => timingLine.Offset));
        }

        return maxTimes.Count == 0 ? TimelineMarginMs : maxTimes.Max() + TimelineMarginMs;
    }

    private static ObjectsOverviewDifficulty GetObjectsOverviewDifficulty(Beatmap beatmap)
    {
        var timelineObjects = beatmap.HitObjects.Select(hitObject => new ObjectsTimelineObject
        {
            StartTimeMs = hitObject.time,
            EndTimeMs = hitObject.GetEndTime(),
            ObjectType = hitObject.GetObjectType(),
            Edges = hitObject.GetEdgeTimes().Select(edgeTime => new ObjectsTimelineEdge
            {
                TimeMs = edgeTime,
                PartName = hitObject.GetPartName(edgeTime)
            }).ToList()
        }).ToList();

        var snappingCounts = SupportedSnapDivisors.ToDictionary(divisor => divisor, _ => 0);
        var edgeCount = 0;
        var unsnappedCount = 0;

        foreach (var hitObject in beatmap.HitObjects)
        {
            foreach (var edgeTime in hitObject.GetEdgeTimes())
            {
                edgeCount++;

                if (beatmap.GetTimingLine<UninheritedLine>(edgeTime) == null)
                {
                    unsnappedCount++;
                    continue;
                }

                var divisor = beatmap.GetLowestDivisor(edgeTime);
                if (snappingCounts.ContainsKey(divisor))
                    snappingCounts[divisor]++;
                else
                    unsnappedCount++;
            }
        }

        var totalSnapPoints = Math.Max(1, edgeCount);

        return new ObjectsOverviewDifficulty
        {
            Version = beatmap.MetadataSettings.version,
            Mode = beatmap.GeneralSettings.mode.ToString(),
            ObjectCount = beatmap.HitObjects.Count,
            EdgeCount = edgeCount,
            UnsnappedCount = unsnappedCount,
            UnsnappedPercentage = edgeCount > 0 ? unsnappedCount * 100d / totalSnapPoints : 0,
            TimelineObjects = timelineObjects,
            Snappings = SupportedSnapDivisors.Select(divisor => new ObjectsSnappingBucket
            {
                Divisor = divisor,
                Label = $"1/{divisor}",
                Count = snappingCounts[divisor],
                Percentage = edgeCount > 0 ? snappingCounts[divisor] * 100d / totalSnapPoints : 0
            }).ToList()
        };
    }
}

