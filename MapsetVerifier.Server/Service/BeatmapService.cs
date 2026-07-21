using System.Text.RegularExpressions;
using MapsetVerifier.Framework;
using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Parser.Objects;
using MapsetVerifier.Server.Model;
using MapsetVerifier.Server.Service.OsuRuntime;
using Serilog;

namespace MapsetVerifier.Server.Service;

public static class BeatmapService
{
    private static readonly Regex BackgroundRegex = new Regex(
        "0,0,\"(?<file>[^\"]+)\"",
        RegexOptions.Compiled
    );

    public static string? DetectSongsFolder()
    {
        if (OperatingSystem.IsWindows())
        {
            // Try registry first: HKEY_CLASSES_ROOT\\osu\\DefaultIcon default value path to osu!.exe
            try
            {
                var key = Microsoft.Win32.Registry.ClassesRoot.OpenSubKey("osu\\DefaultIcon");
                var value = key?.GetValue(string.Empty)?.ToString();
                if (!string.IsNullOrWhiteSpace(value))
                {
                    value = value.Trim('"');
                    var exePath = value;
                    var dir = Path.GetDirectoryName(exePath);
                    if (!string.IsNullOrWhiteSpace(dir))
                    {
                        var songsPath = Path.Combine(dir, "Songs");
                        if (Directory.Exists(songsPath))
                            return songsPath;
                    }
                }
            }
            catch
            { /* ignore registry errors */
            }

            // Fallback to %USERPROFILE%\AppData\Local\osu!\Songs
            try
            {
                var userProfile = Environment.GetEnvironmentVariable("USERPROFILE");
                if (!string.IsNullOrWhiteSpace(userProfile))
                {
                    var fallback = Path.Combine(userProfile, "AppData", "Local", "osu!", "Songs");
                    if (Directory.Exists(fallback))
                        return fallback;
                }
            }
            catch
            { /* ignore */
            }
        }
        return null;
    }

    /// <summary>
    /// Uses an explicit Songs directory when it exists; otherwise the same detection as <see cref="DetectSongsFolder"/>.
    /// </summary>
    public static string? ResolveSongsFolder(string? songsFolderOverride)
    {
        if (
            !string.IsNullOrWhiteSpace(songsFolderOverride) && Directory.Exists(songsFolderOverride)
        )
            return songsFolderOverride;
        return DetectSongsFolder();
    }

    public static ApiLazerLookupResult GetCurrentLazerBeatmap(
        string? lazerDataDirOverride = null
    ) => CurrentBeatmapLookupService.GetCurrentLazerBeatmap(lazerDataDirOverride);

    public static ApiLazerLookupResult GetCurrentStableBeatmap(string? songsFolderOverride) =>
        CurrentBeatmapLookupService.GetCurrentStableBeatmap(songsFolderOverride);

    public static ApiBeatmapPage GetBeatmaps(
        string songsFolder,
        string? search,
        int page,
        int pageSize,
        IReadOnlySet<string>? bookmarkedFolders = null
    )
    {
        if (!Directory.Exists(songsFolder))
            return new ApiBeatmapPage([], page, pageSize, false);

        var dirInfo = new DirectoryInfo(songsFolder);
        var folders = dirInfo
            .GetDirectories()
            .OrderByDescending(GetEffectiveLastWriteTimeUtc)
            .ToList();

        if (bookmarkedFolders is { Count: > 0 })
            folders = folders.Where(f => bookmarkedFolders.Contains(f.Name)).ToList();

        var skipped = page * pageSize;
        var pageFolders = folders.Skip(skipped).Take(pageSize + 1).ToList();
        var results = new List<ApiBeatmap>();
        foreach (var folder in pageFolders.Take(pageSize))
        {
            try
            {
                var osuFiles = Directory.GetFiles(folder.FullName, "*.osu");
                if (osuFiles.Length == 0)
                    continue;
                var content = File.ReadAllText(osuFiles[0]);
                var meta = ParseBeatmapMetadata(folder.FullName, content);
                if (MatchesSearch(meta, search))
                {
                    var backgroundUrl = string.IsNullOrEmpty(meta.backgroundPath)
                        ? string.Empty
                        : $"/beatmap/image?folder={Uri.EscapeDataString(folder.Name)}";
                    results.Add(
                        new ApiBeatmap(
                            folder: folder.Name,
                            title: meta.title ?? string.Empty,
                            artist: meta.artist ?? string.Empty,
                            creator: meta.creator ?? string.Empty,
                            beatmapId: meta.beatmapId ?? string.Empty,
                            beatmapSetId: meta.beatmapSetId ?? string.Empty,
                            backgroundPath: backgroundUrl
                        )
                    );
                }
            }
            catch (Exception)
            {
                // Ignore individual mapset failures to avoid breaking entire page.
            }
        }
        var hasMore = pageFolders.Count > pageSize;
        return new ApiBeatmapPage(results, page, pageSize, hasMore);
    }

