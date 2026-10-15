namespace MapsetVerifier.Server.Model
{
    public class MaterializeLazerBeatmapRequest
    {
        public string BeatmapSetId { get; set; } = string.Empty;
        public string? LazerDataDir { get; set; }
    }
}
