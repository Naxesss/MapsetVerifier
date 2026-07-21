using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using MapsetVerifier.Server.Model;

namespace MapsetVerifier.Server.Service.OsuRuntime;

public static class CurrentBeatmapLookupService
{
    private static readonly Regex OsuEditorWindowRegex = new(
        @"^\s*osu!(?:cuttingedge|stable|beta)?\s*(?:b\d+)?\s*-\s*(?<metadata>.+?\.osu)\s*$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase
    );
    private static readonly Regex OsuBuildTitlePrefixRegex = new(
        @"^osu!(?:cuttingedge|stable|beta)?\s*(?:b\d+)?\s*-\s*",
        RegexOptions.Compiled | RegexOptions.IgnoreCase
    );
    private static readonly Regex AnyOsuFilenameRegex = new(
        @"(?<metadata>[^\\/:*?""<>|\r\n]+?\.osu)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase
    );
    private static readonly Regex BackgroundRegex = new(
        "0,0,\"(?<file>[^\"]+)\"",
        RegexOptions.Compiled
    );
    private static readonly object StickyMetadataLock = new();
    private static readonly Dictionary<OsuClientKind, string> LastDetectedMetadataByClient = new();

    public static ApiLazerLookupResult GetCurrentLazerBeatmap() =>
        GetCurrentBeatmap(OsuClientKind.Lazer, null);

    public static ApiLazerLookupResult GetCurrentStableBeatmap(string? songsFolderOverride) =>
        GetCurrentBeatmap(OsuClientKind.Stable, songsFolderOverride);

    private static ApiLazerLookupResult GetCurrentBeatmap(
        OsuClientKind clientKind,
        string? songsFolderOverride
    )
    {
        if (!OperatingSystem.IsWindows())
            return new ApiLazerLookupResult(
                "unsupported_platform",
                "Current map lookup is only supported on Windows.",
                null,
                null,
                null,
                null
            );

        var processes = SnapshotOsuProcesses();
        if (processes.Count == 0)
            return new ApiLazerLookupResult(
                "no_process",
                "Could not detect an osu! process.",
                null,
                null,
                null,
                null
            );

        if (clientKind == OsuClientKind.Stable)
            return GetCurrentStableBeatmapFromAllOsuProcesses(processes, songsFolderOverride);

        var targetProcesses = processes.Where(p => p.ClientKind == clientKind).ToList();
        if (targetProcesses.Count == 0)
        {
            var hasUnknown = processes.Any(p => p.ClientKind == OsuClientKind.Unknown);
            if (hasUnknown)
                return new ApiLazerLookupResult(
                    "ambiguous_client",
                    $"Could not confidently identify an osu!{clientKind.ToString().ToLowerInvariant()} process while multiple osu! clients are running.",
                    null,
                    null,
                    null,
                    null
                );

            return new ApiLazerLookupResult(
                "no_process",
                $"Could not detect an osu!{clientKind.ToString().ToLowerInvariant()} process.",
                null,
                null,
                null,
                null
            );
        }

        var titleCandidates = GetWindowTitleCandidates(
            targetProcesses.Select(p => p.ProcessId).ToHashSet()
        );
        var liveMetadata = titleCandidates
            .OrderBy(c => c.ProcessId)
            .ThenBy(c => c.Title, StringComparer.OrdinalIgnoreCase)
            .Select(c => TryExtractEditorMetadata(c.Title))
            .FirstOrDefault(m => !string.IsNullOrWhiteSpace(m));

        var metadata = ResolveStickyMetadata(clientKind, liveMetadata);
        if (string.IsNullOrWhiteSpace(metadata))
            return new ApiLazerLookupResult(
                "no_editor_title",
                "Open a map in the editor first.",
                null,
                null,
                null,
                null
            );

        return ResolveLazerCurrentMap(metadata, liveMetadata);
    }

