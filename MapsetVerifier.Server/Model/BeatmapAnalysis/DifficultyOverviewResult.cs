namespace MapsetVerifier.Server.Model.BeatmapAnalysis;

public class DifficultyOverviewResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public int MsPerPeak { get; set; }
    public List<DifficultyOverviewDifficulty> Difficulties { get; set; } = [];

    public static DifficultyOverviewResult CreateError(string message) =>
        new() { Success = false, ErrorMessage = message };

    public static DifficultyOverviewResult CreateSuccess(
        int msPerPeak,
        List<DifficultyOverviewDifficulty> difficulties
    ) =>
        new()
        {
            Success = true,
            MsPerPeak = msPerPeak,
            Difficulties = difficulties,
        };
}

public class DifficultyOverviewDifficulty
{
    public string Label { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string Mode { get; set; } = string.Empty;
    public string DifficultyLevel { get; set; } = string.Empty;
    public double StarRating { get; set; }
    public List<double> StarRatingValues { get; set; } = [];
    public List<double> SliderVelocityValues { get; set; } = [];
    public List<double> VolumeValues { get; set; } = [];
    public List<DifficultySkillData> Skills { get; set; } = [];
}

public class DifficultySkillData
{
    public string SkillName { get; set; } = string.Empty;
    public List<double> StrainPeaks { get; set; } = [];
}