    /// <summary>
    /// A folder's own LastWriteTimeUtc isn't updated by NTFS when an existing file inside it is
    /// edited in place (only file creation/deletion/rename bumps it), so sorting "most recently
    /// changed" purely on folder mtime misses in-place edits. Fall back to the newest top-level
    /// file's mtime when it's more recent than the folder's own.
    /// </summary>
    private static DateTime GetEffectiveLastWriteTimeUtc(DirectoryInfo folder)
    {
        var latest = folder.LastWriteTimeUtc;
        try
        {
            foreach (var file in folder.EnumerateFiles())
            {
                if (file.LastWriteTimeUtc > latest)
                    latest = file.LastWriteTimeUtc;
            }
        }
        catch (Exception)
        {
            // Ignore inaccessible folders; fall back to the folder's own timestamp.
        }
        return latest;
    }

    public static ApiBeatmapInfo? GetBeatmapInfo(string beatmapSetFolder)
    {
        if (!Directory.Exists(beatmapSetFolder))
            return null;

        var osuFile = Directory.GetFiles(beatmapSetFolder, "*.osu").FirstOrDefault();
        if (string.IsNullOrWhiteSpace(osuFile))
            return null;

        var content = File.ReadAllText(osuFile);
        var meta = ParseBeatmapMetadata(beatmapSetFolder, content);

        ulong? beatmapId = null;
        if (ulong.TryParse(meta.beatmapId, out var parsedBeatmapId))
            beatmapId = parsedBeatmapId;

        ulong? beatmapSetId = null;
        if (ulong.TryParse(meta.beatmapSetId, out var parsedBeatmapSetId))
            beatmapSetId = parsedBeatmapSetId;

        return new ApiBeatmapInfo(
            title: meta.title,
            artist: meta.artist,
            creator: meta.creator,
            beatmapId: beatmapId,
            beatmapSetId: beatmapSetId
        );
    }

