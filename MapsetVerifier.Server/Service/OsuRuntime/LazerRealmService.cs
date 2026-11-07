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

    /// <summary>
    /// One browsable beatmapset. A library can hold hundreds of thousands of difficulties and
    /// millions of files, so this is kept to what the list needs: file lists and per-difficulty
    /// details are read from `client.realm` when they're actually asked for.
    /// </summary>
    private sealed record LazerBeatmapSetInfo(
        Guid Id,
        long OnlineId,
        DateTimeOffset SortTime,
        string Title,
        string Artist,
        string Creator,
        long FirstBeatmapOnlineId,
        string? BackgroundFile
    );

    private sealed record LazerLibrarySnapshot(
        // Latest first; sets pending deletion are left out.
        List<LazerBeatmapSetInfo> Sets,
        Dictionary<Guid, LazerBeatmapSetInfo> SetsById,
        // Keyed by normalized title of any difficulty, since a set's difficulties can differ.
        Dictionary<string, List<LazerBeatmapSetInfo>> SetsByTitle
    );

    private static readonly object SnapshotLock = new();
    private static string? _cachedDataDirectory;
    private static DateTime _cachedRealmWriteTimeUtc;
    private static LazerLibrarySnapshot? _cachedSnapshot;

    /// <summary>
    /// A full library scan through the dynamic Realm API is expensive (every set + every
    /// difficulty, dynamic-dispatch property reads), so instead of re-scanning on every page
    /// load / search keystroke / image request, the scan result is cached and only rebuilt when
    /// `client.realm`'s own write time changes (i.e. the lazer library actually changed).
    /// </summary>
    private static LazerLibrarySnapshot GetSnapshot(string dataDirectory)
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
                _cachedSnapshot != null
                && _cachedDataDirectory == dataDirectory
                && _cachedRealmWriteTimeUtc == writeTimeUtc
            )
                return _cachedSnapshot;

            // Let go of the outdated snapshot first, so a rebuild doesn't hold two in memory.
            _cachedSnapshot = null;
            var snapshot = BuildSnapshot(dataDirectory);

            _cachedDataDirectory = dataDirectory;
            _cachedRealmWriteTimeUtc = writeTimeUtc;
            _cachedSnapshot = snapshot;

            return snapshot;
        }
    }

    private static LazerLibrarySnapshot BuildSnapshot(string dataDirectory)
    {
        var sets = new List<LazerBeatmapSetInfo>();
        var setsByTitle = new Dictionary<string, List<LazerBeatmapSetInfo>>();
        var titles = new HashSet<string>();

        using (var realm = OpenRealm(dataDirectory))
        {
            dynamic dynamicSets = realm.DynamicApi.All("BeatmapSet");
            foreach (dynamic set in dynamicSets)
            {
                if ((bool)set.DeletePending)
                    continue;

                titles.Clear();
                var isFirstBeatmap = true;
                var firstTitle = string.Empty;
                var firstArtist = string.Empty;
                var firstCreator = string.Empty;
                var firstOnlineId = 0L;
                string? backgroundFile = null;
                DateTimeOffset? latestLocalUpdate = null;

                foreach (dynamic beatmap in set.Beatmaps)
                {
                    dynamic metadata = beatmap.Metadata;
                    string title = metadata?.Title ?? string.Empty;
                    titles.Add(title);

                    if (isFirstBeatmap)
                    {
                        isFirstBeatmap = false;
                        firstTitle = title;
                        firstArtist = metadata?.Artist ?? string.Empty;
                        firstCreator = ReadCreator(metadata);
                        firstOnlineId = Convert.ToInt64(beatmap.OnlineID);
                    }

                    if (string.IsNullOrWhiteSpace(backgroundFile))
                        backgroundFile = metadata?.BackgroundFile;

                    DateTimeOffset? localUpdate = beatmap.LastLocalUpdate;
                    if (
                        localUpdate != null
                        && (latestLocalUpdate == null || localUpdate > latestLocalUpdate)
                    )
                        latestLocalUpdate = localUpdate;
                }

                if (isFirstBeatmap)
                    continue;

                DateTimeOffset dateAdded = set.DateAdded;
                // DateAdded only reflects when the set entered the library, not when it was last
                // edited — a saved change in the editor bumps LastLocalUpdate on the difficulty
                // instead, so use whichever is more recent to sort "latest" correctly.
                var sortTime =
                    latestLocalUpdate != null && latestLocalUpdate > dateAdded
                        ? latestLocalUpdate.Value
                        : dateAdded;

                var info = new LazerBeatmapSetInfo(
                    Id: (Guid)set.ID,
                    OnlineId: Convert.ToInt64(set.OnlineID),
                    SortTime: sortTime,
                    Title: firstTitle,
                    Artist: firstArtist,
                    Creator: firstCreator,
                    FirstBeatmapOnlineId: firstOnlineId,
                    BackgroundFile: string.IsNullOrWhiteSpace(backgroundFile)
                        ? null
                        : backgroundFile
                );
                sets.Add(info);

                foreach (var normalizedTitle in titles.Select(NormalizeForMatch).Distinct())
                {
                    if (!setsByTitle.TryGetValue(normalizedTitle, out var setsWithTitle))
                        setsByTitle[normalizedTitle] = setsWithTitle = new(1);
                    setsWithTitle.Add(info);
                }
            }
        }

        return new LazerLibrarySnapshot(
            sets.OrderByDescending(s => s.SortTime).ToList(),
            sets.ToDictionary(s => s.Id),
            setsByTitle
        );
    }

    private static string ReadCreator(dynamic? metadata)
    {
        dynamic? author = metadata?.Author;
        return author != null ? (string)(author.Username ?? string.Empty) : string.Empty;
    }

    private static List<(string Filename, string Hash)> ReadFiles(dynamic set)
    {
        var files = new List<(string, string)>();
        foreach (dynamic namedFile in set.Files)
        {
            string filename = namedFile.Filename;
            dynamic file = namedFile.File;
            string hash = file.Hash;
            if (!string.IsNullOrWhiteSpace(filename) && !string.IsNullOrWhiteSpace(hash))
                files.Add((filename, hash));
        }

        return files;
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
        LazerLibrarySnapshot snapshot;

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

        var matches = string.IsNullOrWhiteSpace(search)
            ? snapshot.Sets
            : snapshot.Sets.Where(set => MatchesSearch(set, search));

        var skipped = page * pageSize;
        var pageItems = matches.Skip(skipped).Take(pageSize + 1).Select(ToApiBeatmap).ToList();
        var hasMore = pageItems.Count > pageSize;

        return new ApiBeatmapPage(pageItems.Take(pageSize), page, pageSize, hasMore);
    }

    private static ApiBeatmap ToApiBeatmap(LazerBeatmapSetInfo set)
    {
        var id = set.Id.ToString();

        // `v` busts the 24h browser cache on /lazer/image when the set is updated
        // (URL is otherwise only keyed by set id).
        var backgroundUrl =
            $"/beatmap/lazer/image?id={Uri.EscapeDataString(id)}&v={set.SortTime.UtcTicks:x}";

        return new ApiBeatmap(
            folder: id,
            title: set.Title,
            artist: set.Artist,
            creator: set.Creator,
            beatmapId: OnlineIdText(set.FirstBeatmapOnlineId),
            beatmapSetId: OnlineIdText(set.OnlineId),
            backgroundPath: backgroundUrl
        );
    }

    private static string OnlineIdText(long onlineId) =>
        onlineId > 0 ? onlineId.ToString() : string.Empty;

    /// <summary>
    /// Resolves the filename + content hash of every file tracked for a beatmapset, without
    /// materializing anything to disk. Used by both the background-image endpoint (single file)
    /// and <see cref="LazerBeatmapMaterializer"/> (whole set). Read straight from `client.realm`:
    /// keeping these in the snapshot would be most of its size, and while lazer is running, saving
    /// in its editor doesn't reliably change the realm file's write time, so the snapshot can't be
    /// trusted to point at the hash of the latest .osu.
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
            using var realm = OpenRealm(dataDirectory);
            dynamic? set = realm.DynamicApi.Find("BeatmapSet", guid);
            if (set == null || (bool)set.DeletePending)
                return null;

            return ReadFiles(set);
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
    /// possible so materialize/F5 still work. Read straight from `client.realm` for the same
    /// reason as <see cref="GetBeatmapSetFiles"/>.
    /// </summary>
    public static string? ResolveLiveBeatmapSetId(string dataDirectory, string beatmapSetId)
    {
        if (!Guid.TryParse(beatmapSetId, out var guid))
            return null;

        try
        {
            using var realm = OpenRealm(dataDirectory);

            dynamic? requested = realm.DynamicApi.Find("BeatmapSet", guid);
            if (requested == null)
                return null;
            if (!(bool)requested.DeletePending)
                return beatmapSetId;

            long onlineId = Convert.ToInt64(requested.OnlineID);
            if (onlineId <= 0)
                return null;

            dynamic? replacement = realm
                .DynamicApi.All("BeatmapSet")
                .Filter("OnlineID == $0 AND DeletePending == false", onlineId)
                .FirstOrDefault();
            return replacement == null ? null : ((Guid)replacement.ID).ToString();
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
            return GetSnapshot(dataDirectory).SetsById.GetValueOrDefault(guid)?.BackgroundFile;
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
            if (!snapshot.SetsByTitle.TryGetValue(normTitle, out var candidates))
                return null;

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

            using var realm = OpenRealm(dataDirectory);

            foreach (var candidate in candidates)
            {
                dynamic? set = realm.DynamicApi.Find("BeatmapSet", candidate.Id);
                if (set == null || (bool)set.DeletePending)
                    continue;

                long setOnlineId = Convert.ToInt64(set.OnlineID);
                var versionToken = candidate.SortTime.UtcTicks.ToString("x");

                foreach (dynamic beatmap in set.Beatmaps)
                {
                    dynamic metadata = beatmap.Metadata;
                    string beatmapTitle = metadata?.Title ?? string.Empty;
                    if (NormalizeForMatch(beatmapTitle) != normTitle)
                        continue;

                    string beatmapArtist = metadata?.Artist ?? string.Empty;
                    string beatmapCreator = ReadCreator(metadata);
                    string difficultyName = beatmap.DifficultyName ?? string.Empty;

                    var score = 40;
                    if (
                        !string.IsNullOrEmpty(normArtist)
                        && NormalizeForMatch(beatmapArtist) == normArtist
                    )
                        score += 25;
                    if (
                        !string.IsNullOrEmpty(normCreator)
                        && NormalizeForMatch(beatmapCreator) == normCreator
                    )
                        score += 20;
                    if (
                        !string.IsNullOrEmpty(normVersion)
                        && NormalizeForMatch(difficultyName) == normVersion
                    )
                        score += 15;

                    if (score <= bestScore)
                        continue;

                    bestScore = score;
                    long beatmapOnlineId = Convert.ToInt64(beatmap.OnlineID);
                    best = (
                        candidate.Id.ToString(),
                        OnlineIdText(beatmapOnlineId),
                        OnlineIdText(setOnlineId),
                        beatmapTitle,
                        beatmapArtist,
                        beatmapCreator,
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

    private static bool MatchesSearch(LazerBeatmapSetInfo set, string search)
    {
        var searchable =
            $"{set.Title} - {set.Artist} | {set.Creator} ({OnlineIdText(set.FirstBeatmapOnlineId)} {OnlineIdText(set.OnlineId)})";
        return searchable.Contains(search, StringComparison.OrdinalIgnoreCase);
    }
}
