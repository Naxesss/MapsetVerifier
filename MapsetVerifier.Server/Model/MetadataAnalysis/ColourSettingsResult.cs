namespace MapsetVerifier.Server.Model.MetadataAnalysis;

public class DifficultyColourSettings
{
    public string Version { get; set; } = string.Empty;
    public string Mode { get; set; } = string.Empty;
    public bool IsApplicable { get; set; } = true;
    public List<ComboColourInfo> ComboColours { get; set; } = [];
    public ColourInfo? SliderBorder { get; set; }
    public ColourInfo? SliderTrack { get; set; }
}

public class ComboColourInfo
{
    public int Index { get; set; }
    public int R { get; set; }
    public int G { get; set; }
    public int B { get; set; }
    public string Hex { get; set; } = string.Empty;
    public float HspLuminosity { get; set; }
    public string LuminosityWarning { get; set; } = string.Empty;
}

public class ColourInfo
{
    public int R { get; set; }
    public int G { get; set; }
    public int B { get; set; }
    public string Hex { get; set; } = string.Empty;
    public float HspLuminosity { get; set; }
    public string LuminosityWarning { get; set; } = string.Empty;
}

public class KiaiColourAnalysis
{
    public string Version { get; set; } = string.Empty;
    public List<ColourUsageInfo> KiaiColours { get; set; } = [];
    public List<ColourUsageInfo> NonKiaiColours { get; set; } = [];
}

public class ColourUsageInfo
{
    public int ComboIndex { get; set; }
    public int R { get; set; }
    public int G { get; set; }
    public int B { get; set; }
    public string Hex { get; set; } = string.Empty;
    public int UsageCount { get; set; }
}

