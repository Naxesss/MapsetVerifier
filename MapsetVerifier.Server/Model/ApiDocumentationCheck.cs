namespace MapsetVerifier.Server.Model;

public readonly struct ApiDocumentationCheck(
    int id,
    string description,
    string category,
    string subCategory,
    string author)
{
    public int Id { get; } = id;
    public string Description { get; } = description;
    public string Category { get; } = category;
    public string SubCategory { get; } = subCategory;
    public string Author { get; } = author;
}
