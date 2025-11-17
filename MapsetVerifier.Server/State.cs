using MapsetVerifier.Parser.Objects;

namespace MapsetVerifier.Server
{
    public static class State
    {
        public static BeatmapSet LoadedBeatmapSet { get; set; } = null!;
        public static string LoadedBeatmapSetPath { get; set; } = null!;
    }
}