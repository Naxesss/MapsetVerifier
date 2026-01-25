namespace MapsetVerifier.Server.Model.BeatmapAnalysis;

public class BeatmapAnalysisResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    
    public List<DifficultyStatistics> Statistics { get; set; } = [];
    public List<DifficultyGeneralSettings> GeneralSettings { get; set; } = [];
    public List<DifficultyDifficultySettings> DifficultySettings { get; set; } = [];

    public static BeatmapAnalysisResult CreateError(string message) => new()
    {
        Success = false,
        ErrorMessage = message
    };

    public static BeatmapAnalysisResult CreateSuccess(
        List<DifficultyStatistics> statistics,
        List<DifficultyGeneralSettings> generalSettings,
        List<DifficultyDifficultySettings> difficultySettings) => new()
    {
        Success = true,
        Statistics = statistics,
        GeneralSettings = generalSettings,
        DifficultySettings = difficultySettings
    };
}

public class DifficultyStatistics
{
    public string Version { get; set; } = string.Empty;
    public string Mode { get; set; } = string.Empty;
    public double? StarRating { get; set; }
    
    // Object counts
    public int CircleCount { get; set; }
    public int? SliderCount { get; set; } // null for Mania
    public int? SpinnerCount { get; set; } // null for Mania
    public int? HoldNoteCount { get; set; } // null for non-Mania
    
    // Mania-specific
    public List<int>? ObjectsPerColumn { get; set; } // null for non-Mania
    public int ColumnCount { get; set; }
    
    // Additional stats
    public int NewComboCount { get; set; }
    public int BreakCount { get; set; }
    public int UninheritedLineCount { get; set; }
    public int InheritedLineCount { get; set; }
    public double KiaiTimeMs { get; set; }
    public string KiaiTimeFormatted { get; set; } = string.Empty;
    public double DrainTimeMs { get; set; }
    public string DrainTimeFormatted { get; set; } = string.Empty;
    public double PlayTimeMs { get; set; }
    public string PlayTimeFormatted { get; set; } = string.Empty;
}

public class DifficultyGeneralSettings
{
    public string Version { get; set; } = string.Empty;
    public string Mode { get; set; } = string.Empty;
    
    public string AudioFileName { get; set; } = string.Empty;
    public float AudioLeadIn { get; set; }
    public string? StackLeniency { get; set; } // null/"N/A" for non-Standard
    
    // Countdown
    public bool HasCountdown { get; set; }
    public string? CountdownSpeed { get; set; } // None/Normal/Half/Double, null if no countdown
    public int? CountdownOffset { get; set; } // null if no countdown
    
    public bool LetterboxInBreaks { get; set; }
    public bool WidescreenStoryboard { get; set; }
    public float PreviewTime { get; set; }
    public string PreviewTimeFormatted { get; set; } = string.Empty;
    
    // Storyboard-related
    public string? UseSkinSprites { get; set; } // null/"N/A" if no storyboard
    public string SkinPreference { get; set; } = string.Empty;
    
    // Optional
    public string? EpilepsyWarning { get; set; } // null/"N/A" if no video/storyboard
}

public class DifficultyDifficultySettings
{
    public string Version { get; set; } = string.Empty;
    public string Mode { get; set; } = string.Empty;
    
    public float HpDrain { get; set; }
    public string? CircleSize { get; set; } // null/"N/A" for Taiko
    public float OverallDifficulty { get; set; }
    public string? ApproachRate { get; set; } // null/"N/A" for Mania
    public string? SliderTickRate { get; set; } // null/"N/A" for Mania
    public string? SliderVelocity { get; set; } // null/"N/A" for Mania
}

