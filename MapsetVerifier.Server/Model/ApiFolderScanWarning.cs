namespace MapsetVerifier.Server.Model;

/// <summary>
/// Set on a Songs page when sorting by last edit didn't finish in time and the page is ordered by
/// folder dates instead. <see cref="SlowFolders"/> lists folders still being read, slowest first;
/// it is empty when the library as a whole is just slow to scan.
/// </summary>
public readonly struct ApiFolderScanWarning(IEnumerable<ApiSlowFolder> slowFolders)
{
    public IEnumerable<ApiSlowFolder> SlowFolders { get; } = slowFolders;
}

public readonly struct ApiSlowFolder(string folder, double seconds)
{
    public string Folder { get; } = folder;
    public double Seconds { get; } = seconds;
}
