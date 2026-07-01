using System.Collections.Concurrent;
using MapsetVerifier.Parser.Objects.Events;
using MapsetVerifier.Parser.Statics;

namespace MapsetVerifier.Parser.Objects
{
    public class BeatmapSet
    {
        public List<Beatmap> Beatmaps { get; }

        /// <summary> Used hit sound files' relative path from the song folder. </summary>
        public List<string> HitSoundFiles { get; }

        public Osb? Osb { get; private set; }
        public List<string> SongFilePaths { get; } = new();

        public string SongPath { get; }

        public BeatmapSet(string beatmapSetPath)
        {
            Beatmaps = new List<Beatmap>();
            Osb = null;
            SongPath = beatmapSetPath;

            Initalize(beatmapSetPath);

            HitSoundFiles = GetUsedHitSoundFiles().ToList();
            ApplyBeatmapSetDifficultyOverrides();
            SortBeatmapsByInterpretedOrder();
        }

        /// <summary>
        ///     Clears <see cref="Beatmap.InterpretedDifficultyOverride" /> on every beatmap in this set.
        /// </summary>
        public void ClearInterpretedDifficultyOverrides()
        {
            foreach (var beatmap in Beatmaps)
                beatmap.InterpretedDifficultyOverride = null;
        }

        /// <summary>
        ///     Resets overrides, assigns one beatmap an interpreted difficulty, and re-sorts <see cref="Beatmaps" />
        ///     so spread / set checks see consistent ordering.
        /// </summary>
        public void ApplyInterpretedDifficultyOverride(
            Beatmap beatmap,
            Beatmap.Difficulty difficulty
        )
        {
            ClearInterpretedDifficultyOverrides();
            beatmap.InterpretedDifficultyOverride = difficulty;
            SortBeatmapsByInterpretedOrder();
        }

        private void SortBeatmapsByInterpretedOrder()
        {
            var sorted = Beatmaps
                .OrderBy(beatmap => beatmap.GeneralSettings.mode)
                .ThenBy(beatmap => beatmap.GetDifficulty())
                .ThenBy(beatmap => beatmap.StarRating)
                .ThenBy(beatmap => beatmap.GetObjectDensity())
                .ToList();
            Beatmaps.Clear();
            Beatmaps.AddRange(sorted);
        }

        private void ApplyBeatmapSetDifficultyOverrides()
        {
            foreach (var beatmap in Beatmaps)
                beatmap.BeatmapSetDifficultyOverride = null;

            ApplyTaikoOniDifficultyOverrides();
        }

        private void ApplyTaikoOniDifficultyOverrides()
        {
            var taikoBeatmaps = Beatmaps
                .Where(beatmap => beatmap.GeneralSettings.mode == Beatmap.Mode.Taiko)
                .ToList();

            foreach (
                var oniBeatmap in taikoBeatmaps.Where(beatmap =>
                    beatmap.GetDifficultyFromName() == Beatmap.Difficulty.Insane
                    && beatmap.GetDifficulty() >= Beatmap.Difficulty.Expert
                )
            )
            {
                var hasHigherNamedDifficulty = taikoBeatmaps.Any(beatmap =>
                    beatmap != oniBeatmap
                    && beatmap.GetDifficultyFromName() >= Beatmap.Difficulty.Expert
                );
                var hasHigherStarRatingDifficulty = taikoBeatmaps.Any(beatmap =>
                    beatmap != oniBeatmap && beatmap.StarRating > oniBeatmap.StarRating
                );

                if (hasHigherNamedDifficulty || hasHigherStarRatingDifficulty)
                    oniBeatmap.BeatmapSetDifficultyOverride = Beatmap.Difficulty.Insane;
            }
        }

        private void Initalize(string beatmapSetPath)
        {
            if (!Directory.Exists(beatmapSetPath))
                throw new DirectoryNotFoundException(
                    "The folder \"" + beatmapSetPath + "\" does not exist."
                );

            var filePaths = Directory.GetFiles(beatmapSetPath, "*.*", SearchOption.AllDirectories);

            var beatmapFiles = new List<BeatmapFile>();

            foreach (var filePath in filePaths)
            {
                SongFilePaths.Add(filePath);

                if (!filePath.EndsWith(".osu"))
                    continue;

                var fileName = filePath.Substring(SongPath.Length + 1);
                var code = File.ReadAllText(filePath);

                beatmapFiles.Add(new BeatmapFile(fileName, code));
            }

            Beatmap.ClearCacheForSongPath(SongPath);
            var concurrentBeatmaps = new ConcurrentBag<Beatmap>();

            try
            {
                Parallel.ForEach(
                    beatmapFiles,
                    beatmapFile =>
                    {
                        concurrentBeatmaps.Add(
                            new Beatmap(beatmapFile.code, SongPath, beatmapFile.name)
                        );
                    }
                );
            }
            catch (AggregateException ex)
            {
                // Surface the real exception to the Server
                throw ex.Flatten().InnerException!;
            }

            foreach (var beatmap in concurrentBeatmaps)
                Beatmaps.Add(beatmap);

            var expectedOsbFileName = GetOsbFileName()?.ToLower();

            foreach (var filePath in filePaths)
            {
                var currentFileName = filePath.Substring(SongPath.Length + 1);

                if (filePath.EndsWith(".osb") && currentFileName.ToLower() == expectedOsbFileName)
                {
                    Osb = new Osb(File.ReadAllText(filePath));
                }
            }
        }

