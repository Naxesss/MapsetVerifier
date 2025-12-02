using System.Text.RegularExpressions;
using MapsetVerifier.Framework;
using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Parser.Objects;
using MapsetVerifier.Server.Model;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Jpeg;

namespace MapsetVerifier.Server.Service;

public static class BeatmapService
{
    private static readonly Regex BackgroundRegex = new Regex("0,0,\"(?<file>[^\"]+)\"", RegexOptions.Compiled);

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
            catch { /* ignore registry errors */ }

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
            catch { /* ignore */ }
        }
        return null;
    }

    public static ApiBeatmapPage GetBeatmaps(string songsFolder, string? search, int page, int pageSize)
    {
        if (!Directory.Exists(songsFolder))
            return new ApiBeatmapPage([], page, pageSize, false);

        var dirInfo = new DirectoryInfo(songsFolder);
        var folders = dirInfo.GetDirectories()
            .OrderByDescending(d => d.LastWriteTimeUtc)
            .ToList();

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
                    var backgroundUrl = string.IsNullOrEmpty(meta.backgroundPath) ? string.Empty : $"/beatmap/image?folder={Uri.EscapeDataString(folder.Name)}";
                    results.Add(new ApiBeatmap(
                        folder: folder.Name,
                        title: meta.title ?? string.Empty,
                        artist: meta.artist ?? string.Empty,
                        creator: meta.creator ?? string.Empty,
                        beatmapId: meta.beatmapId ?? string.Empty,
                        beatmapSetId: meta.beatmapSetId ?? string.Empty,
                        backgroundPath: backgroundUrl
                    ));
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

    private static bool MatchesSearch((string? title, string? artist, string? creator, string? beatmapId, string? beatmapSetId, string? backgroundPath) meta, string? search)
    {
        if (string.IsNullOrWhiteSpace(search))
            return true;
        var searchable = $"{meta.title} - {meta.artist} | {meta.creator} ({meta.beatmapId} {meta.beatmapSetId})";
        return searchable.Contains(search, StringComparison.OrdinalIgnoreCase);
    }

    private static (string? title, string? artist, string? creator, string? beatmapId, string? beatmapSetId, string? backgroundPath) ParseBeatmapMetadata(string folderPath, string data)
    {
        string? GetValue(string prefix)
        {
            var line = data.Split('\n').FirstOrDefault(l => l.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
            if (line == null) return null;
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
    /// <returns>The background of the beatmapset</returns>
    public static BeatmapImageResult GetBeatmapImage(string folder, bool original = false)
    {
        const int maxHeight = MaxImageHeight; // use constant
        var songsFolder = DetectSongsFolder();
        if (string.IsNullOrWhiteSpace(songsFolder))
            return BeatmapImageResult.Error("Songs folder could not be detected.");
        var targetFolder = Path.Combine(songsFolder, folder);
        if (!Directory.Exists(targetFolder))
            return BeatmapImageResult.Error("Beatmap folder not found.");

        var imagePath = Directory.GetFiles(targetFolder)
            .FirstOrDefault(f => {
                var ext = Path.GetExtension(f).ToLowerInvariant();
                return ext is ".jpg" or ".jpeg" or ".png" or ".gif";
            });
        if (imagePath == null)
            return BeatmapImageResult.Error("No background image found.");

        var extLower = Path.GetExtension(imagePath).ToLowerInvariant();

        var fi = new FileInfo(imagePath);
        var baseEtagCore = fi.Exists ? $"{fi.LastWriteTimeUtc.Ticks:x}-{fi.Length:x}" : Path.GetFileName(imagePath);

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
            img.Mutate(c => c.Resize(new ResizeOptions
            {
                Mode = ResizeMode.Max,
                Size = new Size(targetW, targetH),
                Sampler = KnownResamplers.Lanczos3
            }));
            var ms = new MemoryStream();
            if (extLower == ".png") img.Save(ms, new PngEncoder());
            else img.Save(ms, new JpegEncoder { Quality = 85 });
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

    public static ApiBeatmapSetCheckResult RunBeatmapSetChecks(string beatmapSetFolder)
    {
        var beatmapSet = new BeatmapSet(beatmapSetFolder);
        var issues = Checker.GetBeatmapSetIssues(beatmapSet);
        var checksById = CheckerRegistry.GetChecksWithId();

        var generalIssues = issues.Where(issue => issue.CheckOrigin is GeneralCheck).ToArray();

        var apiBeatmapCheckResults = beatmapSet.Beatmaps.Select(beatmap =>
        {
            var interpretedDifficulty = beatmap.GetDifficulty();
            var beatmapIssues = issues.Where(issue => issue.beatmap == beatmap)
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

            return new ApiCategoryCheckResult(
                checkResults: beatmapCheckResults,
                category: beatmap.MetadataSettings.version,
                beatmapId: beatmap.MetadataSettings.beatmapId,
                mode: beatmap.GeneralSettings.mode,
                difficultyLevel: interpretedDifficulty
            );
        }).ToList();

        var generalCheckResults = generalIssues.Select(issue =>
        {
            var checkId = checksById
                .FirstOrDefault(c => c.Value.GetType() == issue.CheckOrigin?.GetType())
                .Key;

            return new ApiCheckResult(
                id: checkId,
                level: issue.level,
                message: issue.message
            );
        }).ToList();

        // Build checks dictionary: include checks present in general or any difficulty
        var checksPresentIds = new HashSet<int>(generalCheckResults.Select(c => c.Id)
            .Concat(apiBeatmapCheckResults.SelectMany(cat => cat.CheckResults.Select(cr => cr.Id))));
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

        var firstMeta = beatmapSet.Beatmaps.FirstOrDefault()?.MetadataSettings;
        var title = firstMeta?.title;
        var artist = firstMeta?.artist;
        var creator = firstMeta?.creator;

        return new ApiBeatmapSetCheckResult(
            general: new ApiCategoryCheckResult(
                checkResults: generalCheckResults,
                category: "General",
                beatmapId: null,
                mode: null,
                difficultyLevel: null
            ),
            difficulties: apiBeatmapCheckResults,
            beatmapSetId: firstMeta?.beatmapSetId,
            checks: checksDict,
            title: title,
            artist: artist,
            creator: creator
        );
    }

    public static ApiCategoryCheckResult? RunDifficultyCheckWithOverride(
        string beatmapSetFolder,
        string difficultyName,
        Beatmap.Difficulty overrideDifficulty)
    {
        var beatmapSet = new BeatmapSet(beatmapSetFolder);
        var beatmap = beatmapSet.Beatmaps.FirstOrDefault(b => b.MetadataSettings.version == difficultyName);

        if (beatmap == null)
            return null;

        // TODO improve so we run checks only for the overridden difficulty, not the whole set
        var issues = Checker.GetBeatmapSetIssues(beatmapSet);
        var checksById = CheckerRegistry.GetChecksWithId();

        var generalIssues = issues.Where(issue => issue.CheckOrigin is GeneralCheck).ToArray();

        var beatmapIssues = issues.Where(issue => issue.beatmap == beatmap)
            .Except(generalIssues)
            .Where(issue => issue.AppliesToDifficulty(overrideDifficulty));

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

        return new ApiCategoryCheckResult(
            checkResults: beatmapCheckResults,
            category: beatmap.MetadataSettings.version,
            beatmapId: beatmap.MetadataSettings.beatmapId,
            mode: beatmap.GeneralSettings.mode,
            difficultyLevel: overrideDifficulty
        );
    }
}
