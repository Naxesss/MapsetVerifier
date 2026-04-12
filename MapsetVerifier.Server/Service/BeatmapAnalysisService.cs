using System.Globalization;
using MapsetVerifier.Parser.Difficulty;
using MapsetVerifier.Parser.Objects;
using MapsetVerifier.Parser.Objects.HitObjects;
using MapsetVerifier.Parser.Objects.HitObjects.Mania;
using MapsetVerifier.Parser.Objects.TimingLines;
using MapsetVerifier.Parser.Statics;
using MapsetVerifier.Server.Model.BeatmapAnalysis;
using osu.Game.Rulesets.Difficulty;
using osu.Game.Rulesets.Difficulty.Skills;
using Serilog;

namespace MapsetVerifier.Server.Service;

public static class BeatmapAnalysisService
{
    private static readonly int[] SupportedSnapDivisors = [1, 2, 3, 4, 5, 6, 7, 8, 9, 12, 16];
    private const double TimelineMarginMs = 2000;
    private const int MsPerPeak = 400;

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
            var difficulties = beatmapSet.Beatmaps.Select(beatmap => GetObjectsOverviewDifficulty(beatmap, endTimeMs)).ToList();

            return ObjectsOverviewResult.CreateSuccess(startTimeMs, endTimeMs, difficulties);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to analyze objects overview for {Folder}", beatmapSetFolder);
            return ObjectsOverviewResult.CreateError($"Objects overview analysis failed: {ex.Message}");
        }
    }

    public static DifficultyOverviewResult AnalyzeDifficulty(string beatmapSetFolder)
    {
        try
        {
            var beatmapSet = new BeatmapSet(beatmapSetFolder);

            if (beatmapSet.Beatmaps.Count == 0)
                return DifficultyOverviewResult.CreateError("No beatmaps found in folder.");

            var difficulties = beatmapSet.Beatmaps.Select(GetDifficultyOverviewDifficulty).ToList();
            return DifficultyOverviewResult.CreateSuccess(MsPerPeak, difficulties);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to analyze difficulty overview for {Folder}", beatmapSetFolder);
            return DifficultyOverviewResult.CreateError($"Difficulty overview analysis failed: {ex.Message}");
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

    private static DifficultyOverviewDifficulty GetDifficultyOverviewDifficulty(Beatmap beatmap)
    {
        return new DifficultyOverviewDifficulty
        {
            Label = beatmap.MetadataSettings.version,
            Version = beatmap.MetadataSettings.version,
            Mode = beatmap.GeneralSettings.mode.ToString(),
            DifficultyLevel = beatmap.GetDifficulty().ToString(),
            StarRating = beatmap.StarRating,
            StarRatingValues = GetStarRatingValues(beatmap),
            Skills = beatmap.Skills
                .OfType<StrainSkill>()
                .Select(skill => new DifficultySkillData
                {
                    SkillName = GetSkillName(skill, beatmap),
                    StrainPeaks = skill.GetCurrentStrainPeaks().Select(peak => (double)peak).ToList()
                })
                .ToList()
        };
    }

    private static List<double> GetStarRatingValues(Beatmap beatmap)
    {
        var timedAttributes = new LocalDifficultyCalculator().CalculateTimedAttributes(beatmap);

        if (timedAttributes.Count == 0)
            return [];

        var finalTime = Math.Max(timedAttributes[^1].Time, beatmap.HitObjects.Max(hitObject => hitObject.GetEndTime()));
        var pointCount = Math.Max(1, (int)Math.Ceiling(finalTime / MsPerPeak) + 1);
        var values = new List<double>(pointCount);

        var timedIndex = 0;
        var currentStarRating = 0d;

        for (var index = 0; index < pointCount; index++)
        {
            var sampleTime = index * MsPerPeak;

            while (timedIndex < timedAttributes.Count && timedAttributes[timedIndex].Time <= sampleTime)
            {
                currentStarRating = timedAttributes[timedIndex].Attributes.StarRating;
                timedIndex++;
            }

            values.Add(currentStarRating);
        }

        return values;
    }

    private static string GetSkillName(Skill skill, Beatmap beatmap) => SkillNameFormatter.GetSkillName(skill, beatmap);

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

    private static ObjectsOverviewDifficulty GetObjectsOverviewDifficulty(Beatmap beatmap, double globalEndTimeMs)
    {
        var timelineObjects = beatmap.HitObjects.Select(hitObject => new ObjectsTimelineObject
        {
            StartTimeMs = hitObject.time,
            EndTimeMs = hitObject.GetEndTime(),
            ObjectType = hitObject.GetObjectType(),
            HasFinishHitSound = hitObject.HasHitSound(HitObject.HitSounds.Finish),
            ComboColourIndex = GetObjectComboColourIndex(beatmap, hitObject),
            ComboColourHex = GetObjectComboColourHex(beatmap, hitObject),
            Edges = hitObject.GetEdgeTimes().Select(edgeTime => new ObjectsTimelineEdge
            {
                TimeMs = edgeTime,
                PartName = hitObject.GetPartName(edgeTime)
            }).ToList()
        }).ToList();

        var timingSegments = beatmap.TimingLines
            .OfType<UninheritedLine>()
            .Select(line => new ObjectsTimingSegment
            {
                StartTimeMs = line.Offset,
                EndTimeMs = Math.Max(line.Offset, beatmap.GetNextTimingLine<UninheritedLine>(line.Offset)?.Offset ?? globalEndTimeMs),
                OffsetMs = line.Offset,
                MsPerBeat = line.msPerBeat,
                Bpm = line.bpm,
                Meter = line.Meter
            })
            .Where(segment => segment.EndTimeMs > segment.StartTimeMs && segment.MsPerBeat > 0)
            .ToList();

        var breakPeriods = beatmap.Breaks
            .Select(@break => new ObjectsBreakPeriod
            {
                StartTimeMs = @break.GetRealStart(beatmap),
                EndTimeMs = @break.GetRealEnd(beatmap)
            })
            .Where(period => period.EndTimeMs > period.StartTimeMs)
            .ToList();

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
            BreakPeriods = breakPeriods,
            TimelineObjects = timelineObjects,
            TimingSegments = timingSegments,
            Snappings = SupportedSnapDivisors.Select(divisor => new ObjectsSnappingBucket
            {
                Divisor = divisor,
                Label = $"1/{divisor}",
                Count = snappingCounts[divisor],
                Percentage = edgeCount > 0 ? snappingCounts[divisor] * 100d / totalSnapPoints : 0
            }).ToList()
        };
    }

    private static int? GetObjectComboColourIndex(Beatmap beatmap, HitObject hitObject)
    {
        if (hitObject is not Circle && hitObject is not Slider)
            return null;

        return beatmap.GeneralSettings.mode == Beatmap.Mode.Taiko
            ? null
            : beatmap.GetComboColourIndex(hitObject.time);
    }

    private static string? GetObjectComboColourHex(Beatmap beatmap, HitObject hitObject)
    {
        if (hitObject is not Circle && hitObject is not Slider)
            return null;

        if (beatmap.GeneralSettings.mode == Beatmap.Mode.Taiko)
        {
            if (hitObject is Slider)
                return "#FCBF1F";

            return hitObject.HasHitSound(HitObject.HitSounds.Clap) || hitObject.HasHitSound(HitObject.HitSounds.Whistle)
                ? "#448DAB"
                : "#EB452C";
        }

        var colourIndex = beatmap.GetComboColourIndex(hitObject.time);
        if (beatmap.ColourSettings.combos.Count > colourIndex)
        {
            var colour = beatmap.ColourSettings.combos[colourIndex];
            return $"#{(int)colour.X:X2}{(int)colour.Y:X2}{(int)colour.Z:X2}";
        }

        return "#7D7D7D";
    }
}