        /// <summary> Returns the expected .osb file name based on the metadata of the first beatmap if any exists, otherwise null. </summary>
        public string? GetOsbFileName()
        {
            var settings = Beatmaps.FirstOrDefault()?.MetadataSettings;

            if (settings == null)
                return null;

            var songArtist = settings.GetFileNameFiltered(settings.artist);
            var songTitle = settings.GetFileNameFiltered(settings.title);
            var songCreator = settings.GetFileNameFiltered(settings.creator);

            return songArtist + " - " + songTitle + " (" + songCreator + ").osb";
        }

        /// <summary> Returns the full audio file path of the first beatmap in the set if one exists, otherwise null. </summary>
        public string? GetAudioFilePath()
        {
            return Beatmaps.FirstOrDefault()?.GetAudioFilePath() ?? null;
        }

        /// <summary> Returns the audio file name of the first beatmap in the set if one exists, otherwise null. </summary>
        public string? GetAudioFileName()
        {
            return Beatmaps.FirstOrDefault()?.GeneralSettings.audioFileName ?? null;
        }

        /// <summary>
        ///     Returns the last file path matching the given search pattern, relative to the song folder.
        ///     The search pattern allows two wildcards: * = 0 or more, ? = 0 or 1.
        /// </summary>
        private string? GetLastMatchingFilePath(string? searchPattern)
        {
            if (searchPattern == null)
                return null;

            var lastMatchingPath = Directory
                .EnumerateFiles(SongPath, searchPattern, SearchOption.AllDirectories)
                .LastOrDefault();

            if (lastMatchingPath == null)
                return null;

            return PathStatic.RelativePath(lastMatchingPath, SongPath).Replace("\\", "/");
        }

        /// <summary> Returns all used hit sound files in the folder. </summary>
        public IEnumerable<string> GetUsedHitSoundFiles()
        {
            var hsFileNames = Beatmaps
                .SelectMany(beatmap =>
                    beatmap.HitObjects.SelectMany(hitObject => hitObject.GetUsedHitSoundFileNames())
                )
                .Distinct();

            foreach (var hsFileName in hsFileNames)
            {
                var path = GetLastMatchingFilePath($"*{hsFileName}.*");

                if (path == null)
                    continue;

                yield return path;
            }
        }

        /// <summary> Returns whether the given full file path is used by the beatmapset. </summary>
        public bool IsFileUsed(string filePath)
        {
            var relativePath = PathStatic.RelativePath(filePath, SongPath);
            var fileName = relativePath.Split(new[] { '/', '\\' }).Last().ToLower();
            var parsedPath = PathStatic.ParsePath(relativePath);
            var strippedPath = PathStatic.ParsePath(relativePath, true);

            if (fileName.EndsWith(".osu"))
                return true;

            if (IsAudioFile(parsedPath))
                return true;

            if (IsStoryboardFile(parsedPath, strippedPath))
                return true;

            if (IsHitObjectSampleFile(strippedPath))
                return true;

            if (IsHitSoundFile(parsedPath))
                return true;

            if (SkinStatic.IsUsed(fileName, this))
                return true;

            if (IsUsedOsbFile(fileName))
                return true;

            if (IsAnimationFrameFile(parsedPath))
                return true;

            return false;
        }

        private bool IsAudioFile(string? parsedPath)
        {
            return parsedPath != null
                && Beatmaps.Any(beatmap =>
                    beatmap.GeneralSettings.audioFileName.ToLower() == parsedPath
                );
        }

        private bool IsStoryboardFile(string? parsedPath, string? strippedPath)
        {
            return IsExactStoryboardReference(parsedPath)
                || IsExtensionlessStoryboardReference(parsedPath, strippedPath);
        }

        private bool IsExactStoryboardReference(string? parsedPath)
        {
            return parsedPath != null
                && (
                    Beatmaps.Any(beatmap =>
                        GetStoryboardPaths(beatmap, true).Any(path => IsSamePath(path, parsedPath))
                    )
                    || (
                        Osb != null
                        && GetStoryboardPaths(Osb, true).Any(path => IsSamePath(path, parsedPath))
                    )
                );
        }

