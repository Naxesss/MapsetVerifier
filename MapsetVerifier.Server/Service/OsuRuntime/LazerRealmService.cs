using MapsetVerifier.Server.Model;
using Realms;
using Serilog;

namespace MapsetVerifier.Server.Service.OsuRuntime;

/// <summary>
/// Reads osu!lazer's `client.realm` database directly so beatmapsets are browsable at any time,
/// without requiring the user to have a map open via the editor's "Edit externally" feature.
/// Always opened dynamic + read-only: dynamic avoids coupling to osu.Game's compiled Realm
/// schema (which changes across lazer releases), and read-only guarantees MV can never corrupt
/// the user's real lazer database.
/// </summary>
public static class LazerRealmService
{
    private const string RealmFileName = "client.realm";

    private sealed record LazerBeatmapInfo(
        string BeatmapId,
        string DifficultyName,
        string Title,
        string Artist,
        string Creator,
        long OnlineId,
        DateTimeOffset? LastLocalUpdate,
        string? BackgroundFile
    );

    private sealed record LazerBeatmapSetInfo(
        Guid Id,
        bool DeletePending,
        long OnlineId,
        DateTimeOffset SortTime,
        string Title,
        string Artist,
        string Creator,
        long FirstBeatmapOnlineId,
        List<(string Filename, string Hash)> Files,
        List<LazerBeatmapInfo> Beatmaps
    );

    private static readonly object SnapshotLock = new();
    private static string? _cachedDataDirectory;
    private static DateTime _cachedRealmWriteTimeUtc;
    private static List<LazerBeatmapSetInfo>? _cachedSets;

    /// <summary>
    /// A full library scan through the dynamic Realm API is expensive (every set + every
    /// difficulty, dynamic-dispatch property reads), so instead of re-scanning on every page
    /// load / search keystroke / image request, the scan result is cached and only rebuilt when
    /// `client.realm`'s own write time changes (i.e. the lazer library actually changed).
    /// </summary>
    private static List<LazerBeatmapSetInfo> GetSnapshot(string dataDirectory)
    {
        var realmPath = Path.Combine(dataDirectory, RealmFileName);
        var writeTimeUtc = File.GetLastWriteTimeUtc(realmPath);

        // The lock spans the whole rescan (not just the cache check) so that concurrent cold
        // callers — e.g. the beatmap list load and the "currently open beatmap" poller both
        // firing on mount — block and share one scan instead of each running their own duplicate
        // full-library scan against the same realm file at the same time.
        lock (SnapshotLock)
        {
            if (
                _cachedSets != null
                && _cachedDataDirectory == dataDirectory
                && _cachedRealmWriteTimeUtc == writeTimeUtc
            )
                return _cachedSets;

            var sets = BuildSnapshot(dataDirectory);

            _cachedDataDirectory = dataDirectory;
            _cachedRealmWriteTimeUtc = writeTimeUtc;
            _cachedSets = sets;

            return sets;
        }
    }