    private static ApiLazerLookupResult GetCurrentStableBeatmapFromAllOsuProcesses(
        IReadOnlyList<OsuProcessSnapshot> processes,
        string? songsFolderOverride
    )
    {
        var songsFolder = BeatmapService.ResolveSongsFolder(songsFolderOverride);
        if (string.IsNullOrWhiteSpace(songsFolder))
            return new ApiLazerLookupResult(
                "songs_folder_not_found",
                "Songs folder could not be detected.",
                null,
                null,
                null,
                null
            );

        var stableRoot = Path.GetDirectoryName(
            songsFolder.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
        );
        if (string.IsNullOrWhiteSpace(stableRoot) || !Directory.Exists(stableRoot))
            return new ApiLazerLookupResult(
                "songs_folder_not_found",
                "Songs folder path is invalid for stable lookup.",
                null,
                null,
                songsFolder,
                null
            );

        static string NormalizePath(string path) =>
            Path.GetFullPath(path)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var normalizedStableRoot = NormalizePath(stableRoot);
        var stableProcessIds = processes
            .Where(p => !string.IsNullOrWhiteSpace(p.ExecutablePath))
            .Where(p =>
            {
                var exeDir = Path.GetDirectoryName(p.ExecutablePath!);
                if (string.IsNullOrWhiteSpace(exeDir))
                    return false;
                return string.Equals(
                    NormalizePath(exeDir),
                    normalizedStableRoot,
                    StringComparison.OrdinalIgnoreCase
                );
            })
            .Select(p => p.ProcessId)
            .ToHashSet();

        if (stableProcessIds.Count == 0)
        {
            return new ApiLazerLookupResult(
                "no_process",
                $"No osu! process was detected in the stable install directory '{stableRoot}'.",
                null,
                null,
                songsFolder,
                null
            );
        }

        var titleCandidates = GetWindowTitleCandidates(stableProcessIds);
        var metadataCandidates = titleCandidates
            .OrderBy(c => c.ProcessId)
            .ThenBy(c => c.Title, StringComparer.OrdinalIgnoreCase)
            .Select(c => TryExtractEditorMetadata(c.Title))
            .OfType<string>()
            .Where(m => !string.IsNullOrWhiteSpace(m))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var liveMetadata = metadataCandidates.FirstOrDefault() ?? string.Empty;
        var metadata = ResolveStickyMetadata(OsuClientKind.Stable, liveMetadata);
        if (string.IsNullOrWhiteSpace(metadata))
            return new ApiLazerLookupResult(
                "no_editor_title",
                "osu! is running, but no editor window title with a .osu filename was detected.",
                null,
                null,
                songsFolder,
                null
            );

        string? resolvedFolder = null;
        string? resolvedMetadata = null;

        foreach (var candidate in metadataCandidates)
        {
            var folderFromCandidate = TryFindStableBeatmapSetFolder(songsFolder, candidate);
            if (string.IsNullOrWhiteSpace(folderFromCandidate))
                continue;

            resolvedFolder = folderFromCandidate;
            resolvedMetadata = candidate;
            break;
        }

        if (!string.IsNullOrWhiteSpace(resolvedFolder))
        {
            var beatmap = TryBuildBeatmapFromFolder(resolvedFolder);
            var displayMetadata = resolvedMetadata ?? metadata;
            return new ApiLazerLookupResult(
                "folder_found",
                "Current stable map found.",
                displayMetadata,
                resolvedFolder,
                songsFolder,
                beatmap
            );
        }

        var stickyResolved = TryFindStableBeatmapSetFolder(songsFolder, metadata);
        if (!string.IsNullOrWhiteSpace(stickyResolved))
        {
            var beatmap = TryBuildBeatmapFromFolder(stickyResolved);
            return new ApiLazerLookupResult(
                "folder_found",
                "Current stable map found from sticky metadata.",
                metadata,
                stickyResolved,
                songsFolder,
                beatmap
            );
        }

        var message =
            liveMetadata == null
                ? "Using the last detected metadata. Keep the map open in the editor to resolve it in Songs."
                : "Metadata detected. Could not find an exact .osu filename match in Songs yet.";
        return new ApiLazerLookupResult(
            "metadata_detected",
            message,
            metadata,
            null,
            songsFolder,
            null
        );
    }

    private static readonly Regex EditorMetadataFilenameRegex = new(
        @"^(?<artist>.+?)\s-\s(?<title>.+?)\s\((?<creator>.+?)\)\s\[(?<version>.+)\]\.osu$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase
    );

    private static readonly object MaterializeCacheLock = new();
    private static string? _lastMaterializedLazerSetId;
    private static string? _lastMaterializedLazerSignature;
    private static string? _lastMaterializedLazerFolderPath;

