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
        List<(ApiBeatmap Beatmap, DateTimeOffset SortTime)> mapped;

        try
        {
            using var realm = OpenRealm(dataDirectory);
            mapped = new List<(ApiBeatmap, DateTimeOffset)>();

            dynamic sets = realm.DynamicApi.All("BeatmapSet");
            foreach (dynamic set in sets)
            {
                bool deletePending = set.DeletePending;
                if (deletePending)
                    continue;

                dynamic? firstBeatmap = null;
                DateTimeOffset? latestLocalUpdate = null;
                foreach (dynamic beatmap in set.Beatmaps)
                {
                    firstBeatmap ??= beatmap;

                    DateTimeOffset? localUpdate = beatmap.LastLocalUpdate;
                    if (
                        localUpdate != null
                        && (latestLocalUpdate == null || localUpdate > latestLocalUpdate)
                    )
                        latestLocalUpdate = localUpdate;
                }
                if (firstBeatmap == null)
                    continue;

                dynamic metadata = firstBeatmap.Metadata;
                if (metadata == null)
                    continue;

                var id = ((Guid)set.ID).ToString();
                string title = metadata.Title ?? string.Empty;
                string artist = metadata.Artist ?? string.Empty;

                dynamic author = metadata.Author;
                string creator =
                    author != null ? (string)(author.Username ?? string.Empty) : string.Empty;

                var beatmapSetOnlineId = Convert.ToInt64(set.OnlineID);
                var beatmapOnlineId = Convert.ToInt64(firstBeatmap.OnlineID);

                DateTimeOffset dateAdded = set.DateAdded;
                // DateAdded only reflects when the set entered the library, not when it was last
                // edited — a saved change in the editor bumps LastLocalUpdate on the difficulty
                // instead, so use whichever is more recent to sort "latest" correctly.
                var sortTime =
                    latestLocalUpdate != null && latestLocalUpdate > dateAdded
                        ? latestLocalUpdate.Value
                        : dateAdded;

                // `v` busts the 24h browser cache on /lazer/image when the set is updated
                // (URL is otherwise only keyed by set id).
                var backgroundUrl =
                    $"/beatmap/lazer/image?id={Uri.EscapeDataString(id)}&v={sortTime.UtcTicks:x}";

                var apiBeatmap = new ApiBeatmap(
                    folder: id,
                    title: title,
                    artist: artist,
                    creator: creator,
                    beatmapId: beatmapOnlineId > 0 ? beatmapOnlineId.ToString() : string.Empty,
                    beatmapSetId: beatmapSetOnlineId > 0
                        ? beatmapSetOnlineId.ToString()
                        : string.Empty,
                    backgroundPath: backgroundUrl
                );

                if (!MatchesSearch(apiBeatmap, search))
                    continue;

                mapped.Add((apiBeatmap, sortTime));
            }
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
            using var realm = OpenRealm(dataDirectory);
            dynamic sets = realm.DynamicApi.All("BeatmapSet");
            foreach (dynamic set in sets)
            {
                Guid id = set.ID;
                if (id != guid)
                    continue;

                bool deletePending = set.DeletePending;
                if (deletePending)
                    return null;

                var result = new List<(string, string)>();
                foreach (dynamic namedFile in set.Files)
                {
                    string filename = namedFile.Filename;
                    dynamic file = namedFile.File;
                    string hash = file.Hash;
                    if (!string.IsNullOrWhiteSpace(filename) && !string.IsNullOrWhiteSpace(hash))
                        result.Add((filename, hash));
                }

                return result;
            }
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
            using var realm = OpenRealm(dataDirectory);
            dynamic sets = realm.DynamicApi.All("BeatmapSet");

            long onlineId = 0;
            var foundRequested = false;

            foreach (dynamic set in sets)
            {
                Guid id = set.ID;
                if (id != guid)
                    continue;

                foundRequested = true;
                bool deletePending = set.DeletePending;
                if (!deletePending)
                    return beatmapSetId;

                onlineId = Convert.ToInt64(set.OnlineID);
                break;
            }

            if (!foundRequested || onlineId <= 0)
                return null;

            foreach (dynamic set in sets)
            {
                bool deletePending = set.DeletePending;
                if (deletePending)
                    continue;

                if (Convert.ToInt64(set.OnlineID) != onlineId)
                    continue;

                return ((Guid)set.ID).ToString();
            }
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
            using var realm = OpenRealm(dataDirectory);
            dynamic sets = realm.DynamicApi.All("BeatmapSet");
            foreach (dynamic set in sets)
            {
                Guid id = set.ID;
                if (id != guid)
                    continue;

                foreach (dynamic beatmap in set.Beatmaps)
                {
                    dynamic metadata = beatmap.Metadata;
                    if (metadata == null)
                        continue;

                    string? backgroundFile = metadata.BackgroundFile;
                    if (!string.IsNullOrWhiteSpace(backgroundFile))
                        return backgroundFile;
                }

                return null;
            }
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
            using var realm = OpenRealm(dataDirectory);

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

            dynamic sets = realm.DynamicApi.All("BeatmapSet");
            foreach (dynamic set in sets)
            {
                bool deletePending = set.DeletePending;
                if (deletePending)
                    continue;

                DateTimeOffset? latestLocalUpdate = null;
                foreach (dynamic beatmap in set.Beatmaps)
                {
                    DateTimeOffset? localUpdate = beatmap.LastLocalUpdate;
                    if (
                        localUpdate != null
                        && (latestLocalUpdate == null || localUpdate > latestLocalUpdate)
                    )
                        latestLocalUpdate = localUpdate;
                }

                DateTimeOffset dateAdded = set.DateAdded;
                var sortTime =
                    latestLocalUpdate != null && latestLocalUpdate > dateAdded
                        ? latestLocalUpdate.Value
                        : dateAdded;
                var versionToken = sortTime.UtcTicks.ToString("x");

                foreach (dynamic beatmap in set.Beatmaps)
                {
                    dynamic metadata = beatmap.Metadata;
                    if (metadata == null)
                        continue;

                    string bmTitle = metadata.Title ?? string.Empty;
                    if (NormalizeForMatch(bmTitle) != normTitle)
                        continue;

                    string bmArtist = metadata.Artist ?? string.Empty;
                    dynamic author = metadata.Author;
                    string bmCreator =
                        author != null ? (string)(author.Username ?? string.Empty) : string.Empty;
                    string bmVersion = beatmap.DifficultyName ?? string.Empty;

                    var score = 40;
                    if (
                        !string.IsNullOrEmpty(normArtist)
                        && NormalizeForMatch(bmArtist) == normArtist
                    )
                        score += 25;
                    if (
                        !string.IsNullOrEmpty(normCreator)
                        && NormalizeForMatch(bmCreator) == normCreator
                    )
                        score += 20;
                    if (
                        !string.IsNullOrEmpty(normVersion)
                        && NormalizeForMatch(bmVersion) == normVersion
                    )
                        score += 15;

                    if (score <= bestScore)
                        continue;

                    bestScore = score;
                    var setId = ((Guid)set.ID).ToString();
                    var beatmapSetOnlineId = Convert.ToInt64(set.OnlineID);
                    var beatmapOnlineId = Convert.ToInt64(beatmap.OnlineID);
                    best = (
                        setId,
                        beatmapOnlineId > 0 ? beatmapOnlineId.ToString() : string.Empty,
                        beatmapSetOnlineId > 0 ? beatmapSetOnlineId.ToString() : string.Empty,
                        bmTitle,
                        bmArtist,
                        bmCreator,
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
