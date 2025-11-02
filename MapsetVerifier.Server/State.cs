using MapsetVerifier.Parser.Objects;

namespace MapsetVerifier.Server
{
    public static class State
    {
        public static BeatmapSet LoadedBeatmapSet { get; set; }
        public static string LoadedBeatmapSetPath { get; set; }
    }
}