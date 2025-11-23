namespace MapsetVerifier.Server.Model;

public readonly struct ApiCheckDefinition(
    int id,
    string name,
    IEnumerable<string> difficulties)
{
    public int Id { get; } = id;
    public string Name { get; } = name;
    public IEnumerable<string> Difficulties { get; } = difficulties;
}

