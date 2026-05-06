using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Parser.Objects;

namespace MapsetVerifier.Checks.Tests;

public sealed class CheckTestContext : IDisposable
{
    private readonly string tempPath;

    private CheckTestContext(string tempPath)
    {
        this.tempPath = tempPath;
        BeatmapSet = new BeatmapSet(tempPath);
    }

    public BeatmapSet BeatmapSet { get; }

    public static CheckTestContext Create(string fixtureName)
    {
        var sourcePath = Path.Combine(AppContext.BaseDirectory, "TestData", "Beatmaps", fixtureName);
        if (!Directory.Exists(sourcePath))
            throw new DirectoryNotFoundException($"Beatmap fixture '{fixtureName}' was not found at '{sourcePath}'.");

        var tempPath = Path.Combine(Path.GetTempPath(), "MapsetVerifierChecksTests", fixtureName, Guid.NewGuid().ToString("N"));
        CopyDirectory(sourcePath, tempPath);

        return new CheckTestContext(tempPath);
    }

    public static CheckTestContext CreateFromOsuFiles(IEnumerable<(string FileName, string Content)> osuFiles, IEnumerable<string>? extraFiles = null)
    {
        var tempPath = Path.Combine(Path.GetTempPath(), "MapsetVerifierChecksTests", "Generated", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempPath);

        foreach (var (fileName, content) in osuFiles)
            File.WriteAllText(Path.Combine(tempPath, fileName), content);

        if (extraFiles != null)
        {
            foreach (var file in extraFiles)
                File.WriteAllText(Path.Combine(tempPath, file), string.Empty);
        }

        return new CheckTestContext(tempPath);
    }

    public Beatmap GetBeatmap(string difficultyName) =>
        BeatmapSet.Beatmaps.FirstOrDefault(beatmap => beatmap.MetadataSettings.version == difficultyName)
        ?? throw new InvalidOperationException($"Beatmap '{difficultyName}' was not found in fixture '{BeatmapSet}'.");

    public List<Issue> RunBeatmapCheck<TCheck>(string difficultyName)
        where TCheck : BeatmapCheck, new() =>
        new TCheck().GetIssues(GetBeatmap(difficultyName)).ToList();

    public List<Issue> RunBeatmapSetCheck<TCheck>()
        where TCheck : BeatmapSetCheck, new() =>
        new TCheck().GetIssues(BeatmapSet).ToList();

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(tempPath))
                Directory.Delete(tempPath, true);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    private static void CopyDirectory(string sourcePath, string destinationPath)
    {
        Directory.CreateDirectory(destinationPath);

        foreach (var directory in Directory.GetDirectories(sourcePath, "*", SearchOption.AllDirectories))
            Directory.CreateDirectory(directory.Replace(sourcePath, destinationPath));

        foreach (var file in Directory.GetFiles(sourcePath, "*", SearchOption.AllDirectories))
            File.Copy(file, file.Replace(sourcePath, destinationPath), true);
    }
}