    private static bool MatchesSearch(
        (
            string? title,
            string? artist,
            string? creator,
            string? beatmapId,
            string? beatmapSetId,
            string? backgroundPath
        ) meta,
        string? search
    )
    {
        if (string.IsNullOrWhiteSpace(search))
            return true;
        var searchable =
            $"{meta.title} - {meta.artist} | {meta.creator} ({meta.beatmapId} {meta.beatmapSetId})";
        return searchable.Contains(search, StringComparison.OrdinalIgnoreCase);
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
            var value = line.Substring(prefix.Length).Trim();
            return value;
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

    /// <summary>
    /// Loads the background image for a beatmap.
    /// </summary>
    /// <param name="folder">The folder of the beatmapset where we can find the background</param>
    /// <param name="original">When set to true, return the full-sized background without thumbnail resize</param>
    /// <param name="songsFolderOverride">Song folder used to when finding the background of a song. When not provided it will try to detect it.</param>
    /// <returns>The background of the beatmapset</returns>
    public static BeatmapImageResult GetBeatmapImage(
        string folder,
        bool original = false,
        string? songsFolderOverride = null
    )
    {
        var songsFolder = ResolveSongsFolder(songsFolderOverride);
        if (string.IsNullOrWhiteSpace(songsFolder))
            return BeatmapImageResult.Error("Songs folder could not be detected.");
        var targetFolder = Path.Combine(songsFolder, folder);
        if (!Directory.Exists(targetFolder))
            return BeatmapImageResult.Error("Beatmap folder not found.");

        var imagePath = GetConfiguredBackgroundImagePath(targetFolder);
        if (imagePath == null)
            return BeatmapImageResult.Error("No background image found.");

        return BeatmapImageProcessing.BuildResult(imagePath, original);
    }

    /// <summary>
    /// Resolves and streams a lazer beatmapset's background image directly from its
    /// content-addressed file, without materializing the whole set to disk.
    /// </summary>
    public static BeatmapImageResult GetLazerBeatmapImage(
        string beatmapSetId,
        bool original = false,
        string? lazerDataDirOverride = null
    )
    {
        var dataDir = LazerRealmService.ResolveLazerDataDirectory(lazerDataDirOverride);
        if (string.IsNullOrWhiteSpace(dataDir))
            return BeatmapImageResult.Error("Lazer data folder could not be detected.");

        var backgroundFile = LazerRealmService.GetBackgroundFilename(dataDir, beatmapSetId);
        if (string.IsNullOrWhiteSpace(backgroundFile))
            return BeatmapImageResult.Error("No background image found.");

        var extension = Path.GetExtension(backgroundFile);
        if (!BeatmapImageProcessing.IsSupportedImageFile(backgroundFile))
            return BeatmapImageResult.Error("No background image found.");

        var files = LazerRealmService.GetBeatmapSetFiles(dataDir, beatmapSetId);
        var hash = files
            ?.FirstOrDefault(f =>
                string.Equals(f.Filename, backgroundFile, StringComparison.OrdinalIgnoreCase)
            )
            .Hash;
        if (string.IsNullOrWhiteSpace(hash))
            return BeatmapImageResult.Error("Background image file could not be resolved.");

        var imagePath = LazerRealmService.ResolveFilePathFromHash(dataDir, hash);
        if (!File.Exists(imagePath))
            return BeatmapImageResult.Error("Background image file is missing on disk.");

        return BeatmapImageProcessing.BuildResult(imagePath, original, extension);
    }

    public static ApiBeatmapPage GetLazerBeatmaps(
        string? search,
        int page,
        int pageSize,
        string? lazerDataDirOverride = null
    )
    {
        var dataDir = LazerRealmService.ResolveLazerDataDirectory(lazerDataDirOverride);
        if (string.IsNullOrWhiteSpace(dataDir))
            return new ApiBeatmapPage([], page, pageSize, false);

        return LazerRealmService.GetBeatmapSets(dataDir, search, page, pageSize);
    }

    public static ApiLazerMaterializeResult MaterializeLazerBeatmap(
        string beatmapSetId,
        string? lazerDataDirOverride = null
    )
    {
        var dataDir = LazerRealmService.ResolveLazerDataDirectory(lazerDataDirOverride);
        if (string.IsNullOrWhiteSpace(dataDir))
            return ApiLazerMaterializeResult.Error("Lazer data folder could not be detected.");

        return LazerBeatmapMaterializer.Materialize(dataDir, beatmapSetId);
    }

    private static string? GetConfiguredBackgroundImagePath(string beatmapSetFolder)
    {
        foreach (
            var osuFile in Directory
                .GetFiles(beatmapSetFolder, "*.osu")
                .OrderBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase)
        )
        {
            try
            {
                var code = File.ReadAllText(osuFile);
                var beatmap = new Beatmap(code, beatmapSetFolder, Path.GetFileName(osuFile));
                var backgroundPath = beatmap.GetBackgroundFilePath();

                if (
                    backgroundPath != null
                    && BeatmapImageProcessing.IsSupportedImageFile(backgroundPath)
                )
                    return backgroundPath;
            }
            catch (Exception)
            {
                // Ignore invalid .osu files and continue looking for a usable background.
            }
        }

        return null;
    }

    public static ApiBeatmapStructure GetBeatmapSetStructure(string beatmapSetFolder)
    {
        var beatmapSet = CreateBeatmapSet(beatmapSetFolder);
        return BuildBeatmapStructure(beatmapSet);
    }