    private static List<LazerBeatmapSetInfo> BuildSnapshot(string dataDirectory)
    {
        var sets = new List<LazerBeatmapSetInfo>();
        using (var realm = OpenRealm(dataDirectory))
        {
            dynamic dynamicSets = realm.DynamicApi.All("BeatmapSet");
            foreach (dynamic set in dynamicSets)
            {
                bool deletePending = set.DeletePending;

                var beatmaps = new List<LazerBeatmapInfo>();
                DateTimeOffset? latestLocalUpdate = null;
                foreach (dynamic beatmap in set.Beatmaps)
                {
                    dynamic metadata = beatmap.Metadata;
                    string bmTitle = metadata?.Title ?? string.Empty;
                    string bmArtist = metadata?.Artist ?? string.Empty;
                    dynamic? author = metadata?.Author;
                    string bmCreator =
                        author != null ? (string)(author.Username ?? string.Empty) : string.Empty;
                    string bmVersion = beatmap.DifficultyName ?? string.Empty;
                    var bmOnlineId = Convert.ToInt64(beatmap.OnlineID);
                    DateTimeOffset? localUpdate = beatmap.LastLocalUpdate;
                    string? backgroundFile = metadata?.BackgroundFile;

                    if (
                        localUpdate != null
                        && (latestLocalUpdate == null || localUpdate > latestLocalUpdate)
                    )
                        latestLocalUpdate = localUpdate;

                    beatmaps.Add(
                        new LazerBeatmapInfo(
                            BeatmapId: bmOnlineId > 0 ? bmOnlineId.ToString() : string.Empty,
                            DifficultyName: bmVersion,
                            Title: bmTitle,
                            Artist: bmArtist,
                            Creator: bmCreator,
                            OnlineId: bmOnlineId,
                            LastLocalUpdate: localUpdate,
                            BackgroundFile: backgroundFile
                        )
                    );
                }

                if (beatmaps.Count == 0)
                    continue;

                var firstBeatmap = beatmaps[0];

                var files = new List<(string, string)>();
                foreach (dynamic namedFile in set.Files)
                {
                    string filename = namedFile.Filename;
                    dynamic file = namedFile.File;
                    string hash = file.Hash;
                    if (!string.IsNullOrWhiteSpace(filename) && !string.IsNullOrWhiteSpace(hash))
                        files.Add((filename, hash));
                }

                DateTimeOffset dateAdded = set.DateAdded;
                // DateAdded only reflects when the set entered the library, not when it was last
                // edited — a saved change in the editor bumps LastLocalUpdate on the difficulty
                // instead, so use whichever is more recent to sort "latest" correctly.
                var sortTime =
                    latestLocalUpdate != null && latestLocalUpdate > dateAdded
                        ? latestLocalUpdate.Value
                        : dateAdded;

                sets.Add(
                    new LazerBeatmapSetInfo(
                        Id: (Guid)set.ID,
                        DeletePending: deletePending,
                        OnlineId: Convert.ToInt64(set.OnlineID),
                        SortTime: sortTime,
                        Title: firstBeatmap.Title,
                        Artist: firstBeatmap.Artist,
                        Creator: firstBeatmap.Creator,
                        FirstBeatmapOnlineId: firstBeatmap.OnlineId,
                        Files: files,
                        Beatmaps: beatmaps
                    )
                );
            }
        }

        return sets;
    }

    public static string? DetectLazerDataDirectory()
    {
        if (!OperatingSystem.IsWindows())
            return null;

        try
        {
            var appData = Environment.GetEnvironmentVariable("APPDATA");
            if (string.IsNullOrWhiteSpace(appData))
                return null;

            var candidate = Path.Combine(appData, "osu");
            return File.Exists(Path.Combine(candidate, RealmFileName)) ? candidate : null;
        }
        catch
        {
            return null;
        }
    }

    public static string? ResolveLazerDataDirectory(string? lazerDataDirOverride)
    {
        if (
            !string.IsNullOrWhiteSpace(lazerDataDirOverride)
            && File.Exists(Path.Combine(lazerDataDirOverride, RealmFileName))
        )
            return lazerDataDirOverride;

        return DetectLazerDataDirectory();
    }

    internal static Realm OpenRealm(string dataDirectory)
    {
        var config = new RealmConfiguration(Path.Combine(dataDirectory, RealmFileName))
        {
            IsDynamic = true,
            IsReadOnly = true,
        };
        return Realm.GetInstance(config);
    }

