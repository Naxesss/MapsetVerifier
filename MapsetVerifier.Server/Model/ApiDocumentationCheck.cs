using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Parser.Objects;

namespace MapsetVerifier.Server.Model;

public readonly struct ApiDocumentationCheck(
    int id,
    string description,
    string category,
    Beatmap.Mode[] modes,
    string author,
    IEnumerable<Issue.Level> outcomes)
{
    public int Id { get; } = id;
    public string Description { get; } = description;
    public string Category { get; } = category;
    public Beatmap.Mode[] Modes { get; } = modes;
    public string Author { get; } = author;
    public IEnumerable<Issue.Level> Outcomes { get; } = outcomes;
}
