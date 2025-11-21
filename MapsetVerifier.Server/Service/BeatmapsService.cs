using System.Text.RegularExpressions;
using MapsetVerifier.Server.Model;

namespace MapsetVerifier.Server.Service;

public static class BeatmapsService
{
    private static readonly Regex BackgroundRegex = new Regex("0,0,\"(?<file>[^\"]+)\"", RegexOptions.Compiled);

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
            return new ApiBeatmapPage(Enumerable.Empty<ApiBeatmap>(), page, pageSize, false, 0);

        var dirInfo = new DirectoryInfo(songsFolder);
        var folders = dirInfo.GetDirectories()
            .OrderByDescending(d => d.LastWriteTimeUtc)
            .ToList();

        int totalCount;
        if (string.IsNullOrWhiteSpace(search))
        {
            totalCount = folders.Count;
        }
        else
        {
            // Need to filter by search; approximate by checking metadata of each folder's first osu file.
            totalCount = 0;
            foreach (var folder in folders)
            {
                try
                {
                    var osuFile = Directory.GetFiles(folder.FullName, "*.osu").FirstOrDefault();
                    if (osuFile == null) continue;
                    var content = File.ReadLines(osuFile).Take(2000).Aggregate(string.Empty, (acc, line) => acc + line + "\n"); // partial read
                    var meta = ParseBeatmapMetadata(folder.FullName, content);
                    if (MatchesSearch(meta, search)) totalCount++;
                }
                catch
                {
                    // ignored
                }
            }
        }

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
                    var backgroundUrl = string.IsNullOrEmpty(meta.backgroundPath) ? string.Empty : $"/beatmaps/image?folder={Uri.EscapeDataString(folder.Name)}";
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
            catch { }
        }
        var hasMore = pageFolders.Count > pageSize;
        return new ApiBeatmapPage(results, page, pageSize, hasMore, totalCount);
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

    public static BeatmapImageResult GetBeatmapImage(string folder)
    {
        var songsFolder = DetectSongsFolder();
        if (string.IsNullOrWhiteSpace(songsFolder))
            return BeatmapImageResult.Error("Songs folder could not be detected.");
        var targetFolder = Path.Combine(songsFolder, folder);
        if (!Directory.Exists(targetFolder))
            return BeatmapImageResult.Error("Beatmap folder not found.");

        var imagePath = Directory.GetFiles(targetFolder)
            .FirstOrDefault(f => {
                var ext = Path.GetExtension(f).ToLowerInvariant();
                return ext == ".jpg" || ext == ".jpeg" || ext == ".png" || ext == ".gif";
            });
        if (imagePath == null)
            return BeatmapImageResult.Error("No background image found.");

        var etag = '"' + Path.GetFileName(imagePath) + '"';
        var extLower = Path.GetExtension(imagePath).ToLowerInvariant();
        var mime = extLower switch
        {
            ".png" => "image/png",
            ".gif" => "image/gif",
            _ => "image/jpeg"
        };
        return BeatmapImageResult.SuccessResult(imagePath, mime, etag);
    }
}
