namespace MapsetVerifier.Server.Model;

public class ApiLoadedPlugin
{
    public string FileName { get; set; } = "";
    public string FilePath { get; set; } = "";
    public string? AssemblyName { get; set; }
    public string? Version { get; set; }
    public string[] Authors { get; set; } = [];
    public int CheckCount { get; set; }
    public int GeneralCheckCount { get; set; }
    public int BeatmapCheckCount { get; set; }
    public int BeatmapSetCheckCount { get; set; }
    public string[] CheckNames { get; set; } = [];
}
