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

        /// <summary>
        ///     Relative paths of .osu files that contained no data and were excluded from
        ///     <see cref="Beatmaps" /> entirely, rather than being treated as a (broken) difficulty.
        /// </summary>
        public List<string> EmptyBeatmapFiles { get; } = new();

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

                if (string.IsNullOrWhiteSpace(code))
                {
                    EmptyBeatmapFiles.Add(fileName);

                    continue;
                }

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

        // Built once per instance instead of re-enumerating every sprite/video/background/sample/
        // animation reference (across every beatmap and the .osb) on every IsFileUsed call - for
        // storyboard-heavy mapsets this was the dominant cost, since it's an O(storyboard element
        // count) scan repeated once per song file.
        private HashSet<string>? storyboardPathsExcludingAnimations;
        private HashSet<string>? storyboardPathsIncludingAnimations;

        private void EnsureStoryboardPathSets()
        {
            if (storyboardPathsExcludingAnimations != null)
                return;

            var excludingAnimations = new HashSet<string>();
            var animationsOnly = new HashSet<string>();

            void AddParsedPaths(IEnumerable<string?> paths, HashSet<string> target)
            {
                foreach (var path in paths)
                {
                    var parsed = PathStatic.ParsePath(path);

                    if (parsed != null)
                        target.Add(parsed);
                }
            }

            foreach (var beatmap in Beatmaps)
            {
                AddParsedPaths(GetStoryboardPaths(beatmap, false), excludingAnimations);
                AddParsedPaths(
                    beatmap.Animations.Select(animation => animation.path),
                    animationsOnly
                );
            }

            if (Osb != null)
            {
                AddParsedPaths(GetStoryboardPaths(Osb, false), excludingAnimations);
                AddParsedPaths(Osb.animations.Select(animation => animation.path), animationsOnly);
            }

            storyboardPathsExcludingAnimations = excludingAnimations;
            storyboardPathsIncludingAnimations = [.. excludingAnimations, .. animationsOnly];
        }

        private bool IsExactStoryboardReference(string? parsedPath)
        {
            if (parsedPath == null)
                return false;

            EnsureStoryboardPathSets();

            return storyboardPathsIncludingAnimations!.Contains(parsedPath);
        }

        private bool IsExtensionlessStoryboardReference(string? parsedPath, string? strippedPath)
        {
            if (
                parsedPath == null
                || strippedPath == null
                || parsedPath != GetLastMatchingFilePathWithStrippedPath(strippedPath)
            )
                return false;

            EnsureStoryboardPathSets();

            return storyboardPathsExcludingAnimations!.Contains(strippedPath);
        }

        // Built once per instance instead of scanning all SongFilePaths on every call.
        private Dictionary<string, string>? lastMatchingPathByStrippedPath;

        private string? GetLastMatchingFilePathWithStrippedPath(string? strippedPath)
        {
            if (strippedPath == null)
                return null;

            if (lastMatchingPathByStrippedPath == null)
            {
                lastMatchingPathByStrippedPath = new Dictionary<string, string>();

                foreach (var path in SongFilePaths)
                {
                    var relativePath = PathStatic.RelativePath(path, SongPath);
                    var stripped = PathStatic.ParsePath(relativePath, true);

                    if (stripped == null)
                        continue;

                    var parsed = PathStatic.ParsePath(relativePath);

                    if (parsed != null)
                        // Later entries overwrite earlier ones, matching the original
                        // SongFilePaths.LastOrDefault(...) semantics.
                        lastMatchingPathByStrippedPath[stripped] = parsed;
                }
            }

            return lastMatchingPathByStrippedPath.GetValueOrDefault(strippedPath);
        }

        private HashSet<string>? hitObjectSampleFileNames;

        private bool IsHitObjectSampleFile(string? strippedPath)
        {
            if (strippedPath == null)
                return false;

            // Built once per instance instead of rescanning every beatmap's hit objects on every
            // call - IsFileUsed (and therefore this) gets called once per song file, so without
            // this the whole hit object list gets walked once per file in the folder.
            hitObjectSampleFileNames ??=
            [
                .. Beatmaps
                    .SelectMany(beatmap => beatmap.HitObjects)
                    .Select(hitObject => PathStatic.ParsePath(hitObject.filename, true))
                    .Where(path => path != null)
                    .Select(path => path!),
            ];

            return hitObjectSampleFileNames.Contains(strippedPath);
        }

        private HashSet<string>? hitSoundFileParsedPaths;

        private bool IsHitSoundFile(string? parsedPath)
        {
            if (parsedPath == null)
                return false;

            // Built once per instance instead of rescanning all HitSoundFiles on every call -
            // IsFileUsed (and therefore this) gets called once per song file, so without this a
            // mapset with N hit sound files does an O(N) scan for every one of those N files.
            hitSoundFileParsedPaths ??=
            [
                .. HitSoundFiles
                    .Select(hsPath => PathStatic.ParsePath(hsPath))
                    .Where(path => path != null)
                    .Select(path => path!),
            ];

            return hitSoundFileParsedPaths.Contains(parsedPath);
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
