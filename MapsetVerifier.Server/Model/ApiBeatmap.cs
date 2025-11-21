using System.Text.Json.Serialization;

namespace MapsetVerifier.Server.Model;

public readonly struct ApiBeatmap(
    string folder,
    string title,
    string artist,
    string creator,
    string beatmapId,
    string beatmapSetId,
    string backgroundPath)
{
    public string Folder { get; } = folder;
    public string Title { get; } = title;
    public string Artist { get; } = artist;
    public string Creator { get; } = creator;
    [JsonPropertyName("beatmapID")] public string BeatmapId { get; } = beatmapId;
    [JsonPropertyName("beatmapSetID")] public string BeatmapSetId { get; } = beatmapSetId;
    public string BackgroundPath { get; } = backgroundPath;
}

