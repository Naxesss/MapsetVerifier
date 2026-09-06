namespace MapsetVerifier.Server.Model;

public readonly struct ApiRunningOsuClients(bool stable, bool lazer)
{
    public bool Stable { get; } = stable;
    public bool Lazer { get; } = lazer;
}
