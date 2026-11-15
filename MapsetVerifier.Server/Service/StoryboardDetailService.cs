using MapsetVerifier.Framework.Objects.Resources;
using MapsetVerifier.Parser.Objects;
using MapsetVerifier.Parser.Objects.Events;
using MapsetVerifier.Parser.Statics;
using MapsetVerifier.Server.Model.MetadataAnalysis;
using Serilog;

namespace MapsetVerifier.Server.Service;

/// <summary>
/// Reads what a beatmap set's storyboard is made of, for the storyboard's page on Resources. Only
/// what the storyboard holds; whether any of it breaks a ranking rule is for Checks to say.
/// </summary>
public static class StoryboardDetailService
{
    /// <summary>The order osu! draws sprite layers in, back to front.</summary>
    private static readonly Sprite.Layer[] LayerOrder =
    [
        Sprite.Layer.Background,
        Sprite.Layer.Fail,
        Sprite.Layer.Pass,
        Sprite.Layer.Foreground,
        Sprite.Layer.Overlay,
        Sprite.Layer.Unknown,
    ];

    /// <summary>The storyboard elements of one source: the .osb, or one difficulty's own.</summary>
    private sealed record SourceElements(
        string Name,
        bool IsOsb,
        List<Sprite> Sprites,
        List<Animation> Animations,
        List<Sample> Samples
    );

    public static StoryboardDetailResult Analyze(string beatmapSetFolder)
    {
        try
        {
            var beatmapSet = new BeatmapSet(beatmapSetFolder);
            var sources = GetSources(beatmapSet);

            var result = new StoryboardDetailResult { Success = true };

            foreach (var source in sources)
            {
                result.SpriteCount += source.Sprites.Count;
                result.AnimationCount += source.Animations.Count;
                result.SampleCount += source.Samples.Count;
                result.Sources.Add(
                    new StoryboardSource
                    {
                        Name = source.Name,
                        IsOsb = source.IsOsb,
                        SpriteCount = source.Sprites.Count,
                        AnimationCount = source.Animations.Count,
                        SampleCount = source.Samples.Count,
                    }
                );
            }

            result.UsedByDifficulties = GetUsedByDifficulties(beatmapSet, sources);
            result.Layers = GetLayerCounts(sources);

            result.Images = GetImages(beatmapSet, sources);
            result.ImageFileCount = result.Images.Sum(image =>
                Math.Max(0, image.FrameCount - image.MissingFileCount)
            );
            result.ImageSizeBytes = result.Images.Sum(image => image.FileSizeBytes);
            result.ImageSizeFormatted = FileSizeFormatter.Format(result.ImageSizeBytes);

            result.Samples = sources
                .SelectMany(source =>
                    source.Samples.Select(sample => new StoryboardSampleEvent
                    {
                        Time = sample.time,
                        Timestamp = Timestamp.Get(sample.time),
                        Path = sample.path ?? string.Empty,
                        Volume = sample.volume,
                        Layer = sample.layer.ToString(),
                        Source = source.Name,
                    })
                )
                .Where(sample => sample.Path.Length > 0)
                .OrderBy(sample => sample.Time)
                .ToList();
            result.SampleFiles = GetSampleFiles(beatmapSet, result.Samples);
            result.SampleSizeBytes = result.SampleFiles.Sum(file => file.FileSizeBytes);
            result.SampleSizeFormatted = FileSizeFormatter.Format(result.SampleSizeBytes);

            result.SongDurationMs = GetSongDurationMs(beatmapSet);

            return result;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to read the storyboard of {Folder}", beatmapSetFolder);
            return StoryboardDetailResult.CreateError(
                $"Reading the storyboard failed: {ex.Message}"
            );
        }
    }

    /// <summary>
    /// Every image and sample path the set's storyboards use, animation frames one by one, so the
    /// file endpoint can serve them.
    /// </summary>
    public static IEnumerable<string> GetUsedFilePaths(BeatmapSet beatmapSet)
    {
        foreach (var source in GetSources(beatmapSet))
        {
            foreach (var sprite in source.Sprites)
                if (sprite.path != null)
                    yield return sprite.path;

            foreach (var animation in source.Animations)
            foreach (var framePath in animation.framePaths)
                yield return framePath;

            foreach (var sample in source.Samples)
                if (sample.path != null)
                    yield return sample.path;
        }
    }

    private static List<SourceElements> GetSources(BeatmapSet beatmapSet)
    {
        var sources = new List<SourceElements>();

        if (beatmapSet.Osb != null)
            sources.Add(
                new SourceElements(
                    beatmapSet.GetOsbFileName() ?? ".osb",
                    true,
                    beatmapSet.Osb.sprites.Where(sprite => sprite is not Animation).ToList(),
                    beatmapSet.Osb.animations,
                    beatmapSet.Osb.samples
                )
            );

        foreach (var beatmap in beatmapSet.InOverviewOrder())
        {
            var sprites = beatmap.Sprites.Where(sprite => sprite is not Animation).ToList();
            if (sprites.Count == 0 && beatmap.Animations.Count == 0 && beatmap.Samples.Count == 0)
                continue;

            sources.Add(
                new SourceElements(
                    beatmap.MetadataSettings.version,
                    false,
                    sprites,
                    beatmap.Animations,
                    beatmap.Samples
                )
            );
        }

        return sources;
    }

