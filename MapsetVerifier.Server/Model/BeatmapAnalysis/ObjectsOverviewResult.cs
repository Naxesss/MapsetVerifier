namespace MapsetVerifier.Server.Model.BeatmapAnalysis;

public class ObjectsOverviewResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public double StartTimeMs { get; set; }
    public double EndTimeMs { get; set; }
    public List<ObjectsOverviewDifficulty> Difficulties { get; set; } = [];

    public static ObjectsOverviewResult CreateError(string message) =>
        new() { Success = false, ErrorMessage = message };

    public static ObjectsOverviewResult CreateSuccess(
        double startTimeMs,
        double endTimeMs,
        List<ObjectsOverviewDifficulty> difficulties
    ) =>
        new()
        {
            Success = true,
            StartTimeMs = startTimeMs,
            EndTimeMs = endTimeMs,
            Difficulties = difficulties,
        };
}

public class ObjectsOverviewDifficulty
{
    public string Version { get; set; } = string.Empty;
    public string Mode { get; set; } = string.Empty;
    public double? StarRating { get; set; }
    public int ObjectCount { get; set; }
    public int EdgeCount { get; set; }
    public int UnsnappedCount { get; set; }
    public double UnsnappedPercentage { get; set; }
    public List<ObjectsBreakPeriod> BreakPeriods { get; set; } = [];
    public List<ObjectsTimelineObject> TimelineObjects { get; set; } = [];
    public List<ObjectsTimingSegment> TimingSegments { get; set; } = [];
    public List<ObjectsSnappingBucket> Snappings { get; set; } = [];
    public List<double> UnsnappedEdgeTimesMs { get; set; } = [];
    public List<ObjectsTimelineSample> TimelineSamples { get; set; } = [];
    public List<ObjectsHitsoundGapPeriod> HitsoundGapPeriods { get; set; } = [];
}

public class ObjectsBreakPeriod
{
    public double StartTimeMs { get; set; }
    public double EndTimeMs { get; set; }
}

public class ObjectsTimelineObject
{
    public double StartTimeMs { get; set; }
    public double EndTimeMs { get; set; }
    public string ObjectType { get; set; } = string.Empty;
    public bool HasFinishHitSound { get; set; }
    public int HitSoundFlags { get; set; }
    public int? ComboColourIndex { get; set; }
    public string? ComboColourHex { get; set; }
    public List<ObjectsTimelineEdge> Edges { get; set; } = [];
}

public class ObjectsTimelineEdge
{
    public double TimeMs { get; set; }
    public string PartName { get; set; } = string.Empty;
    public int HitSoundFlags { get; set; }
}

public class ObjectsTimelineSample
{
    public double TimeMs { get; set; }
    public string Source { get; set; } = string.Empty;
    public string? HitSound { get; set; }
    public string Sampleset { get; set; } = string.Empty;
    public int CustomIndex { get; set; }
    public string? PartName { get; set; }
    public string? ObjectType { get; set; }
}

public class ObjectsHitsoundGapPeriod
{
    public double StartTimeMs { get; set; }
    public double EndTimeMs { get; set; }
}

public class ObjectsTimingSegment
{
    public double StartTimeMs { get; set; }
    public double EndTimeMs { get; set; }
    public double OffsetMs { get; set; }
    public double MsPerBeat { get; set; }
    public double Bpm { get; set; }
    public int Meter { get; set; }
    public string Sampleset { get; set; } = string.Empty;
    public int CustomIndex { get; set; }
}

public class ObjectsSnappingBucket
{
    public int Divisor { get; set; }
    public string Label { get; set; } = string.Empty;
    public int Count { get; set; }
    public double Percentage { get; set; }
    public List<double> EdgeTimesMs { get; set; } = [];
}
