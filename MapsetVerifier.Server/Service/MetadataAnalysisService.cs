using System.Numerics;
using MapsetVerifier.Framework.Objects.Resources;
using MapsetVerifier.Parser.Objects;
using MapsetVerifier.Parser.Statics;
using MapsetVerifier.Server.Model.MetadataAnalysis;
using Serilog;

namespace MapsetVerifier.Server.Service;

public static class MetadataAnalysisService
{
    public static MetadataAnalysisResult Analyze(string beatmapSetFolder)
    {
        try
        {
            var beatmapSet = new BeatmapSet(beatmapSetFolder);

            if (beatmapSet.Beatmaps.Count == 0)
                return MetadataAnalysisResult.CreateError("No beatmaps found in folder.");

            var difficulties = GetDifficultyMetadata(beatmapSet);
            var resources = GetResourcesInfo(beatmapSet);
            var colourSettings = GetColourSettings(beatmapSet);

            return MetadataAnalysisResult.CreateSuccess(difficulties, resources, colourSettings);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to analyze metadata for {Folder}", beatmapSetFolder);
            return MetadataAnalysisResult.CreateError($"Analysis failed: {ex.Message}");
        }
    }

    private static List<DifficultyMetadata> GetDifficultyMetadata(BeatmapSet beatmapSet)
    {
        return beatmapSet.Beatmaps.Select(beatmap => new DifficultyMetadata
        {
            Version = beatmap.MetadataSettings.version,
            Artist = beatmap.MetadataSettings.artist,
            ArtistUnicode = beatmap.MetadataSettings.artistUnicode,
            Title = beatmap.MetadataSettings.title,
            TitleUnicode = beatmap.MetadataSettings.titleUnicode,
            Creator = beatmap.MetadataSettings.creator,
            Source = beatmap.MetadataSettings.source,
            Tags = beatmap.MetadataSettings.tags,
            BeatmapId = beatmap.MetadataSettings.beatmapId,
            BeatmapSetId = beatmap.MetadataSettings.beatmapSetId,
            Mode = beatmap.GeneralSettings.mode.ToString(),
            StarRating = beatmap.StarRating
        }).ToList();
    }

    private static ResourcesInfo GetResourcesInfo(BeatmapSet beatmapSet)
    {
        var resources = new ResourcesInfo
        {
            HitSounds = GetHitSoundUsage(beatmapSet),
            Backgrounds = GetBackgroundInfo(beatmapSet),
            Videos = GetVideoInfo(beatmapSet),
            Storyboard = GetStoryboardInfo(beatmapSet),
            AudioFile = GetAudioFileInfo(beatmapSet)
        };

        // Calculate total folder size
        var directoryInfo = new DirectoryInfo(beatmapSet.SongPath);
        resources.TotalFolderSizeBytes = GetDirectorySize(directoryInfo);
        resources.TotalFolderSizeFormatted = FormatFileSize(resources.TotalFolderSizeBytes);

        return resources;
    }

