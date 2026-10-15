namespace MapsetVerifier.Framework.Objects
{
    public class CustomCheckPluginInfo
    {
        public required string FileName { get; init; }
        public required string FilePath { get; init; }
        public required string AssemblyName { get; init; }
        public string? Version { get; init; }
        public string[] Authors { get; init; } = [];
        public int CheckCount { get; init; }
        public int GeneralCheckCount { get; init; }
        public int BeatmapCheckCount { get; init; }
        public int BeatmapSetCheckCount { get; init; }
        public string[] CheckNames { get; init; } = [];
    }
}
