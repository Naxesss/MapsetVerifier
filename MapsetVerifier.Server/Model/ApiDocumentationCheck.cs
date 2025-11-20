using MapsetVerifier.Framework.Objects;

namespace MapsetVerifier.Server.Model;

public readonly struct ApiDocumentationCheck(
    int id,
    string description,
    string category,
    string subCategory,
    string author,
    IEnumerable<Issue.Level> outcomes)
{
    public int Id { get; } = id;
    public string Description { get; } = description;
    public string Category { get; } = category;
    public string SubCategory { get; } = subCategory;
    public string Author { get; } = author;
    public IEnumerable<Issue.Level> Outcomes { get; } = outcomes;
}
