using System.Text.RegularExpressions;
using MapsetVerifier.Framework;
using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Parser.Objects;
using MapsetVerifier.Server.Model;
using MapsetVerifier.Server.Service.OsuRuntime;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Processing;

namespace MapsetVerifier.Server.Service;

public static class BeatmapService
{
    private static readonly Regex BackgroundRegex = new Regex(
        "0,0,\"(?<file>[^\"]+)\"",
        RegexOptions.Compiled
    );
    private static readonly string[] SupportedImageExtensions = [".jpg", ".jpeg", ".png", ".gif"];

    private static readonly string[] SupportedAudioExtensions = [".mp3", ".ogg", ".wav", ".oga"];

    /// <summary>
    /// Desired thumbnail height
    /// </summary>
    private const int MaxImageHeight = 126;

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

    public static ApiLazerLookupResult GetCurrentLazerBeatmap() =>
        CurrentBeatmapLookupService.GetCurrentLazerBeatmap();

    public static ApiLazerLookupResult GetCurrentStableBeatmap(string? songsFolderOverride) =>
        CurrentBeatmapLookupService.GetCurrentStableBeatmap(songsFolderOverride);

    public static ApiBeatmapPage GetBeatmaps(
        string songsFolder,
        string? search,
        int page,
        int pageSize
    )
    {
        if (!Directory.Exists(songsFolder))
            return new ApiBeatmapPage([], page, pageSize, false);

        var dirInfo = new DirectoryInfo(songsFolder);
        var folders = dirInfo.GetDirectories().OrderByDescending(d => d.LastWriteTimeUtc).ToList();

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

    public static ApiBeatmapInfo? GetBeatmapInfo(string beatmapSetFolder)
    {
        if (!Directory.Exists(beatmapSetFolder))
            return null;

        var osuFile = Directory.GetFiles(beatmapSetFolder, "*.osu").FirstOrDefault();
        if (string.IsNullOrWhiteSpace(osuFile))
            return null;

        var content = File.ReadAllText(osuFile);
        var meta = ParseBeatmapMetadata(beatmapSetFolder, content);

        ulong? beatmapSetId = null;
        if (ulong.TryParse(meta.beatmapSetId, out var parsedBeatmapSetId))
            beatmapSetId = parsedBeatmapSetId;

        return new ApiBeatmapInfo(
            title: meta.title,
            artist: meta.artist,
            creator: meta.creator,
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
        const int maxHeight = MaxImageHeight; // use constant
        var songsFolder = ResolveSongsFolder(songsFolderOverride);
        if (string.IsNullOrWhiteSpace(songsFolder))
            return BeatmapImageResult.Error("Songs folder could not be detected.");
        var targetFolder = Path.Combine(songsFolder, folder);
        if (!Directory.Exists(targetFolder))
            return BeatmapImageResult.Error("Beatmap folder not found.");

        var imagePath = GetConfiguredBackgroundImagePath(targetFolder);
        if (imagePath == null)
            return BeatmapImageResult.Error("No background image found.");

        var extLower = Path.GetExtension(imagePath).ToLowerInvariant();

        var fi = new FileInfo(imagePath);
        var baseEtagCore = fi.Exists
            ? $"{fi.LastWriteTimeUtc.Ticks:x}-{fi.Length:x}"
            : Path.GetFileName(imagePath);

        var originalHeight = 0;
        try
        {
            var info = Image.Identify(imagePath);
            originalHeight = info.Height;
        }
        catch (Exception)
        {
            // ignore identify errors
        }

        var needsResize = !original && originalHeight > maxHeight;

        if (!needsResize)
        {
            var mimeOriginal = extLower == ".png" ? "image/png" : "image/jpeg";
            var etagOriginal = '"' + baseEtagCore + '"';
            return BeatmapImageResult.SuccessResult(imagePath, mimeOriginal, etagOriginal);
        }

        // Perform in-memory resize
        try
        {
            using var img = Image.Load(imagePath);
            var ratio = (double)maxHeight / img.Height;
            var targetW = Math.Max(1, (int)Math.Round(img.Width * ratio));
            var targetH = maxHeight;
            img.Mutate(c =>
                c.Resize(
                    new ResizeOptions
                    {
                        Mode = ResizeMode.Max,
                        Size = new Size(targetW, targetH),
                        Sampler = KnownResamplers.Lanczos3,
                    }
                )
            );
            var ms = new MemoryStream();
            if (extLower == ".png")
                img.Save(ms, new PngEncoder());
            else
                img.Save(ms, new JpegEncoder { Quality = 85 });
            ms.Position = 0; // reset for reading
            var mime = extLower == ".png" ? "image/png" : "image/jpeg";
            var etag = '"' + baseEtagCore + $"-h{maxHeight}" + '"';
            return BeatmapImageResult.SuccessStreamResult(ms, mime, etag);
        }
        catch (Exception ex)
        {
            return BeatmapImageResult.Error("Failed to resize image: " + ex.Message);
        }
    }

    /// <summary>
    /// Resolves audio file relative to the beatmap set folder (osu! General AudioFilename).
    /// </summary>
    public static BeatmapAudioResult GetBeatmapAudio(
        string folder,
        string audioFile,
        string? songsFolderOverride = null
    )
    {
        if (string.IsNullOrWhiteSpace(audioFile))
            return BeatmapAudioResult.Error("Audio file is required.");

        var songsFolder = ResolveSongsFolder(songsFolderOverride);
        if (string.IsNullOrWhiteSpace(songsFolder))
            return BeatmapAudioResult.Error("Songs folder could not be detected.");

        var targetFolderPhysical = Path.GetFullPath(Path.Combine(songsFolder, folder));
        if (!Directory.Exists(targetFolderPhysical))
            return BeatmapAudioResult.Error("Beatmap folder not found.");

        var trimmed = audioFile
            .Trim()
            .TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var normalizedSep = trimmed.Replace('/', Path.DirectorySeparatorChar);

        if (Path.IsPathRooted(normalizedSep))
            return BeatmapAudioResult.Error("Invalid audio path.");

        foreach (
            var part in normalizedSep.Split(
                Path.DirectorySeparatorChar,
                StringSplitOptions.RemoveEmptyEntries
            )
        )
        {
            if (part is "." or "..")
                return BeatmapAudioResult.Error("Invalid audio path.");
        }

        var resolved = Path.GetFullPath(Path.Combine(targetFolderPhysical, normalizedSep));
        var root = Path.TrimEndingDirectorySeparator(Path.GetFullPath(targetFolderPhysical));

        var comparison = OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;

        var isInside =
            resolved.Equals(root, comparison)
            || resolved.StartsWith(root + Path.DirectorySeparatorChar, comparison);
        if (!isInside)
            return BeatmapAudioResult.Error("Audio file is outside beatmap folder.");

        var extension = Path.GetExtension(resolved).ToLowerInvariant();
        if (Array.IndexOf(SupportedAudioExtensions, extension) < 0)
            return BeatmapAudioResult.Error("Unsupported or unrecognized audio format.");

        if (!File.Exists(resolved))
            return BeatmapAudioResult.Error("Audio file not found.");

        var mime = extension switch
        {
            ".mp3" => "audio/mpeg",
            ".ogg" or ".oga" => "audio/ogg",
            ".wav" => "audio/wav",
            _ => "application/octet-stream",
        };

        return BeatmapAudioResult.ForFile(resolved, mime);
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

                if (backgroundPath != null && IsSupportedImageFile(backgroundPath))
                    return backgroundPath;
            }
            catch (Exception)
            {
                // Ignore invalid .osu files and continue looking for a usable background.
            }
        }

        return null;
    }

    private static bool IsSupportedImageFile(string filePath) =>
        SupportedImageExtensions.Contains(Path.GetExtension(filePath).ToLowerInvariant());

    public static ApiBeatmapSetCheckResult RunBeatmapSetChecks(string beatmapSetFolder)
    {
        var beatmapSet = new BeatmapSet(beatmapSetFolder);
        var issues = Checker.GetBeatmapSetIssues(beatmapSet);
        var checksById = CheckerRegistry.GetChecksWithId();

        var generalIssues = issues.Where(issue => issue.CheckOrigin is GeneralCheck).ToArray();

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

        // TODO improve so we run checks only for the overridden difficulty, not the whole set
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