        private bool IsExtensionlessStoryboardReference(string? parsedPath, string? strippedPath)
        {
            return parsedPath != null
                && strippedPath != null
                && parsedPath == GetLastMatchingFilePathWithStrippedPath(strippedPath)
                && (
                    Beatmaps.Any(beatmap =>
                        GetStoryboardPaths(beatmap, false)
                            .Any(path => IsExtensionlessReference(path, strippedPath))
                    )
                    || (
                        Osb != null
                        && GetStoryboardPaths(Osb, false)
                            .Any(path => IsExtensionlessReference(path, strippedPath))
                    )
                );
        }

        private bool IsHitObjectSampleFile(string? strippedPath)
        {
            return strippedPath != null
                && Beatmaps.Any(beatmap =>
                    beatmap.HitObjects.Any(hitObject =>
                        PathStatic.ParsePath(hitObject.filename, true) == strippedPath
                    )
                );
        }

        private bool IsHitSoundFile(string? parsedPath)
        {
            return parsedPath != null
                && HitSoundFiles.Any(hsPath => PathStatic.ParsePath(hsPath) == parsedPath);
        }

        private bool IsUsedOsbFile(string fileName)
        {
            return Osb != null && fileName == GetOsbFileName()?.ToLower() && Osb.IsUsed();
        }

        private bool IsAnimationFrameFile(string? parsedPath)
        {
            return Beatmaps.Any(beatmap => IsAnimationPathUsed(parsedPath, beatmap.Animations))
                || (Osb != null && IsAnimationPathUsed(parsedPath, Osb.animations));
        }

        private static bool IsSamePath(string? referencePath, string? parsedPath)
        {
            return parsedPath != null && PathStatic.ParsePath(referencePath) == parsedPath;
        }

        private static IEnumerable<string?> GetStoryboardPaths(
            Beatmap beatmap,
            bool includeAnimations
        )
        {
            foreach (var sprite in beatmap.Sprites)
                yield return sprite.path;
            foreach (var video in beatmap.Videos)
                yield return video.path;
            foreach (var background in beatmap.Backgrounds)
                yield return background.path;
            foreach (var sample in beatmap.Samples)
                yield return sample.path;

            if (!includeAnimations)
                yield break;

            foreach (var animation in beatmap.Animations)
                yield return animation.path;
        }

        private static IEnumerable<string?> GetStoryboardPaths(Osb osb, bool includeAnimations)
        {
            foreach (var sprite in osb.sprites)
                yield return sprite.path;
            foreach (var video in osb.videos)
                yield return video.path;
            foreach (var background in osb.backgrounds)
                yield return background.path;
            foreach (var sample in osb.samples)
                yield return sample.path;

            if (!includeAnimations)
                yield break;

            foreach (var animation in osb.animations)
                yield return animation.path;
        }

        /// <summary> Returns whether the given path (case insensitive) is used by any of the given animations. </summary>
        private static bool IsAnimationPathUsed(string? filePath, List<Animation> animations)
        {
            if (filePath == null)
            {
                return false;
            }

            foreach (var animation in animations)
            {
                foreach (var framePath in animation.framePaths)
                    if (
                        string.Equals(
                            framePath,
                            filePath,
                            StringComparison.CurrentCultureIgnoreCase
                        )
                    )
                        return true;
            }

            return false;
        }

        private string? GetLastMatchingFilePathWithStrippedPath(string? strippedPath)
        {
            if (strippedPath == null)
                return null;

            var lastMatchingPath = SongFilePaths.LastOrDefault(path =>
                PathStatic.ParsePath(PathStatic.RelativePath(path, SongPath), true) == strippedPath
            );

            return lastMatchingPath == null
                ? null
                : PathStatic.ParsePath(PathStatic.RelativePath(lastMatchingPath, SongPath));
        }

        private static bool IsExtensionlessReference(string? referencePath, string? strippedPath)
        {
            if (referencePath == null || strippedPath == null)
                return false;

            return PathStatic.ParsePath(referencePath) == strippedPath;
        }

        /// <summary> Returns the beatmapset as a string in the format "Artist - Title (Creator)". </summary>
        public override string ToString()
        {
            if (Beatmaps.Count > 0)
            {
                var settings = Beatmaps.First().MetadataSettings;

                var songArtist = settings.GetFileNameFiltered(settings.artist);
                var songTitle = settings.GetFileNameFiltered(settings.title);
                var songCreator = settings.GetFileNameFiltered(settings.creator);

                return songArtist + " - " + songTitle + " (" + songCreator + ")";
            }

            return "No beatmaps in set.";
        }

        private struct BeatmapFile
        {
            public readonly string name;
            public readonly string code;

            public BeatmapFile(string name, string code)
            {
                this.name = name;
                this.code = code;
            }
        }
    }
}
