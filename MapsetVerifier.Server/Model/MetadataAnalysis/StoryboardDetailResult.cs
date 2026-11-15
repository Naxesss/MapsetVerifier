namespace MapsetVerifier.Server.Model.MetadataAnalysis;

/// <summary>
/// What a beatmap set's storyboard is made of: the .osb shared by every difficulty and each
/// difficulty's own storyboard elements, with the images and samples they use.
/// </summary>
public class StoryboardDetailResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }

    public int SpriteCount { get; set; }
    public int AnimationCount { get; set; }
    public int SampleCount { get; set; }

    /// <summary>Sprites and animations per layer, in the order osb layers are drawn.</summary>
    public List<StoryboardLayerCount> Layers { get; set; } = [];

    /// <summary>Where storyboard elements come from: the .osb, then each difficulty with its own.</summary>
    public List<StoryboardSource> Sources { get; set; } = [];

    /// <summary>The difficulties that show the storyboard, through the .osb or their own elements.</summary>
    public List<string> UsedByDifficulties { get; set; } = [];

    public List<StoryboardImage> Images { get; set; } = [];
    public int ImageFileCount { get; set; }
    public long ImageSizeBytes { get; set; }
    public string ImageSizeFormatted { get; set; } = string.Empty;

    public List<StoryboardSampleFile> SampleFiles { get; set; } = [];
    public long SampleSizeBytes { get; set; }
    public string SampleSizeFormatted { get; set; } = string.Empty;

    /// <summary>Every sample as it plays, by time.</summary>
    public List<StoryboardSampleEvent> Samples { get; set; } = [];

    /// <summary>The song's length, to lay the samples out over.</summary>
    public double SongDurationMs { get; set; }

    public static StoryboardDetailResult CreateError(string message) =>
        new() { Success = false, ErrorMessage = message };
}

public class StoryboardLayerCount
{
    public string Layer { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class StoryboardSource
{
    /// <summary>The .osb file name, or a difficulty's name.</summary>
    public string Name { get; set; } = string.Empty;
    public bool IsOsb { get; set; }
    public int SpriteCount { get; set; }
    public int AnimationCount { get; set; }
    public int SampleCount { get; set; }
}

/// <summary>One image file a storyboard uses, or one animation's frames together.</summary>
public class StoryboardImage
{
    /// <summary>The path as written in the storyboard; for an animation, before its frame numbers.</summary>
    public string Path { get; set; } = string.Empty;

    /// <summary>The file to show it by: the image itself, or an animation's first frame.</summary>
    public string PreviewPath { get; set; } = string.Empty;

    public bool IsAnimation { get; set; }
    public int FrameCount { get; set; }

    /// <summary>How many sprites or animations use it.</summary>
    public int UseCount { get; set; }

    /// <summary>The files that exist; for an animation, all of its frames together.</summary>
    public long FileSizeBytes { get; set; }
    public string FileSizeFormatted { get; set; } = string.Empty;

    public int Width { get; set; }
    public int Height { get; set; }
    public string Resolution { get; set; } = string.Empty;

    /// <summary>Files it needs that aren't in the folder; for an animation, its missing frames.</summary>
    public int MissingFileCount { get; set; }

    /// <summary>The paths of the files it needs that aren't in the folder.</summary>
    public List<string> MissingFiles { get; set; } = [];

    public List<string> Layers { get; set; } = [];
    public List<string> Sources { get; set; } = [];
}

public class StoryboardSampleFile
{
    public string Path { get; set; } = string.Empty;
    public int UseCount { get; set; }
    public bool Exists { get; set; }
    public long FileSizeBytes { get; set; }
    public string FileSizeFormatted { get; set; } = string.Empty;
    public string FirstTimestamp { get; set; } = string.Empty;
}

public class StoryboardSampleEvent
{
    public double Time { get; set; }
    public string Timestamp { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public float Volume { get; set; }
    public string Layer { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
}