    /// <summary>
    /// Resolves the "currently open in the lazer editor" shortcut by matching the editor window
    /// title's metadata against osu!lazer's realm library, instead of scanning OS temp
    /// directories for a beatmap exported via "Edit externally". This means the shortcut works
    /// even when the map was opened normally (not externally), and the underlying browse/open
    /// flow no longer depends on it at all.
    /// </summary>
    private static ApiLazerLookupResult ResolveLazerCurrentMap(
        string metadata,
        string? liveMetadata
    )
    {
        var dataDir = LazerRealmService.ResolveLazerDataDirectory(null);
        if (string.IsNullOrWhiteSpace(dataDir))
            return new ApiLazerLookupResult(
                "lazer_data_dir_not_found",
                "Lazer data folder could not be detected.",
                metadata,
                null,
                null,
                null
            );

        var parsed = TryParseEditorFilenameMetadata(metadata);
        if (parsed == null)
        {
            var parseFailedMessage =
                liveMetadata == null
                    ? "Using the last detected beatmap. Its metadata could not be parsed."
                    : "Beatmap detected, but its metadata could not be parsed.";
            return new ApiLazerLookupResult(
                "metadata_detected",
                parseFailedMessage,
                metadata,
                null,
                null,
                null
            );
        }

        var match = LazerRealmService.FindBestMatchingBeatmap(
            dataDir,
            parsed.Value.Artist,
            parsed.Value.Title,
            parsed.Value.Creator,
            parsed.Value.Version
        );
        if (match == null)
        {
            var noMatchMessage =
                liveMetadata == null
                    ? "Using the last detected beatmap. Could not find a matching mapset in your lazer library yet."
                    : "Beatmap detected. Could not find a matching mapset in your lazer library yet.";
            return new ApiLazerLookupResult(
                "metadata_detected",
                noMatchMessage,
                metadata,
                null,
                null,
                null
            );
        }

        var folderPath = GetOrMaterializeCached(dataDir, match.Value.SetId);
        if (string.IsNullOrWhiteSpace(folderPath))
            return new ApiLazerLookupResult(
                "metadata_detected",
                "Beatmap matched, but its files could not be materialized.",
                metadata,
                null,
                null,
                null
            );

        var backgroundUrl = $"/beatmap/lazer/image?id={Uri.EscapeDataString(match.Value.SetId)}";
        var beatmap = new ApiBeatmap(
            match.Value.SetId,
            match.Value.Title,
            match.Value.Artist,
            match.Value.Creator,
            match.Value.BeatmapId,
            match.Value.BeatmapSetId,
            backgroundUrl
        );

        return new ApiLazerLookupResult(
            "folder_found",
            "Beatmap loaded!",
            metadata,
            folderPath,
            dataDir,
            beatmap
        );
    }

    /// <summary>
    /// Materialization is a real file copy, so avoid blindly redoing it on every 1.5-5s poll from
    /// the frontend "current map" panel. Instead, cheaply re-read the set's current file/hash
    /// manifest from realm (no file I/O) on every call and only re-copy when that manifest
    /// actually changed since the last materialize — e.g. the user saved new changes in the
    /// editor. This keeps the shortcut correct without paying for a full re-copy on every poll.
    /// </summary>
    private static string? GetOrMaterializeCached(string dataDir, string setId)
    {
        var files = LazerRealmService.GetBeatmapSetFiles(dataDir, setId);
        if (files == null)
            return null;

        var signature = string.Join(
            '|',
            files
                .OrderBy(f => f.Filename, StringComparer.Ordinal)
                .Select(f => $"{f.Filename}:{f.Hash}")
        );

        lock (MaterializeCacheLock)
        {
            if (
                _lastMaterializedLazerSetId == setId
                && _lastMaterializedLazerSignature == signature
                && !string.IsNullOrWhiteSpace(_lastMaterializedLazerFolderPath)
                && Directory.Exists(_lastMaterializedLazerFolderPath)
            )
                return _lastMaterializedLazerFolderPath;
        }

        var result = LazerBeatmapMaterializer.Materialize(dataDir, setId);
        if (!result.Success || string.IsNullOrWhiteSpace(result.FolderPath))
            return null;

        lock (MaterializeCacheLock)
        {
            _lastMaterializedLazerSetId = setId;
            _lastMaterializedLazerSignature = signature;
            _lastMaterializedLazerFolderPath = result.FolderPath;
        }

        return result.FolderPath;
    }

