using System.Globalization;
using MapsetVerifier.Parser.Difficulty;
using MapsetVerifier.Parser.Objects;
using MapsetVerifier.Parser.Objects.HitObjects;
using MapsetVerifier.Parser.Objects.HitObjects.Mania;
using MapsetVerifier.Parser.Objects.TimingLines;
using MapsetVerifier.Parser.Statics;
using MapsetVerifier.Server.Model.BeatmapAnalysis;
using MathNet.Numerics;
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

            return BeatmapAnalysisResult.CreateSuccess(
                statistics,
                generalSettings,
                difficultySettings
            );
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
            var difficulties = beatmapSet
                .Beatmaps.Select(beatmap => GetObjectsOverviewDifficulty(beatmap, endTimeMs))
                .ToList();

            return ObjectsOverviewResult.CreateSuccess(startTimeMs, endTimeMs, difficulties);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to analyze objects overview for {Folder}", beatmapSetFolder);
            return ObjectsOverviewResult.CreateError(
                $"Objects overview analysis failed: {ex.Message}"
            );
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
            return DifficultyOverviewResult.CreateError(
                $"Difficulty overview analysis failed: {ex.Message}"
            );
        }
    }

    private static List<DifficultyStatistics> GetStatistics(BeatmapSet beatmapSet)
    {
        return beatmapSet
            .Beatmaps.Select(beatmap =>
            {
                var mode = beatmap.GeneralSettings.mode;
                var isMania = mode == Beatmap.Mode.Mania;
                var keys = (int)beatmap.DifficultySettings.circleSize;

                var stats = new DifficultyStatistics
                {
                    Version = beatmap.MetadataSettings.version,
                    Mode = mode.ToString(),
                    StarRating = beatmap.StarRating,
                    CircleCount = beatmap.HitObjects.OfType<Circle>().Count(),
                    SliderCount = isMania ? null : beatmap.HitObjects.OfType<Slider>().Count(),
                    SpinnerCount = isMania ? null : beatmap.HitObjects.OfType<Spinner>().Count(),
                    HoldNoteCount = isMania ? beatmap.HitObjects.OfType<HoldNote>().Count() : null,
                    ColumnCount = isMania ? keys : 0,
                    NewComboCount = beatmap.HitObjects.Count(o =>
                        o.type.HasFlag(HitObject.Types.NewCombo)
                    ),
                    BreakCount = beatmap.Breaks.Count,
                    UninheritedLineCount = beatmap.TimingLines.OfType<UninheritedLine>().Count(),
                    InheritedLineCount = beatmap.TimingLines.OfType<InheritedLine>().Count(),
                    DrainTimeMs = beatmap.GetDrainTime(mode),
                    DrainTimeFormatted = Timestamp.Get(beatmap.GetDrainTime(mode)),
                    PlayTimeMs = beatmap.GetPlayTime(),
                    PlayTimeFormatted = Timestamp.Get(beatmap.GetPlayTime()),
                };

                // Mania column distribution
                if (isMania && keys > 0)
                {
                    stats.ObjectsPerColumn = Enumerable
                        .Range(0, keys)
                        .Select(col =>
                            beatmap.HitObjects.Count(o => ManiaExtensions.GetColumn(o, keys) == col)
                        )
                        .ToList();
                }

                // Kiai time calculation
                var kiaiMs = CalculateKiaiTime(beatmap);
                stats.KiaiTimeMs = kiaiMs;
                stats.KiaiTimeFormatted = Timestamp.Get(kiaiMs);

                return stats;
            })
            .ToList();
    }

    private static double CalculateKiaiTime(Beatmap beatmap)
    {
        var lines = beatmap.TimingLines.OrderBy(l => l.Offset).ToList();
        if (lines.Count == 0)
            return 0;

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
        return beatmapSet
            .Beatmaps.Select(beatmap =>
            {
                var mode = beatmap.GeneralSettings.mode;
                var hasStoryboard =
                    beatmap.HasDifficultySpecificStoryboard()
                    || (beatmapSet.Osb?.IsUsed() ?? false);
                var hasVideoOrStoryboard = beatmap.Videos.Any() || hasStoryboard;
                var countdownSetting = beatmap.GeneralSettings.countdown;
                var countdownSettingEnabled =
                    countdownSetting != Parser.Settings.GeneralSettings.Countdown.None;
                var hasCountdown = countdownSettingEnabled && beatmap.GetCountdownStartBeat() >= 0;

                return new DifficultyGeneralSettings
                {
                    Version = beatmap.MetadataSettings.version,
                    Mode = mode.ToString(),
                    AudioFileName = beatmap.GeneralSettings.audioFileName,
                    AudioLeadIn = beatmap.GeneralSettings.audioLeadIn,
                    StackLeniency =
                        mode == Beatmap.Mode.Standard
                            ? beatmap.GeneralSettings.stackLeniency.ToString(
                                CultureInfo.InvariantCulture
                            )
                            : null,
                    HasCountdown = hasCountdown,
                    CountdownInsufficientTime = countdownSettingEnabled && !hasCountdown,
                    CountdownSpeed = countdownSettingEnabled ? countdownSetting.ToString() : null,
                    CountdownOffset = countdownSettingEnabled
                        ? beatmap.GeneralSettings.countdownBeatOffset
                        : null,
                    LetterboxInBreaks = beatmap.GeneralSettings.letterbox,
                    WidescreenStoryboard = beatmap.GeneralSettings.widescreenSupport,
                    PreviewTime = beatmap.GeneralSettings.previewTime,
                    PreviewTimeFormatted = Timestamp.Get(beatmap.GeneralSettings.previewTime),
                    UseSkinSprites = hasStoryboard
                        ? beatmap.GeneralSettings.useSkinSprites.ToString()
                        : null,
                    SkinPreference = beatmap.GeneralSettings.skinPreference,
                    EpilepsyWarning = hasVideoOrStoryboard
                        ? beatmap.GeneralSettings.epilepsyWarning.ToString()
                        : null,
                };
            })
            .ToList();
    }

    private static List<DifficultyDifficultySettings> GetDifficultySettings(BeatmapSet beatmapSet)
    {
        return beatmapSet
            .Beatmaps.Select(beatmap =>
            {
                var mode = beatmap.GeneralSettings.mode;
                var isTaiko = mode == Beatmap.Mode.Taiko;
                var isMania = mode == Beatmap.Mode.Mania;

                return new DifficultyDifficultySettings
                {
                    Version = beatmap.MetadataSettings.version,
                    Mode = mode.ToString(),
                    HpDrain = beatmap.DifficultySettings.hpDrain,
                    CircleSize = isTaiko
                        ? null
                        : beatmap.DifficultySettings.circleSize.ToString(
                            CultureInfo.InvariantCulture
                        ),
                    OverallDifficulty = beatmap.DifficultySettings.overallDifficulty,
                    ApproachRate = isMania
                        ? null
                        : beatmap.DifficultySettings.approachRate.ToString(
                            CultureInfo.InvariantCulture
                        ),
                    SliderTickRate = isMania
                        ? null
                        : beatmap.DifficultySettings.sliderTickRate.ToString(
                            CultureInfo.InvariantCulture
                        ),
                    SliderVelocity = isMania
                        ? null
                        : beatmap.DifficultySettings.sliderMultiplier.ToString(
                            CultureInfo.InvariantCulture
                        ),
                };
            })
            .ToList();
    }

    private static DifficultyOverviewDifficulty GetDifficultyOverviewDifficulty(Beatmap beatmap)
    {
        var timedAttributes = new LocalDifficultyCalculator().CalculateTimedAttributes(beatmap);
        var pointCount = GetSamplePointCount(beatmap, timedAttributes);

        return new DifficultyOverviewDifficulty
        {
            Label = beatmap.MetadataSettings.version,
            Version = beatmap.MetadataSettings.version,
            Mode = beatmap.GeneralSettings.mode.ToString(),
            DifficultyLevel = beatmap.GetDifficulty().ToString(),
            StarRating = beatmap.StarRating,
            StarRatingSamples = GetStarRatingSamples(beatmap, timedAttributes, pointCount),
            SliderVelocitySamples = GetSliderVelocitySamples(beatmap),
            VolumeSamples = GetVolumeSamples(beatmap),
            Skills = GetSkillSamples(beatmap, pointCount),
        };
    }

    private static int GetSamplePointCount(
        Beatmap beatmap,
        List<osu.Game.Rulesets.Difficulty.TimedDifficultyAttributes> timedAttributes
    )
    {
        if (timedAttributes.Count == 0)
            return 0;

        var finalTime = GetFinalSampleTimeMs(beatmap, timedAttributes);
        return Math.Max(1, (int)Math.Ceiling(finalTime / MsPerPeak) + 1);
    }

    private static double GetMapEndTimeMs(Beatmap beatmap)
    {
        if (beatmap.TimingLines.Count == 0 && beatmap.HitObjects.Count == 0)
            return 0;

        var timingMax =
            beatmap.TimingLines.Count > 0
                ? beatmap.TimingLines.Max(timingLine => timingLine.Offset)
                : 0;
        var objectsMax =
            beatmap.HitObjects.Count > 0
                ? beatmap.HitObjects.Max(hitObject => hitObject.GetEndTime())
                : 0;

        return Math.Max(timingMax, objectsMax);
    }

    private static double GetFinalSampleTimeMs(
        Beatmap beatmap,
        List<osu.Game.Rulesets.Difficulty.TimedDifficultyAttributes> timedAttributes
    )
    {
        if (timedAttributes.Count > 0)
        {
            return Math.Max(
                timedAttributes[^1].Time,
                beatmap.HitObjects.Count > 0
                    ? beatmap.HitObjects.Max(hitObject => hitObject.GetEndTime())
                    : 0
            );
        }

        if (beatmap.TimingLines.Count == 0 && beatmap.HitObjects.Count == 0)
            return 0;

        var timingMax =
            beatmap.TimingLines.Count > 0
                ? beatmap.TimingLines.Max(timingLine => timingLine.Offset)
                : 0;
        var objectsMax =
            beatmap.HitObjects.Count > 0
                ? beatmap.HitObjects.Max(hitObject => hitObject.GetEndTime())
                : 0;

        return Math.Max(timingMax, objectsMax);
    }

    private static double GetFirstStrainSectionEndMs(Beatmap beatmap)
    {
        if (beatmap.HitObjects.Count == 0)
            return MsPerPeak;

        var firstObjectTime = beatmap.HitObjects.Min(hitObject => hitObject.time);
        return Math.Ceiling(firstObjectTime / MsPerPeak) * MsPerPeak;
    }

    private static List<DifficultySamplePoint> GetStarRatingSamples(
        Beatmap beatmap,
        List<osu.Game.Rulesets.Difficulty.TimedDifficultyAttributes> timedAttributes,
        int pointCount
    )
    {
        if (timedAttributes.Count == 0 || pointCount == 0)
            return [];

        var samples = new List<DifficultySamplePoint>(pointCount);

        var timedIndex = 0;
        var currentStarRating = 0d;

        for (var index = 0; index < pointCount; index++)
        {
            var sampleTime = index * MsPerPeak;

            while (
                timedIndex < timedAttributes.Count && timedAttributes[timedIndex].Time <= sampleTime
            )
            {
                currentStarRating = timedAttributes[timedIndex].Attributes.StarRating;
                timedIndex++;
            }

            samples.Add(
                new DifficultySamplePoint { TimeMs = sampleTime, Value = currentStarRating }
            );
        }

        return samples;
    }

    private static List<DifficultySamplePoint> GetSliderVelocitySamples(Beatmap beatmap)
    {
        if (beatmap.GeneralSettings.mode == Beatmap.Mode.Mania)
            return [];

        if (beatmap.TimingLines.Count == 0)
            return [];

        return BuildTimingLineChangeSamples(
            beatmap,
            beatmap.GetSvChanges(),
            timeMs => beatmap.GetTimingLine(timeMs)?.SvMult ?? 1.0
        );
    }

    private static List<DifficultySamplePoint> GetVolumeSamples(Beatmap beatmap)
    {
        if (beatmap.TimingLines.Count == 0)
            return [];

        return BuildTimingLineChangeSamples(
            beatmap,
            beatmap.GetVolumeChanges(),
            timeMs => beatmap.GetTimingLine(timeMs)?.Volume ?? 100.0
        );
    }

    private static List<DifficultySamplePoint> BuildTimingLineChangeSamples(
        Beatmap beatmap,
        List<TimingLine> changes,
        Func<double, double> getValueAt
    )
    {
        var endTimeMs = GetMapEndTimeMs(beatmap);
        if (endTimeMs <= 0)
            return [];

        var samples = new List<DifficultySamplePoint>
        {
            new() { TimeMs = 0, Value = getValueAt(0) },
        };

        foreach (var line in changes)
        {
            if (samples.Count > 0 && samples[^1].TimeMs.AlmostEqual(line.Offset))
            {
                samples[^1] = new DifficultySamplePoint
                {
                    TimeMs = line.Offset,
                    Value = getValueAt(line.Offset),
                };
            }
            else
            {
                samples.Add(
                    new DifficultySamplePoint
                    {
                        TimeMs = line.Offset,
                        Value = getValueAt(line.Offset),
                    }
                );
            }
        }

        var lastValue = getValueAt(endTimeMs);
        if (!samples[^1].TimeMs.AlmostEqual(endTimeMs))
        {
            samples.Add(new DifficultySamplePoint { TimeMs = endTimeMs, Value = lastValue });
        }
        else
        {
            samples[^1] = new DifficultySamplePoint { TimeMs = endTimeMs, Value = lastValue };
        }

        return samples;
    }

    private static List<DifficultySkillData> GetSkillSamples(Beatmap beatmap, int pointCount)
    {
        if (pointCount == 0)
            return [];

        return beatmap
            .Skills.OfType<StrainSkill>()
            .Select(skill => new DifficultySkillData
            {
                SkillName = GetSkillName(skill, beatmap),
                StrainSamples = GetStrainSkillSamples(skill, beatmap, pointCount),
            })
            .ToList();
    }

    private static List<DifficultySamplePoint> GetStrainSkillSamples(
        StrainSkill skill,
        Beatmap beatmap,
        int pointCount
    )
    {
        var peaks = skill.GetCurrentStrainPeaks().ToList();
        var firstSectionEnd = GetFirstStrainSectionEndMs(beatmap);
        var samples = new List<DifficultySamplePoint>(pointCount);

        var peakIndex = 0;
        var currentPeak = 0d;

        for (var index = 0; index < pointCount; index++)
        {
            var sampleTime = firstSectionEnd + index * MsPerPeak;

            while (peakIndex < peaks.Count && firstSectionEnd + peakIndex * MsPerPeak <= sampleTime)
            {
                currentPeak = peaks[peakIndex];
                peakIndex++;
            }

            samples.Add(new DifficultySamplePoint { TimeMs = sampleTime, Value = currentPeak });
        }

        return samples;
    }

    private static string GetSkillName(Skill skill, Beatmap beatmap) =>
        SkillNameFormatter.GetSkillName(skill, beatmap);

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

    private static ObjectsOverviewDifficulty GetObjectsOverviewDifficulty(
        Beatmap beatmap,
        double globalEndTimeMs
    )
    {
        var timelineObjects = beatmap
            .HitObjects.Select(hitObject => new ObjectsTimelineObject
            {
                StartTimeMs = hitObject.time,
                EndTimeMs = hitObject.GetEndTime(),
                ObjectType = hitObject.GetObjectType(),
                HasFinishHitSound = hitObject.HasHitSound(HitObject.HitSounds.Finish),
                HitSoundFlags = GetObjectHitSoundFlags(hitObject),
                ComboColourIndex = GetObjectComboColourIndex(beatmap, hitObject),
                ComboColourHex = GetObjectComboColourHex(beatmap, hitObject),
                Edges = hitObject
                    .GetEdgeTimes()
                    .Select(edgeTime => new ObjectsTimelineEdge
                    {
                        TimeMs = edgeTime,
                        PartName = hitObject.GetPartName(edgeTime),
                        HitSoundFlags = GetEdgeHitSoundFlags(hitObject, edgeTime),
                    })
                    .ToList(),
            })
            .ToList();

        var timelineSamples = beatmap
            .HitObjects.SelectMany(hitObject => BuildTimelineSamples(hitObject))
            .OrderBy(sample => sample.TimeMs)
            .ToList();

        var hitsoundGapPeriods = BuildHitsoundGapPeriods(beatmap);

        var timingSegments = beatmap
            .TimingLines.OfType<UninheritedLine>()
            .Select(line => new ObjectsTimingSegment
            {
                StartTimeMs = line.Offset,
                EndTimeMs = Math.Max(
                    line.Offset,
                    beatmap.GetNextTimingLine<UninheritedLine>(line.Offset)?.Offset
                        ?? globalEndTimeMs
                ),
                OffsetMs = line.Offset,
                MsPerBeat = line.msPerBeat,
                Bpm = line.bpm,
                Meter = line.Meter,
                Sampleset = line.Sampleset.ToString(),
                CustomIndex = line.CustomIndex,
            })
            .Where(segment => segment.EndTimeMs > segment.StartTimeMs && segment.MsPerBeat > 0)
            .ToList();

        var breakPeriods = beatmap
            .Breaks.Select(@break => new ObjectsBreakPeriod
            {
                StartTimeMs = @break.GetRealStart(beatmap),
                EndTimeMs = @break.GetRealEnd(beatmap),
            })
            .Where(period => period.EndTimeMs > period.StartTimeMs)
            .ToList();

        var snappingCounts = SupportedSnapDivisors.ToDictionary(divisor => divisor, _ => 0);
        var snappingEdgeTimes = SupportedSnapDivisors.ToDictionary(
            divisor => divisor,
            _ => new List<double>()
        );
        var edgeCount = 0;
        var unsnappedCount = 0;
        var unsnappedEdgeTimes = new List<double>();

        foreach (var hitObject in beatmap.HitObjects)
        {
            foreach (var edgeTime in hitObject.GetEdgeTimes())
            {
                if (hitObject.type == HitObject.Types.Spinner)
                {
                    continue;
                }

                edgeCount++;

                var unsnapIssue = beatmap.GetUnsnapIssue(edgeTime);
                if (unsnapIssue != null)
                {
                    unsnappedCount++;
                    unsnappedEdgeTimes.Add(edgeTime);
                    continue;
                }

                var divisor = beatmap.GetLowestDivisor(edgeTime);
                if (snappingCounts.ContainsKey(divisor))
                {
                    snappingCounts[divisor]++;
                    snappingEdgeTimes[divisor].Add(edgeTime);
                }
            }
        }

        var totalSnapPoints = Math.Max(1, edgeCount);

        return new ObjectsOverviewDifficulty
        {
            Version = beatmap.MetadataSettings.version,
            Mode = beatmap.GeneralSettings.mode.ToString(),
            StarRating = beatmap.StarRating,
            ObjectCount = beatmap.HitObjects.Count,
            EdgeCount = edgeCount,
            UnsnappedCount = unsnappedCount,
            UnsnappedPercentage = edgeCount > 0 ? unsnappedCount * 100d / totalSnapPoints : 0,
            BreakPeriods = breakPeriods,
            TimelineObjects = timelineObjects,
            TimingSegments = timingSegments,
            UnsnappedEdgeTimesMs = unsnappedEdgeTimes,
            TimelineSamples = timelineSamples,
            HitsoundGapPeriods = hitsoundGapPeriods,
            Snappings = SupportedSnapDivisors
                .Select(divisor => new ObjectsSnappingBucket
                {
                    Divisor = divisor,
                    Label = $"1/{divisor}",
                    Count = snappingCounts[divisor],
                    Percentage =
                        edgeCount > 0 ? snappingCounts[divisor] * 100d / totalSnapPoints : 0,
                    EdgeTimesMs = snappingEdgeTimes[divisor],
                })
                .ToList(),
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

            return
                hitObject.HasHitSound(HitObject.HitSounds.Clap)
                || hitObject.HasHitSound(HitObject.HitSounds.Whistle)
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

    private static int GetObjectHitSoundFlags(HitObject hitObject)
    {
        if (hitObject is Slider slider)
            return (int)slider.StartHitSound;

        return (int)hitObject.hitSound;
    }

    private static int GetEdgeHitSoundFlags(HitObject hitObject, double edgeTime)
    {
        if (hitObject is Slider slider)
        {
            if (IsClose(hitObject.time, edgeTime))
                return (int)slider.StartHitSound;

            if (IsClose(slider.EndTime, edgeTime))
                return (int)slider.EndHitSound;

            var curveDuration = slider.GetCurveDuration();
            for (var i = 0; i < slider.ReverseHitSounds.Count; i++)
            {
                var reverseTime = hitObject.time + curveDuration * (i + 1);
                if (IsClose(reverseTime, edgeTime))
                    return (int)slider.ReverseHitSounds[i];
            }

            return 0;
        }

        if (hitObject is Spinner spinner && IsClose(spinner.endTime, edgeTime))
            return (int)hitObject.hitSound;

        return (int)hitObject.hitSound;
    }

    private static bool IsClose(double left, double right) =>
        left <= right + 2 && left >= right - 2;

    private static IEnumerable<ObjectsTimelineSample> BuildTimelineSamples(HitObject hitObject)
    {
        var objectType = hitObject.GetObjectType();
        var edgeTimes = hitObject.GetEdgeTimes().ToList();

        foreach (var sample in hitObject.usedHitSamples)
        {
            string? partName = null;
            if (sample.HitSource == HitSample.HitSourceType.Edge)
            {
                var matchedEdge = edgeTimes.FirstOrDefault(edgeTime =>
                    IsClose(edgeTime, sample.Time)
                );
                if (edgeTimes.Any(edgeTime => IsClose(edgeTime, sample.Time)))
                    partName = hitObject.GetPartName(matchedEdge);
            }

            yield return new ObjectsTimelineSample
            {
                TimeMs = sample.Time,
                Source = sample.HitSource.ToString(),
                HitSound = FormatHitSound(sample.HitSound),
                Sampleset = sample.Sampleset?.ToString() ?? "Normal",
                CustomIndex = sample.CustomIndex,
                PartName = partName,
                ObjectType = objectType,
            };
        }
    }

    private static string? FormatHitSound(HitObject.HitSounds? hitSound)
    {
        if (hitSound is null or HitObject.HitSounds.None)
            return null;

        return hitSound.ToString();
    }

    private static List<ObjectsHitsoundGapPeriod> BuildHitsoundGapPeriods(Beatmap beatmap)
    {
        const int warningTotal = 10000;
        const int warningTime = 8 * 1500;
        const int warningObject = 2 * 200;

        var gaps = new List<ObjectsHitsoundGapPeriod>();
        if (beatmap.HitObjects.Count == 0)
            return gaps;

        var gapStart = beatmap.HitObjects.First().time;
        var objectsPassed = 0;
        HitSample.SamplesetType? prevSample = null;

        void ApplyFeedbackUpdate(
            HitObject.HitSounds hitSound,
            HitSample.SamplesetType sampleset,
            double time
        )
        {
            prevSample ??= sampleset;

            if (hitSound > 0 || sampleset != prevSample)
            {
                TryRecordGap(
                    gapStart,
                    time,
                    objectsPassed,
                    warningTotal,
                    warningTime,
                    warningObject,
                    gaps
                );
                gapStart = time;
                objectsPassed = 0;
                prevSample = sampleset;
            }
            else
            {
                objectsPassed++;
            }
        }

        foreach (var hitObject in beatmap.HitObjects)
        {
            switch (hitObject)
            {
                case Circle:
                    ApplyFeedbackUpdate(
                        hitObject.hitSound,
                        hitObject.GetSampleset(),
                        hitObject.time
                    );
                    break;

                case Slider slider:
                    ApplyFeedbackUpdate(
                        slider.StartHitSound,
                        slider.GetStartSampleset(),
                        slider.time
                    );

                    for (var reverseIndex = 0; reverseIndex < slider.EdgeAmount - 1; reverseIndex++)
                    {
                        ApplyFeedbackUpdate(
                            slider.ReverseHitSounds.ElementAt(reverseIndex),
                            slider.GetReverseSampleset(reverseIndex),
                            Math.Floor(slider.time + slider.GetCurveDuration() * (reverseIndex + 1))
                        );
                    }

                    ApplyFeedbackUpdate(
                        slider.EndHitSound,
                        slider.GetEndSampleset(),
                        slider.EndTime
                    );
                    break;
            }
        }

        return gaps;
    }

    private static void TryRecordGap(
        double gapStart,
        double gapEnd,
        int objectsPassed,
        int warningTotal,
        int warningTime,
        int warningObject,
        List<ObjectsHitsoundGapPeriod> gaps
    )
    {
        var timeDifference = gapEnd - gapStart;
        if (timeDifference <= 0)
            return;

        var timeScore = timeDifference;
        var objectScore = objectsPassed * 200.0;

        if (
            timeScore + objectScore > warningTotal
            && timeScore > warningTime
            && objectScore > warningObject
        )
        {
            gaps.Add(new ObjectsHitsoundGapPeriod { StartTimeMs = gapStart, EndTimeMs = gapEnd });
        }
    }
}
