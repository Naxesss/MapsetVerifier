namespace MapsetVerifier.RankingCriteria;

/// <summary> Locates MapsetVerifier.RankingCriteria/Data in a source checkout, for tooling that edits it. </summary>
public static class RcDataDirectory
{
    /// <summary> Finds the data directory by walking up until MapsetVerifier.slnx is found. </summary>
    public static string Find()
    {
        foreach (var start in new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory })
            for (var dir = new DirectoryInfo(start); dir != null; dir = dir.Parent)
                if (File.Exists(Path.Combine(dir.FullName, "MapsetVerifier.slnx")))
                    return Path.Combine(dir.FullName, "MapsetVerifier.RankingCriteria", "Data");

        throw new InvalidOperationException(
            "Could not find the repository root (MapsetVerifier.slnx)."
        );
    }
}