    public static (BeatmapSet BeatmapSet, ApiBeatmapStructure Structure) ParseBeatmapSet(
        string beatmapSetFolder
    )
    {
        var beatmapSet = CreateBeatmapSet(beatmapSetFolder);
        return (beatmapSet, BuildBeatmapStructure(beatmapSet));
    }

    public static ApiBeatmapSetCheckResult RunBeatmapSetChecks(
        string beatmapSetFolder,
        IProgress<CheckProgress>? progress = null,
        bool includeCheckRunDelta = true,
        bool createSnapshot = true
    )
    {
        var (beatmapSet, _) = ParseBeatmapSet(beatmapSetFolder);
        return RunBeatmapSetChecks(beatmapSet, progress, includeCheckRunDelta, createSnapshot);
    }

    public static ApiBeatmapSetCheckResult RunBeatmapSetChecks(
        BeatmapSet beatmapSet,
        IProgress<CheckProgress>? progress = null,
        bool includeCheckRunDelta = true,
        bool createSnapshot = true
    )
    {
        if (createSnapshot)
        {
            try
            {
                SnapshotService.SnapshotCurrentBeatmapSet(beatmapSet);
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Failed to snapshot beatmap set before running checks.");
            }
        }

        var issues = Checker.GetBeatmapSetIssues(beatmapSet, progress);
        var result = BuildBeatmapSetCheckResult(beatmapSet, issues);
        var delta = includeCheckRunDelta
            ? CheckRunHistoryService.BuildDeltaAndRememberCurrent(beatmapSet, result)
            : null;

        return new ApiBeatmapSetCheckResult(
            general: result.General,
            difficulties: result.Difficulties,
            checks: result.Checks,
            checkRunDelta: delta
        );
    }

    private static BeatmapSet CreateBeatmapSet(string beatmapSetFolder)
    {
        var beatmapSet = new BeatmapSet(beatmapSetFolder);
        beatmapSet.ClearInterpretedDifficultyOverrides();
        return beatmapSet;
    }

    private static ApiBeatmapStructure BuildBeatmapStructure(BeatmapSet beatmapSet) =>
        new(
            beatmapSet
                .Beatmaps.Select(beatmap => new ApiBeatmapStructureDifficulty(
                    category: beatmap.MetadataSettings.version,
                    beatmapId: beatmap.MetadataSettings.beatmapId,
                    mode: beatmap.GeneralSettings.mode,
                    starRating: beatmap.StarRating
                ))
                .ToList()
        );

