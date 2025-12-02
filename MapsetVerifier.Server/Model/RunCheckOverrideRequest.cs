using MapsetVerifier.Parser.Objects;

namespace MapsetVerifier.Server.Model;

public class RunCheckOverrideRequest
{
    public string Folder { get; set; } = string.Empty;
    public string DifficultyName { get; set; } = string.Empty;
    public Beatmap.Difficulty OverrideDifficulty { get; set; }
}