    private static (
        string? Artist,
        string? Title,
        string? Creator,
        string? Version
    )? TryParseEditorFilenameMetadata(string metadata)
    {
        var match = EditorMetadataFilenameRegex.Match(metadata);
        if (!match.Success)
            return null;

        return (
            match.Groups["artist"].Value.Trim(),
            match.Groups["title"].Value.Trim(),
            match.Groups["creator"].Value.Trim(),
            match.Groups["version"].Value.Trim()
        );
    }

    private static string? TryFindStableBeatmapSetFolder(string songsFolder, string metadata)
    {
        if (
            string.IsNullOrWhiteSpace(songsFolder)
            || string.IsNullOrWhiteSpace(metadata)
            || !Directory.Exists(songsFolder)
        )
            return null;

        metadata = SanitizeExtractedMetadata(metadata) ?? metadata;

        var matches = new List<string>();
        try
        {
            foreach (var folder in new DirectoryInfo(songsFolder).EnumerateDirectories())
            {
                var candidate = Path.Combine(folder.FullName, metadata);
                if (File.Exists(candidate))
                    matches.Add(candidate);
            }
        }
        catch (Exception)
        {
            return null;
        }

        var selected = matches.OrderByDescending(File.GetLastWriteTimeUtc).FirstOrDefault();
        return string.IsNullOrWhiteSpace(selected) ? null : Path.GetDirectoryName(selected);
    }

    private static string NormalizeLookupName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        var sb = new StringBuilder(value.Length);
        foreach (var c in value.ToLowerInvariant())
        {
            if (char.IsLetterOrDigit(c))
                sb.Append(c);
        }