    /// <summary>A used .osb shows on every difficulty; otherwise only difficulties with their own elements.</summary>
    private static List<string> GetUsedByDifficulties(
        BeatmapSet beatmapSet,
        List<SourceElements> sources
    )
    {
        var osbIsUsed = beatmapSet.Osb?.IsUsed() ?? false;
        var ownStoryboards = sources
            .Where(source => !source.IsOsb)
            .Select(source => source.Name)
            .ToHashSet();

        return beatmapSet
            .InOverviewOrder()
            .Select(beatmap => beatmap.MetadataSettings.version)
            .Where(version => osbIsUsed || ownStoryboards.Contains(version))
            .ToList();
    }

    private static List<StoryboardLayerCount> GetLayerCounts(List<SourceElements> sources)
    {
        var layers = sources
            .SelectMany(source => source.Sprites.Concat(source.Animations))
            .GroupBy(sprite => sprite.layer)
            .ToDictionary(group => group.Key, group => group.Count());

        return LayerOrder
            .Where(layers.ContainsKey)
            .Select(layer => new StoryboardLayerCount
            {
                Layer = layer.ToString(),
                Count = layers[layer],
            })
            .ToList();
    }

    /// <summary>Each image once, animations by their frames together, heaviest first.</summary>
    private static List<StoryboardImage> GetImages(
        BeatmapSet beatmapSet,
        List<SourceElements> sources
    )
    {
        var images = new Dictionary<string, StoryboardImage>(StringComparer.OrdinalIgnoreCase);

        void Use(
            string path,
            bool isAnimation,
            Sprite sprite,
            string source,
            Func<StoryboardImage> create
        )
        {
            var key = (isAnimation ? "animation:" : "sprite:") + path;
            if (!images.TryGetValue(key, out var image))
            {
                image = create();
                images[key] = image;
            }

            image.UseCount++;
            var layer = sprite.layer.ToString();
            if (!image.Layers.Contains(layer))
                image.Layers.Add(layer);
            if (!image.Sources.Contains(source))
                image.Sources.Add(source);
        }

        foreach (var source in sources)
        {
            foreach (var sprite in source.Sprites)
            {
                if (sprite.path == null)
                    continue;

                Use(
                    sprite.path,
                    false,
                    sprite,
                    source.Name,
                    () => ReadImage(beatmapSet, sprite.path, [sprite.path])
                );
            }

            foreach (var animation in source.Animations)
            {
                if (animation.path == null)
                    continue;

                Use(
                    animation.path,
                    true,
                    animation,
                    source.Name,
                    () => ReadImage(beatmapSet, animation.path, animation.framePaths)
                );
            }
        }

        foreach (var image in images.Values)
            image.Layers = LayerOrder
                .Select(layer => layer.ToString())
                .Where(image.Layers.Contains)
                .ToList();

        return images
            .Values.OrderBy(image => image.MissingFileCount == 0)
            .ThenByDescending(image => image.FileSizeBytes)
            .ThenBy(image => image.Path, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    /// <summary>Sizes up an image, or an animation from all its frames, as found in the folder.</summary>
    private static StoryboardImage ReadImage(
        BeatmapSet beatmapSet,
        string path,
        List<string> filePaths
    )
    {
        var isAnimation = filePaths.Count != 1 || filePaths[0] != path;
        var image = new StoryboardImage
        {
            Path = path,
            IsAnimation = isAnimation,
            FrameCount = isAnimation ? filePaths.Count : 1,
            PreviewPath = filePaths.FirstOrDefault() ?? path,
        };

        var previewFound = false;
        foreach (var filePath in filePaths)
        {
            var fullPath = Path.Combine(beatmapSet.SongPath, filePath);
            if (!File.Exists(fullPath))
            {
                image.MissingFileCount++;
                image.MissingFiles.Add(filePath);
                continue;
            }

            image.FileSizeBytes += new FileInfo(fullPath).Length;

            if (previewFound)
                continue;
            previewFound = true;
            image.PreviewPath = filePath;

            try
            {
                var tagFile = new FileAbstraction(fullPath).GetTagFile();
                if (tagFile != null)
                {
                    image.Width = tagFile.Properties.PhotoWidth;
                    image.Height = tagFile.Properties.PhotoHeight;
                    image.Resolution = $"{image.Width} × {image.Height}";
                }
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Failed to read the resolution of {File}", filePath);
            }
        }

        if (filePaths.Count == 0)
        {
            image.MissingFileCount = 1;
            image.MissingFiles.Add(path);
        }

        image.FileSizeFormatted = FileSizeFormatter.Format(image.FileSizeBytes);
        return image;
    }

    /// <summary>Each sample file once, most used first.</summary>
    private static List<StoryboardSampleFile> GetSampleFiles(
        BeatmapSet beatmapSet,
        List<StoryboardSampleEvent> samples
    )
    {
        return samples
            .GroupBy(sample => sample.Path, StringComparer.OrdinalIgnoreCase)
            .Select(group =>
            {
                var file = new StoryboardSampleFile
                {
                    Path = group.First().Path,
                    UseCount = group.Count(),
                    FirstTimestamp = group.First().Timestamp,
                };

                var fullPath = Path.Combine(beatmapSet.SongPath, file.Path);
                if (File.Exists(fullPath))
                {
                    file.Exists = true;
                    file.FileSizeBytes = new FileInfo(fullPath).Length;
                }

                file.FileSizeFormatted = FileSizeFormatter.Format(file.FileSizeBytes);
                return file;
            })
            .OrderByDescending(file => file.UseCount)
            .ThenBy(file => file.Path, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static double GetSongDurationMs(BeatmapSet beatmapSet)
    {
        var audioPath = beatmapSet.GetAudioFilePath();
        if (audioPath == null || !File.Exists(audioPath))
            return 0;

        try
        {
            return AudioBASS.GetDuration(audioPath);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to read the song's length for the storyboard samples");
            return 0;
        }
    }
}