    private static List<HitSoundUsage> GetHitSoundUsage(BeatmapSet beatmapSet)
    {
        var hitSoundUsages = new Dictionary<string, HitSoundUsage>();

        foreach (var hsFile in beatmapSet.HitSoundFiles)
        {
            var fullPath = Path.Combine(beatmapSet.SongPath, hsFile);
            var usage = new HitSoundUsage
            {
                FileName = hsFile,
                UsagePerDifficulty = []
            };

            if (File.Exists(fullPath))
            {
                try
                {
                    var fileInfo = new FileInfo(fullPath);
                    usage.FileSizeBytes = fileInfo.Length;
                    usage.FileSizeFormatted = FormatFileSize(fileInfo.Length);
                    usage.Format = AudioBASS.EnumToString(AudioBASS.GetFormat(fullPath));
                    usage.DurationMs = AudioBASS.GetDuration(fullPath);
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "Failed to get hit sound info for {File}", hsFile);
                }
            }

            // Get usage per difficulty
            var fileNameWithoutExt = Path.GetFileNameWithoutExtension(hsFile);
            foreach (var beatmap in beatmapSet.Beatmaps)
            {
                var usedFiles = beatmap.HitObjects
                    .SelectMany(obj => obj.GetUsedHitSoundFileNames())
                    .Where(name => name.Equals(fileNameWithoutExt, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                if (usedFiles.Count > 0)
                {
                    var timestamps = beatmap.HitObjects
                        .Where(obj => obj.GetUsedHitSoundFileNames()
                            .Any(name => name.Equals(fileNameWithoutExt, StringComparison.OrdinalIgnoreCase)))
                        .Take(10)
                        .Select(obj => Timestamp.Get(obj.time))
                        .ToList();

                    usage.UsagePerDifficulty.Add(new DifficultyHitSoundUsage
                    {
                        Version = beatmap.MetadataSettings.version,
                        UsageCount = usedFiles.Count,
                        Timestamps = timestamps
                    });
                }
            }

            usage.TotalUsageCount = usage.UsagePerDifficulty.Sum(u => u.UsageCount);
            hitSoundUsages[hsFile] = usage;
        }

        return hitSoundUsages.Values.OrderByDescending(u => u.TotalUsageCount).ToList();
    }

    private static List<BackgroundInfo> GetBackgroundInfo(BeatmapSet beatmapSet)
    {
        var backgrounds = new Dictionary<string, BackgroundInfo>();

        foreach (var beatmap in beatmapSet.Beatmaps)
        {
            foreach (var bg in beatmap.Backgrounds)
            {
                if (bg.path == null) continue;

                if (!backgrounds.TryGetValue(bg.path, out var bgInfo))
                {
                    bgInfo = new BackgroundInfo { FileName = bg.path, UsedByDifficulties = [] };
                    var fullPath = Path.Combine(beatmapSet.SongPath, bg.path);

                    if (File.Exists(fullPath))
                    {
                        try
                        {
                            var fileInfo = new FileInfo(fullPath);
                            bgInfo.FileSizeBytes = fileInfo.Length;
                            bgInfo.FileSizeFormatted = FormatFileSize(fileInfo.Length);
                            var tagFile = new FileAbstraction(fullPath).GetTagFile();
                            bgInfo.Width = tagFile.Properties.PhotoWidth;
                            bgInfo.Height = tagFile.Properties.PhotoHeight;
                            bgInfo.Resolution = $"{bgInfo.Width} × {bgInfo.Height}";
                        }
                        catch (Exception ex) { Log.Warning(ex, "Failed to get bg info for {File}", bg.path); }
                    }
                    backgrounds[bg.path] = bgInfo;
                }
                bgInfo.UsedByDifficulties.Add(beatmap.MetadataSettings.version);
            }
        }
        return backgrounds.Values.ToList();
    }

    private static List<VideoInfo> GetVideoInfo(BeatmapSet beatmapSet)
    {
        var videos = new Dictionary<string, VideoInfo>();

        foreach (var beatmap in beatmapSet.Beatmaps)
        {
            foreach (var video in beatmap.Videos)
            {
                if (video.path == null) continue;

                if (!videos.TryGetValue(video.path, out var videoInfo))
                {
                    videoInfo = new VideoInfo { FileName = video.path, OffsetMs = video.offset, UsedByDifficulties = [] };
                    var fullPath = Path.Combine(beatmapSet.SongPath, video.path);

                    if (File.Exists(fullPath))
                    {
                        try
                        {
                            var fileInfo = new FileInfo(fullPath);
                            videoInfo.FileSizeBytes = fileInfo.Length;
                            videoInfo.FileSizeFormatted = FormatFileSize(fileInfo.Length);
                            var tagFile = new FileAbstraction(fullPath).GetTagFile();
                            videoInfo.Width = tagFile.Properties.VideoWidth;
                            videoInfo.Height = tagFile.Properties.VideoHeight;
                            videoInfo.Resolution = $"{videoInfo.Width} × {videoInfo.Height}";
                            videoInfo.DurationMs = tagFile.Properties.Duration.TotalMilliseconds;
                            videoInfo.DurationFormatted = Timestamp.Get(videoInfo.DurationMs);
                        }
                        catch (Exception ex) { Log.Warning(ex, "Failed to get video info for {File}", video.path); }
                    }
                    videos[video.path] = videoInfo;
                }
                videoInfo.UsedByDifficulties.Add(beatmap.MetadataSettings.version);
            }
        }

        // Also check OSB videos
        if (beatmapSet.Osb != null)
        {
            foreach (var video in beatmapSet.Osb.videos)
            {
                if (video.path == null || videos.ContainsKey(video.path)) continue;

                var videoInfo = new VideoInfo { FileName = video.path, OffsetMs = video.offset, UsedByDifficulties = ["(Storyboard)"] };
                var fullPath = Path.Combine(beatmapSet.SongPath, video.path);

                if (File.Exists(fullPath))
                {
                    try
                    {
                        var fileInfo = new FileInfo(fullPath);
                        videoInfo.FileSizeBytes = fileInfo.Length;
                        videoInfo.FileSizeFormatted = FormatFileSize(fileInfo.Length);
                        var tagFile = new FileAbstraction(fullPath).GetTagFile();
                        videoInfo.Width = tagFile.Properties.VideoWidth;
                        videoInfo.Height = tagFile.Properties.VideoHeight;
                        videoInfo.Resolution = $"{videoInfo.Width} × {videoInfo.Height}";
                        videoInfo.DurationMs = tagFile.Properties.Duration.TotalMilliseconds;
                        videoInfo.DurationFormatted = Timestamp.Get(videoInfo.DurationMs);
                    }
                    catch (Exception ex) { Log.Warning(ex, "Failed to get video info for {File}", video.path); }
                }
                videos[video.path] = videoInfo;
            }
        }

        return videos.Values.ToList();
    }

    private static StoryboardInfo GetStoryboardInfo(BeatmapSet beatmapSet)
    {
        var info = new StoryboardInfo
        {
            HasOsb = beatmapSet.Osb != null,
            OsbFileName = beatmapSet.GetOsbFileName(),
            OsbIsUsed = beatmapSet.Osb?.IsUsed() ?? false,
            DifficultySpecificStoryboards = []
        };

        foreach (var beatmap in beatmapSet.Beatmaps)
        {
            info.DifficultySpecificStoryboards.Add(new DifficultyStoryboardInfo
            {
                Version = beatmap.MetadataSettings.version,
                HasStoryboard = beatmap.HasDifficultySpecificStoryboard(),
                SpriteCount = beatmap.Sprites.Count,
                AnimationCount = beatmap.Animations.Count,
                SampleCount = beatmap.Samples.Count
            });
        }

        return info;
    }

    private static AudioFileInfo? GetAudioFileInfo(BeatmapSet beatmapSet)
    {
        var audioPath = beatmapSet.GetAudioFilePath();
        if (audioPath == null || !File.Exists(audioPath)) return null;

        try
        {
            var fileInfo = new FileInfo(audioPath);
            return new AudioFileInfo
            {
                FileName = beatmapSet.GetAudioFileName() ?? Path.GetFileName(audioPath),
                FileSizeBytes = fileInfo.Length,
                FileSizeFormatted = FormatFileSize(fileInfo.Length),
                DurationMs = AudioBASS.GetDuration(audioPath),
                DurationFormatted = Timestamp.Get(AudioBASS.GetDuration(audioPath)),
                Format = AudioBASS.EnumToString(AudioBASS.GetFormat(audioPath)),
                AverageBitrate = Math.Round(AudioBASS.GetBitrate(audioPath))
            };
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to get audio file info");
            return null;
        }
    }

    private static List<DifficultyColourSettings> GetColourSettings(BeatmapSet beatmapSet)
    {
        return beatmapSet.Beatmaps.Select(beatmap =>
        {
            var mode = beatmap.GeneralSettings.mode;
            var isApplicable = mode != Beatmap.Mode.Taiko && mode != Beatmap.Mode.Mania;

            var settings = new DifficultyColourSettings
            {
                Version = beatmap.MetadataSettings.version,
                Mode = mode.ToString(),
                IsApplicable = isApplicable,
                ComboColours = [],
                SliderBorder = null,
                SliderTrack = null
            };

            if (!isApplicable) return settings;

            // Combo colours - account for that colour at index 0 is actually last displayed in-game
            for (var i = 0; i < beatmap.ColourSettings.combos.Count; i++)
            {
                var displayIndex = i == 0 ? beatmap.ColourSettings.combos.Count : i;
                var colour = beatmap.ColourSettings.combos[i];
                settings.ComboColours.Add(CreateComboColourInfo(displayIndex, colour));
            }

            // Sort by display index
            settings.ComboColours = settings.ComboColours.OrderBy(c => c.Index).ToList();

            if (beatmap.ColourSettings.sliderBorder.HasValue)
                settings.SliderBorder = CreateColourInfo(beatmap.ColourSettings.sliderBorder.Value, false);

            if (beatmap.ColourSettings.sliderTrackOverride.HasValue)
                settings.SliderTrack = CreateColourInfo(beatmap.ColourSettings.sliderTrackOverride.Value, false);

            return settings;
        }).ToList();
    }

    private static ComboColourInfo CreateComboColourInfo(int index, Vector3 colour)
    {
        var luminosity = GetHspLuminosity(colour);
        return new ComboColourInfo
        {
            Index = index,
            R = (int)colour.X,
            G = (int)colour.Y,
            B = (int)colour.Z,
            Hex = $"#{(int)colour.X:X2}{(int)colour.Y:X2}{(int)colour.Z:X2}",
            HspLuminosity = luminosity,
            LuminosityWarning = GetLuminosityWarning(luminosity, true)
        };
    }

    private static ColourInfo CreateColourInfo(Vector3 colour, bool isCombo)
    {
        var luminosity = GetHspLuminosity(colour);
        return new ColourInfo
        {
            R = (int)colour.X,
            G = (int)colour.Y,
            B = (int)colour.Z,
            Hex = $"#{(int)colour.X:X2}{(int)colour.Y:X2}{(int)colour.Z:X2}",
            HspLuminosity = luminosity,
            LuminosityWarning = GetLuminosityWarning(luminosity, isCombo)
        };
    }

    private static float GetHspLuminosity(Vector3 colour) =>
        (float)Math.Sqrt(colour.X * colour.X * 0.299f + colour.Y * colour.Y * 0.587f + colour.Z * colour.Z * 0.114f);

    private static string GetLuminosityWarning(float luminosity, bool checkKiai)
    {
        if (luminosity < 43)
            return "Too dark (< 43)";
        if (checkKiai && luminosity > 250)
            return "Too bright for kiai (> 250)";
        return string.Empty;
    }

    private static long GetDirectorySize(DirectoryInfo directoryInfo)
    {
        long totalSize = 0;
        foreach (var fileInfo in directoryInfo.GetFiles())
            totalSize += fileInfo.Length;
        foreach (var subdirectoryInfo in directoryInfo.GetDirectories())
            totalSize += GetDirectorySize(subdirectoryInfo);
        return totalSize;
    }

    private static string FormatFileSize(long bytes)
    {
        if (bytes >= 1024 * 1024 * 1024)
            return $"{bytes / (1024.0 * 1024.0 * 1024.0):0.##} GB";
        if (bytes >= 1024 * 1024)
            return $"{bytes / (1024.0 * 1024.0):0.##} MB";
        if (bytes >= 1024)
            return $"{bytes / 1024.0:0.##} KB";
        return $"{bytes} B";
    }
}