    public static ApiBeatmapPage GetBeatmapSets(
        string dataDirectory,
        string? search,
        int page,
        int pageSize
    )
    {
        List<LazerBeatmapSetInfo> snapshot;

        try
        {
            snapshot = GetSnapshot(dataDirectory);
        }
        catch (Exception ex)
        {
            Log.Warning(
                ex,
                "Failed to read lazer beatmap sets from {DataDirectory}",
                dataDirectory
            );
            return new ApiBeatmapPage([], page, pageSize, false);
        }

        var mapped = new List<(ApiBeatmap Beatmap, DateTimeOffset SortTime)>();
        foreach (var set in snapshot)
        {
            if (set.DeletePending)
                continue;

            var id = set.Id.ToString();

            // `v` busts the 24h browser cache on /lazer/image when the set is updated
            // (URL is otherwise only keyed by set id).
            var backgroundUrl =
                $"/beatmap/lazer/image?id={Uri.EscapeDataString(id)}&v={set.SortTime.UtcTicks:x}";

            var apiBeatmap = new ApiBeatmap(
                folder: id,
                title: set.Title,
                artist: set.Artist,
                creator: set.Creator,
                beatmapId: set.FirstBeatmapOnlineId > 0
                    ? set.FirstBeatmapOnlineId.ToString()
                    : string.Empty,
                beatmapSetId: set.OnlineId > 0 ? set.OnlineId.ToString() : string.Empty,
                backgroundPath: backgroundUrl
            );

            if (!MatchesSearch(apiBeatmap, search))
                continue;

            mapped.Add((apiBeatmap, set.SortTime));
        }

        var ordered = mapped.OrderByDescending(m => m.SortTime).Select(m => m.Beatmap).ToList();

        var skipped = page * pageSize;
        var pageItems = ordered.Skip(skipped).Take(pageSize + 1).ToList();
        var hasMore = pageItems.Count > pageSize;

        return new ApiBeatmapPage(pageItems.Take(pageSize), page, pageSize, hasMore);
    }

    /// <summary>
    /// Resolves the filename + content hash of every file tracked for a beatmapset, without
    /// materializing anything to disk. Used by both the background-image endpoint (single file)
    /// and <see cref="LazerBeatmapMaterializer"/> (whole set).
    /// </summary>
    public static List<(string Filename, string Hash)>? GetBeatmapSetFiles(
        string dataDirectory,
        string beatmapSetId
    )
    {
        if (!Guid.TryParse(beatmapSetId, out var guid))
            return null;

        try
        {
            var snapshot = GetSnapshot(dataDirectory);
            var match = snapshot.FirstOrDefault(s => s.Id == guid);
            return match is { DeletePending: false } ? match.Files : null;
        }
        catch (Exception ex)
        {
            Log.Warning(
                ex,
                "Failed to read lazer beatmap set files for {BeatmapSetId} from {DataDirectory}",
                beatmapSetId,
                dataDirectory
            );
        }

        return null;
    }

    /// <summary>
    /// After delete+redownload, lazer often keeps the old set as DeletePending and imports a new
    /// GUID with the same OnlineID. Map a stale/deleted set id onto the live replacement when
    /// possible so materialize/F5 still work.
    /// </summary>
    public static string? ResolveLiveBeatmapSetId(string dataDirectory, string beatmapSetId)
    {
        if (!Guid.TryParse(beatmapSetId, out var guid))
            return null;

        try
        {
            var snapshot = GetSnapshot(dataDirectory);

            var requested = snapshot.FirstOrDefault(s => s.Id == guid);
            if (requested == null)
                return null;
            if (!requested.DeletePending)
                return beatmapSetId;
            if (requested.OnlineId <= 0)
                return null;

            var replacement = snapshot.FirstOrDefault(s =>
                !s.DeletePending && s.OnlineId == requested.OnlineId
            );
            return replacement?.Id.ToString();
        }
        catch (Exception ex)
        {
            Log.Warning(
                ex,
                "Failed to resolve live lazer beatmap set for {BeatmapSetId} from {DataDirectory}",
                beatmapSetId,
                dataDirectory
            );
        }

        return null;
    }

    /// <summary>
    /// Looks up the background filename tracked for a beatmapset's representative difficulty,
    /// without reading any file content.
    /// </summary>
    public static string? GetBackgroundFilename(string dataDirectory, string beatmapSetId)
    {
        if (!Guid.TryParse(beatmapSetId, out var guid))
            return null;

        try
        {
            var snapshot = GetSnapshot(dataDirectory);
            var set = snapshot.FirstOrDefault(s => s.Id == guid);
            if (set == null)
                return null;

            return set
                .Beatmaps.Select(b => b.BackgroundFile)
                .FirstOrDefault(f => !string.IsNullOrWhiteSpace(f));
        }
        catch (Exception ex)
        {
            Log.Warning(
                ex,
                "Failed to read lazer background filename for {BeatmapSetId} from {DataDirectory}",
                beatmapSetId,
                dataDirectory
            );
        }

        return null;
    }

