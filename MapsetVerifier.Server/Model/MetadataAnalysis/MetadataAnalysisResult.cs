namespace MapsetVerifier.Server.Model.MetadataAnalysis;

public class MetadataAnalysisResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    
    // Metadata
    public List<DifficultyMetadata> Difficulties { get; set; } = [];
    
    // Resources
    public ResourcesInfo Resources { get; set; } = new();
    
    // Colour Settings
    public List<DifficultyColourSettings> ColourSettings { get; set; } = [];

    public static MetadataAnalysisResult CreateError(string message) => new()
    {
        Success = false,
        ErrorMessage = message
    };

    public static MetadataAnalysisResult CreateSuccess(
        List<DifficultyMetadata> difficulties,
        ResourcesInfo resources,
        List<DifficultyColourSettings> colourSettings) => new()
    {
        Success = true,
        Difficulties = difficulties,
        Resources = resources,
        ColourSettings = colourSettings
    };
}

public class DifficultyMetadata
{
    public string Version { get; set; } = string.Empty;
    public string Artist { get; set; } = string.Empty;
    public string ArtistUnicode { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string TitleUnicode { get; set; } = string.Empty;
    public string Creator { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string Tags { get; set; } = string.Empty;
    public ulong? BeatmapId { get; set; }
    public ulong? BeatmapSetId { get; set; }
    public string Mode { get; set; } = string.Empty;
    public double? StarRating { get; set; }
}

public class ResourcesInfo
{
    public List<HitSoundUsage> HitSounds { get; set; } = [];
    public List<BackgroundInfo> Backgrounds { get; set; } = [];
    public List<VideoInfo> Videos { get; set; } = [];
    public StoryboardInfo Storyboard { get; set; } = new();
    public AudioFileInfo? AudioFile { get; set; }
    public long TotalFolderSizeBytes { get; set; }
    public string TotalFolderSizeFormatted { get; set; } = string.Empty;
}

public class HitSoundUsage
{
    public string FileName { get; set; } = string.Empty;
    public string Format { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public string FileSizeFormatted { get; set; } = string.Empty;
    public double DurationMs { get; set; }
    public int TotalUsageCount { get; set; }
    public List<DifficultyHitSoundUsage> UsagePerDifficulty { get; set; } = [];
}

public class DifficultyHitSoundUsage
{
    public string Version { get; set; } = string.Empty;
    public int UsageCount { get; set; }
    public List<string> Timestamps { get; set; } = [];
}

public class BackgroundInfo
{
    public string FileName { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public string FileSizeFormatted { get; set; } = string.Empty;
    public int Width { get; set; }
    public int Height { get; set; }
    public string Resolution { get; set; } = string.Empty;
    public List<string> UsedByDifficulties { get; set; } = [];
}

public class VideoInfo
{
    public string FileName { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public string FileSizeFormatted { get; set; } = string.Empty;
    public int Width { get; set; }
    public int Height { get; set; }
    public string Resolution { get; set; } = string.Empty;
    public double DurationMs { get; set; }
    public string DurationFormatted { get; set; } = string.Empty;
    public int OffsetMs { get; set; }
    public List<string> UsedByDifficulties { get; set; } = [];
}

public class StoryboardInfo
{
    public bool HasOsb { get; set; }
    public string? OsbFileName { get; set; }
    public bool OsbIsUsed { get; set; }
    public List<DifficultyStoryboardInfo> DifficultySpecificStoryboards { get; set; } = [];
}

public class DifficultyStoryboardInfo
{
    public string Version { get; set; } = string.Empty;
    public bool HasStoryboard { get; set; }
    public int SpriteCount { get; set; }
    public int AnimationCount { get; set; }
    public int SampleCount { get; set; }
}

public class AudioFileInfo
{
    public string FileName { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public string FileSizeFormatted { get; set; } = string.Empty;
    public double DurationMs { get; set; }
    public string DurationFormatted { get; set; } = string.Empty;
    public string Format { get; set; } = string.Empty;
    public double AverageBitrate { get; set; }
}

