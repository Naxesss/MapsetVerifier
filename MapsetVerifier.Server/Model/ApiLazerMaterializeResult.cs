namespace MapsetVerifier.Server.Model;

public readonly struct ApiLazerMaterializeResult(
    bool success,
    string? folderPath,
    string? errorMessage
)
{
    public bool Success { get; } = success;
    public string? FolderPath { get; } = folderPath;
    public string? ErrorMessage { get; } = errorMessage;

    public static ApiLazerMaterializeResult SuccessResult(string folderPath) =>
        new(true, folderPath, null);

    public static ApiLazerMaterializeResult Error(string message) => new(false, null, message);
}
