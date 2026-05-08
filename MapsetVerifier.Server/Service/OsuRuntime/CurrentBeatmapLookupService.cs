using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using MapsetVerifier.Server.Model;

namespace MapsetVerifier.Server.Service.OsuRuntime;

public static class CurrentBeatmapLookupService
{
    private static readonly Regex OsuEditorWindowRegex = new(
        @"^\s*osu!\s*-\s*(?<metadata>.+?\.osu)\s*$",
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

        return ResolveLazerCurrentMap(metadata, liveMetadata, targetProcesses);
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

    private static ApiLazerLookupResult ResolveLazerCurrentMap(
        string metadata,
        string? liveMetadata,
        IReadOnlyList<OsuProcessSnapshot> lazerProcesses
    )
    {
        var resolved = TryFindLazerTempBeatmapFolder(metadata, lazerProcesses);
        if (resolved == null)
        {
            var message =
                liveMetadata == null
                    ? "Using the last detected beatmap. In the osu! editor, go to File → Edit externally to load the beatmap."
                    : "Beatmap detected. In the osu! editor, go to File → Edit externally to load the beatmap.";

            return new ApiLazerLookupResult(
                "metadata_detected",
                message,
                metadata,
                null,
                null,
                null
            );
        }

        var beatmap = TryBuildBeatmapFromFolder(resolved.Value.FolderPath);
        return new ApiLazerLookupResult(
            "folder_found",
            "Beatmap loaded!",
            metadata,
            resolved.Value.FolderPath,
            resolved.Value.LookupRoot,
            beatmap
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

    private static (string FolderPath, string LookupRoot)? TryFindLazerTempBeatmapFolder(
        string metadata,
        IReadOnlyList<OsuProcessSnapshot> lazerProcesses
    )
    {
        foreach (var root in GetLazerTempRoots(lazerProcesses))
        {
            var exact = TryFindExactMetadataOsuFile(root, metadata);
            if (string.IsNullOrWhiteSpace(exact))
                continue;

            var parentFolder = Path.GetDirectoryName(exact);
            if (!string.IsNullOrWhiteSpace(parentFolder))
                return (parentFolder, root);
        }

        var metadataNoExtension = Path.GetFileNameWithoutExtension(metadata);
        var normalizedMetadata = NormalizeLookupName(metadataNoExtension);
        var best = GetLazerTempRoots(lazerProcesses)
            .SelectMany(root =>
                EnumerateOsuCandidates(root)
                    .Select(candidate => new
                    {
                        Root = root,
                        Path = candidate,
                        Score = GetMetadataMatchScore(
                            metadataNoExtension,
                            normalizedMetadata,
                            candidate
                        ),
                    })
            )
            .Where(x => x.Score > 0)
            .OrderByDescending(x => x.Score)
            .ThenByDescending(x => File.GetLastWriteTimeUtc(x.Path))
            .FirstOrDefault();

        if (best == null)
            return null;

        var bestFolder = Path.GetDirectoryName(best.Path);
        if (string.IsNullOrWhiteSpace(bestFolder))
            return null;

        return (bestFolder, best.Root);
    }

    private static IEnumerable<string> GetLazerTempRoots(
        IReadOnlyList<OsuProcessSnapshot> lazerProcesses
    )
    {
        var roots = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var process in lazerProcesses)
        {
            if (string.IsNullOrWhiteSpace(process.ExecutablePath))
                continue;

            var exeDir = Path.GetDirectoryName(process.ExecutablePath);
            if (string.IsNullOrWhiteSpace(exeDir))
                continue;

            var tempFromExe = Path.Combine(exeDir, "Temp");
            if (Directory.Exists(tempFromExe))
                roots.Add(tempFromExe);
        }

        var localAppData = Environment.GetEnvironmentVariable("LOCALAPPDATA");
        if (!string.IsNullOrWhiteSpace(localAppData))
        {
            var windowsTemp = Path.Combine(localAppData, "Temp");
            if (Directory.Exists(windowsTemp))
                roots.Add(windowsTemp);

            var osuTemp = Path.Combine(localAppData, "osu!", "Temp");
            if (Directory.Exists(osuTemp))
                roots.Add(osuTemp);
        }

        var systemTemp = Path.GetTempPath();
        if (!string.IsNullOrWhiteSpace(systemTemp) && Directory.Exists(systemTemp))
            roots.Add(systemTemp);

        foreach (var root in roots)
            yield return root;
    }

    private static string? TryFindExactMetadataOsuFile(string root, string metadata)
    {
        if (
            string.IsNullOrWhiteSpace(root)
            || string.IsNullOrWhiteSpace(metadata)
            || !Directory.Exists(root)
        )
            return null;

        try
        {
            return Directory
                .EnumerateFiles(root, metadata, SearchOption.AllDirectories)
                .OrderByDescending(File.GetLastWriteTimeUtc)
                .FirstOrDefault();
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static IEnumerable<string> EnumerateOsuCandidates(string root)
    {
        if (!Directory.Exists(root))
            yield break;

        IEnumerable<DirectoryInfo> folders;
        try
        {
            folders = new DirectoryInfo(root)
                .GetDirectories()
                .OrderByDescending(d => d.LastWriteTimeUtc)
                .Take(150)
                .ToList();
        }
        catch (Exception)
        {
            yield break;
        }

        foreach (var folder in folders)
        {
            IEnumerable<string> files;
            try
            {
                files = Directory.EnumerateFiles(
                    folder.FullName,
                    "*.osu",
                    SearchOption.AllDirectories
                );
            }
            catch (Exception)
            {
                continue;
            }

            foreach (var file in files)
                yield return file;
        }
    }

    private static int GetMetadataMatchScore(
        string metadataNoExtension,
        string normalizedMetadata,
        string osuFilePath
    )
    {
        var fileName = Path.GetFileNameWithoutExtension(osuFilePath);
        if (string.IsNullOrWhiteSpace(fileName))
            return 0;

        var normalizedFileName = NormalizeLookupName(fileName);
        if (string.IsNullOrWhiteSpace(normalizedFileName))
            return 0;

        if (fileName.Equals(metadataNoExtension, StringComparison.OrdinalIgnoreCase))
            return 100;
        if (normalizedFileName.Equals(normalizedMetadata, StringComparison.Ordinal))
            return 95;
        if (
            normalizedFileName.Contains(normalizedMetadata, StringComparison.Ordinal)
            || normalizedMetadata.Contains(normalizedFileName, StringComparison.Ordinal)
        )
            return 75;

        var editorLikeMetadata = TryBuildEditorLikeMetadata(osuFilePath);
        var normalizedEditorLikeMetadata = NormalizeLookupName(editorLikeMetadata ?? string.Empty);
        if (string.IsNullOrWhiteSpace(normalizedEditorLikeMetadata))
            return 0;

        if (normalizedEditorLikeMetadata.Equals(normalizedMetadata, StringComparison.Ordinal))
            return 70;
        if (
            normalizedEditorLikeMetadata.Contains(normalizedMetadata, StringComparison.Ordinal)
            || normalizedMetadata.Contains(normalizedEditorLikeMetadata, StringComparison.Ordinal)
        )
            return 60;

        return 0;
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

    private static string? TryBuildEditorLikeMetadata(string osuFilePath)
    {
        try
        {
            var folder = Path.GetDirectoryName(osuFilePath);
            if (string.IsNullOrWhiteSpace(folder))
                return null;

            var content = File.ReadAllText(osuFilePath);
            var meta = ParseBeatmapMetadata(folder, content);
            if (
                string.IsNullOrWhiteSpace(meta.artist)
                || string.IsNullOrWhiteSpace(meta.title)
                || string.IsNullOrWhiteSpace(meta.creator)
            )
                return null;

            var difficulty = Path.GetFileNameWithoutExtension(osuFilePath)
                .Split('[')
                .Skip(1)
                .FirstOrDefault()
                ?.TrimEnd(']');

            if (string.IsNullOrWhiteSpace(difficulty))
                return null;

            return $"{meta.artist} - {meta.title} ({meta.creator}) [{difficulty}]";
        }
        catch (Exception)
        {
            return null;
        }
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
            return strictMatch.Groups["metadata"].Value.Trim();

        var looseMatches = AnyOsuFilenameRegex.Matches(trimmed);
        if (looseMatches.Count == 0)
            return null;

        // Prefer the right-most occurrence when titles include prefixes/suffixes.
        var loose = looseMatches[^1].Groups["metadata"].Value.Trim();
        return string.IsNullOrWhiteSpace(loose) ? null : loose;
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
