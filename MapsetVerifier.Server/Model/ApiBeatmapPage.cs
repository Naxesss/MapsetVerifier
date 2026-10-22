namespace MapsetVerifier.Server.Model;

public readonly struct ApiBeatmapPage(
    IEnumerable<ApiBeatmap> items,
    int page,
    int pageSize,
    bool hasMore,
    ApiFolderScanWarning? scanWarning = null
)
{
    public IEnumerable<ApiBeatmap> Items { get; } = items;
    public int Page { get; } = page;
    public int PageSize { get; } = pageSize;
    public bool HasMore { get; } = hasMore;
    public ApiFolderScanWarning? ScanWarning { get; } = scanWarning;
}