        return sb.ToString();
    }

    private static ApiBeatmap? TryBuildBeatmapFromFolder(string folderPath)
    {
        try
        {
            if (!Directory.Exists(folderPath))
                return null;

            var folder = new DirectoryInfo(folderPath);
            var osuFile = Directory.GetFiles(folder.FullName, "*.osu").FirstOrDefault();
            if (string.IsNullOrWhiteSpace(osuFile))
                return null;

            var content = File.ReadAllText(osuFile);
            var meta = ParseBeatmapMetadata(folder.FullName, content);
            var backgroundUrl = string.IsNullOrEmpty(meta.backgroundPath)
                ? string.Empty
                : $"/beatmap/image?folder={Uri.EscapeDataString(folder.Name)}";
            return new ApiBeatmap(
                folder.Name,
                meta.title ?? string.Empty,
                meta.artist ?? string.Empty,
                meta.creator ?? string.Empty,
                meta.beatmapId ?? string.Empty,
                meta.beatmapSetId ?? string.Empty,
                backgroundUrl
            );
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static (
        string? title,
        string? artist,
        string? creator,
        string? beatmapId,
        string? beatmapSetId,
        string? backgroundPath
    ) ParseBeatmapMetadata(string folderPath, string data)
    {
        string? GetValue(string prefix)
        {
            var line = data.Split('\n')
                .FirstOrDefault(l => l.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
            if (line == null)
                return null;
            return line.Substring(prefix.Length).Trim();
        }

        var title = GetValue("Title:");
        var artist = GetValue("Artist:");
        var creator = GetValue("Creator:");
        var beatmapId = GetValue("BeatmapID:");
        var beatmapSetId = GetValue("BeatmapSetID:");

        string? backgroundPath = null;
        var match = BackgroundRegex.Match(data);
        if (match.Success)
        {
            var file = match.Groups["file"].Value;
            backgroundPath = Path.Combine(folderPath, file);
        }

        return (title, artist, creator, beatmapId, beatmapSetId, backgroundPath);
    }

    private static string? TryExtractEditorMetadata(string windowTitle)
    {
        if (string.IsNullOrWhiteSpace(windowTitle))
            return null;

        var trimmed = windowTitle.Trim();
        var strictMatch = OsuEditorWindowRegex.Match(trimmed);
        if (strictMatch.Success)
            return SanitizeExtractedMetadata(strictMatch.Groups["metadata"].Value.Trim());

        var looseMatches = AnyOsuFilenameRegex.Matches(trimmed);
        if (looseMatches.Count == 0)
            return null;

        // Prefer the right-most occurrence when titles include prefixes/suffixes.
        var loose = looseMatches[^1].Groups["metadata"].Value.Trim();
        return SanitizeExtractedMetadata(loose);
    }

    private static string? SanitizeExtractedMetadata(string? metadata)
    {
        if (string.IsNullOrWhiteSpace(metadata))
            return null;

        var trimmed = metadata.Trim();
        trimmed = OsuBuildTitlePrefixRegex.Replace(trimmed, string.Empty).Trim();
        return string.IsNullOrWhiteSpace(trimmed) ? null : trimmed;
    }

    private static string? ResolveStickyMetadata(OsuClientKind clientKind, string? liveMetadata)
    {
        lock (StickyMetadataLock)
        {
            if (!string.IsNullOrWhiteSpace(liveMetadata))
            {
                if (!LastDetectedMetadataByClient.TryGetValue(clientKind, out var oldValue))
                {
                    LastDetectedMetadataByClient[clientKind] = liveMetadata;
                }
                else
                {
                    var normalizedCurrent = NormalizeLookupName(liveMetadata);
                    var normalizedOld = NormalizeLookupName(oldValue);
                    if (!string.Equals(normalizedCurrent, normalizedOld, StringComparison.Ordinal))
                        LastDetectedMetadataByClient[clientKind] = liveMetadata;
                }
            }

            return LastDetectedMetadataByClient.TryGetValue(clientKind, out var cached)
                ? cached
                : null;
        }
    }

    private static List<OsuProcessSnapshot> SnapshotOsuProcesses()
    {
        var snapshots = new List<OsuProcessSnapshot>();
        foreach (var process in Process.GetProcesses())
        {
            try
            {
                if (!process.ProcessName.Contains("osu", StringComparison.OrdinalIgnoreCase))
                    continue;

                var kind = OsuProcessClassifier.Classify(process);
                string? executablePath = null;
                try
                {
                    executablePath = process.MainModule?.FileName;
                }
                catch (Exception)
                {
                    // Ignore process path access failures.
                }

                snapshots.Add(
                    new OsuProcessSnapshot(
                        process.Id,
                        kind,
                        executablePath,
                        process.MainWindowTitle
                    )
                );
            }
            catch (Exception)
            {
                // Ignore inaccessible process entries.
            }
            finally
            {
                process.Dispose();
            }
        }

        return snapshots;
    }

    private static List<OsuWindowTitleCandidate> GetWindowTitleCandidates(HashSet<int> processIds)
    {
        var candidates = new Dictionary<string, OsuWindowTitleCandidate>(StringComparer.Ordinal);

        foreach (var snapshot in SnapshotOsuProcesses())
        {
            if (!processIds.Contains(snapshot.ProcessId))
                continue;
            if (string.IsNullOrWhiteSpace(snapshot.MainWindowTitle))
                continue;

            var key = $"{snapshot.ProcessId}:{snapshot.MainWindowTitle}";
            candidates[key] = new OsuWindowTitleCandidate(
                snapshot.ProcessId,
                snapshot.MainWindowTitle
            );
        }

        EnumWindows(
            (hwnd, _) =>
            {
                if (!IsWindowVisible(hwnd))
                    return true;

                GetWindowThreadProcessId(hwnd, out var processId);
                if (!processIds.Contains((int)processId))
                    return true;

                var titleLength = GetWindowTextLengthW(hwnd);
                if (titleLength <= 0)
                    return true;

                var sb = new StringBuilder(titleLength + 1);
                GetWindowTextW(hwnd, sb, sb.Capacity);
                var title = sb.ToString();
                if (string.IsNullOrWhiteSpace(title))
                    return true;

                var key = $"{processId}:{title}";
                candidates[key] = new OsuWindowTitleCandidate((int)processId, title);
                return true;
            },
            IntPtr.Zero
        );

        return candidates.Values.ToList();
    }

    private readonly record struct OsuProcessSnapshot(
        int ProcessId,
        OsuClientKind ClientKind,
        string? ExecutablePath,
        string? MainWindowTitle
    );

    private readonly record struct OsuWindowTitleCandidate(int ProcessId, string Title);

    private delegate bool EnumWindowsProc(IntPtr hwnd, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern bool EnumWindows(EnumWindowsProc enumFunc, IntPtr lParam);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetWindowTextLengthW(IntPtr hWnd);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetWindowTextW(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsWindowVisible(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);
}