    private static ApiBeatmapSetCheckResult BuildBeatmapSetCheckResult(
        BeatmapSet beatmapSet,
        List<Issue> issues
    )
    {
        var checksById = CheckerRegistry.GetChecksWithId();

        var setWideBeatmapSetIssues = issues
            .Where(issue => issue.CheckOrigin is BeatmapSetCheck && issue.beatmap == null)
            .ToArray();

        var generalIssues = issues
            .Where(issue => issue.CheckOrigin is GeneralCheck)
            .Concat(setWideBeatmapSetIssues)
            .ToArray();

        var apiBeatmapCheckResults = beatmapSet
            .Beatmaps.Select(beatmap =>
            {
                var interpretedDifficulty = beatmap.GetDifficulty();
                var beatmapIssues = issues
                    .Where(issue => issue.beatmap == beatmap)
                    .Except(generalIssues)
                    .Where(issue => issue.AppliesToDifficulty(interpretedDifficulty));

                var beatmapCheckResults = beatmapIssues.Select(issue =>
                {
                    var checkId = checksById
                        .FirstOrDefault(c => c.Value.GetType() == issue.CheckOrigin?.GetType())
                        .Key;

                    return new ApiCheckResult(
                        id: checkId,
                        level: issue.level,
                        message: issue.message
                    );
                });

                var parsedDifficulty =
                    interpretedDifficulty == Beatmap.Difficulty.Ultra
                        ? Beatmap.Difficulty.Expert
                        : interpretedDifficulty;

                return new ApiCategoryCheckResult(
                    checkResults: beatmapCheckResults,
                    category: beatmap.MetadataSettings.version,
                    beatmapId: beatmap.MetadataSettings.beatmapId,
                    mode: beatmap.GeneralSettings.mode,
                    difficultyLevel: parsedDifficulty,
                    starRating: beatmap.StarRating
                );
            })
            .ToList();

        var generalCheckResults = generalIssues
            .Select(issue =>
            {
                var checkId = checksById
                    .FirstOrDefault(c => c.Value.GetType() == issue.CheckOrigin?.GetType())
                    .Key;

                return new ApiCheckResult(id: checkId, level: issue.level, message: issue.message);
            })
            .ToList();

        // Build checks dictionary: include checks present in general or any difficulty
        var checksPresentIds = new HashSet<int>(
            generalCheckResults
                .Select(c => c.Id)
                .Concat(
                    apiBeatmapCheckResults.SelectMany(cat => cat.CheckResults.Select(cr => cr.Id))
                )
        );
        var checksDict = new Dictionary<int, ApiCheckDefinition>();
        foreach (var id in checksPresentIds)
        {
            if (!checksById.TryGetValue(id, out var check))
                continue;
            var name = check.GetMetadata().Message;
            var difficultiesForCheck = apiBeatmapCheckResults
                .Where(cat => cat.CheckResults.Any(cr => cr.Id == id))
                .Select(cat => cat.Category)
                .Distinct()
                .ToList();
            // Include "General" if appears in general results
            if (generalCheckResults.Any(gr => gr.Id == id))
                difficultiesForCheck.Insert(0, "General");
            checksDict[id] = new ApiCheckDefinition(
                id: id,
                name: name,
                difficulties: difficultiesForCheck
            );
        }

        return new ApiBeatmapSetCheckResult(
            general: new ApiCategoryCheckResult(
                checkResults: generalCheckResults,
                category: "General",
                beatmapId: null,
                mode: null,
                difficultyLevel: null,
                starRating: null
            ),
            difficulties: apiBeatmapCheckResults,
            checks: checksDict
        );
    }

    public static ApiCategoryOverrideCheckResult? RunDifficultyCheckWithOverride(
        string beatmapSetFolder,
        string difficultyName,
        Beatmap.Difficulty overrideDifficulty
    )
    {
        var beatmapSet = new BeatmapSet(beatmapSetFolder);
        var beatmap = beatmapSet.Beatmaps.FirstOrDefault(b =>
            b.MetadataSettings.version == difficultyName
        );

        if (beatmap == null)
            return null;

        beatmapSet.ApplyInterpretedDifficultyOverride(beatmap, overrideDifficulty);
        var issues = Checker.GetBeatmapSetIssues(beatmapSet);
        var checksById = CheckerRegistry.GetChecksWithId();

        var generalIssues = issues.Where(issue => issue.CheckOrigin is GeneralCheck).ToArray();

        var beatmapIssues = issues
            .Where(issue => issue.beatmap == beatmap)
            .Except(generalIssues)
            .Where(issue => issue.AppliesToDifficulty(overrideDifficulty));

        var beatmapCheckResults = beatmapIssues
            .Select(issue =>
            {
                var checkId = checksById
                    .FirstOrDefault(c => c.Value.GetType() == issue.CheckOrigin?.GetType())
                    .Key;

                return new ApiCheckResult(id: checkId, level: issue.level, message: issue.message);
            })
            .ToList();

        // Build checks dictionary: include checks present in general or any difficulty
        var checksPresentIds = new HashSet<int>(beatmapCheckResults.Select(c => c.Id));
        var checksDict = new Dictionary<int, ApiCheckDefinition>();
        foreach (var id in checksPresentIds)
        {
            if (!checksById.TryGetValue(id, out var check))
                continue;
            var name = check.GetMetadata().Message;
            checksDict[id] = new ApiCheckDefinition(
                id: id,
                name: name,
                difficulties: [difficultyName]
            );
        }

        return new ApiCategoryOverrideCheckResult(
            categoryResult: new ApiCategoryCheckResult(
                checkResults: beatmapCheckResults,
                category: beatmap.MetadataSettings.version,
                beatmapId: beatmap.MetadataSettings.beatmapId,
                mode: beatmap.GeneralSettings.mode,
                difficultyLevel: overrideDifficulty,
                starRating: beatmap.StarRating
            ),
            checks: checksDict
        );
    }
}