    /// <summary>
    /// Matches the editor window title's parsed metadata against the realm library to resolve
    /// the "currently open" beatmap without needing the map exported to a temp folder. Title
    /// must match exactly (normalized); artist/creator/version each add confidence so the best
    /// candidate wins when multiple sets share a title. VersionToken matches the list's
    /// background cache-bust <c>v</c> (max of DateAdded / LastLocalUpdate ticks).
    /// </summary>
    public static (
        string SetId,
        string BeatmapId,
        string BeatmapSetId,
        string Title,
        string Artist,
        string Creator,
        string VersionToken
    )? FindBestMatchingBeatmap(
        string dataDirectory,
        string? artist,
        string? title,
        string? creator,
        string? version
    )
    {
        if (string.IsNullOrWhiteSpace(title))
            return null;

        var normTitle = NormalizeForMatch(title);
        var normArtist = NormalizeForMatch(artist);
        var normCreator = NormalizeForMatch(creator);
        var normVersion = NormalizeForMatch(version);

        try
        {
            var snapshot = GetSnapshot(dataDirectory);

            (
                string SetId,
                string BeatmapId,
                string BeatmapSetId,
                string Title,
                string Artist,
                string Creator,
                string VersionToken
            )? best = null;
            var bestScore = 0;

            foreach (var set in snapshot)
            {
                if (set.DeletePending)
                    continue;

                var versionToken = set.SortTime.UtcTicks.ToString("x");

                foreach (var beatmap in set.Beatmaps)
                {
                    if (NormalizeForMatch(beatmap.Title) != normTitle)
                        continue;

                    var score = 40;
                    if (
                        !string.IsNullOrEmpty(normArtist)
                        && NormalizeForMatch(beatmap.Artist) == normArtist
                    )
                        score += 25;
                    if (
                        !string.IsNullOrEmpty(normCreator)
                        && NormalizeForMatch(beatmap.Creator) == normCreator
                    )
                        score += 20;
                    if (
                        !string.IsNullOrEmpty(normVersion)
                        && NormalizeForMatch(beatmap.DifficultyName) == normVersion
                    )
                        score += 15;

                    if (score <= bestScore)
                        continue;

                    bestScore = score;
                    best = (
                        set.Id.ToString(),
                        beatmap.OnlineId > 0 ? beatmap.OnlineId.ToString() : string.Empty,
                        set.OnlineId > 0 ? set.OnlineId.ToString() : string.Empty,
                        beatmap.Title,
                        beatmap.Artist,
                        beatmap.Creator,
                        versionToken
                    );
                }
            }

            return best;
        }
        catch (Exception ex)
        {
            Log.Warning(
                ex,
                "Failed to match current lazer beatmap against realm library in {DataDirectory}",
                dataDirectory
            );
            return null;
        }
    }

    private static string NormalizeForMatch(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        var sb = new System.Text.StringBuilder(value.Length);
        foreach (var c in value.ToLowerInvariant())
        {
            if (char.IsLetterOrDigit(c))
                sb.Append(c);
        }

        return sb.ToString();
    }

    public static string ResolveFilePathFromHash(string dataDirectory, string hash) =>
        Path.Combine(dataDirectory, "files", hash[..1], hash[..2], hash);

    private static bool MatchesSearch(ApiBeatmap beatmap, string? search)
    {
        if (string.IsNullOrWhiteSpace(search))
            return true;

        var searchable =
            $"{beatmap.Title} - {beatmap.Artist} | {beatmap.Creator} ({beatmap.BeatmapId} {beatmap.BeatmapSetId})";
        return searchable.Contains(search, StringComparison.OrdinalIgnoreCase);
    }
}
