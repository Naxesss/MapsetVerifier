namespace MapsetVerifier.Server.Model;

public readonly struct ApiBeatmapInfo(
    string? title,
    string? artist,
    string? creator,
    ulong? beatmapSetId
)
{
    public string? Title { get; } = title;
    public string? Artist { get; } = artist;
    public string? Creator { get; } = creator;
    public ulong? BeatmapSetId { get; } = beatmapSetId;
}
