using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MapsetVerifier.Parser.Objects.Events;
using MapsetVerifier.Parser.Statics;

namespace MapsetVerifier.Parser.Objects
{
    public class BeatmapSet
    {
        public List<Beatmap> Beatmaps { get; }

        /// <summary> Used hit sound files' relative path from the song folder. </summary>
        public List<string> HitSoundFiles { get; }

        public Osb Osb { get; private set; }
        public List<string> SongFilePaths  { get; } = new();

        public string SongPath { get; }

        public BeatmapSet(string beatmapSetPath)
        {
            var mapsetTrack = new Track("Parsing mapset \"" + PathStatic.CutPath(beatmapSetPath) + "\"...");

            Beatmaps = new List<Beatmap>();
            Osb = null;
            SongPath = beatmapSetPath;

            Initalize(beatmapSetPath);

            var hsTrack = new Track("Finding hit sound files...");
            HitSoundFiles = GetUsedHitSoundFiles().ToList();
            hsTrack.Complete();

            Beatmaps = Beatmaps.OrderBy(beatmap => beatmap.GeneralSettings.mode).ThenBy(beatmap => beatmap.GetDifficulty(true)).ThenBy(beatmap => beatmap.StarRating).ThenBy(beatmap => beatmap.GetObjectDensity()).ToList();

            mapsetTrack.Complete();
        }

        private void Initalize(string beatmapSetPath)
        {
            if (!Directory.Exists(beatmapSetPath))
                throw new DirectoryNotFoundException("The folder \"" + beatmapSetPath + "\" does not exist.");

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

            Beatmap.ClearCache();
            var concurrentBeatmaps = new ConcurrentBag<Beatmap>();

            Parallel.ForEach(beatmapFiles, beatmapFile =>
            {
                var beatmapTrack = new Track("Parsing " + beatmapFile.name + "...");
                concurrentBeatmaps.Add(new Beatmap(beatmapFile.code, SongPath, beatmapFile.name));
                beatmapTrack.Complete();
            });

            foreach (var beatmap in concurrentBeatmaps)
                Beatmaps.Add(beatmap);

            var expectedOsbFileName = GetOsbFileName()?.ToLower();

            foreach (var filePath in filePaths)
            {
                var currentFileName = filePath.Substring(SongPath.Length + 1);

                if (filePath.EndsWith(".osb") && currentFileName.ToLower() == expectedOsbFileName)
                {
                    var osbTrack = new Track("Parsing " + currentFileName + "...");
                    Osb = new Osb(File.ReadAllText(filePath));
                    osbTrack.Complete();
                }
            }
        }

        /// <summary> Returns the expected .osb file name based on the metadata of the first beatmap if any exists, otherwise null. </summary>
        public string GetOsbFileName()
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
        public string GetAudioFilePath() => Beatmaps.FirstOrDefault(beatmap => beatmap != null)?.GetAudioFilePath() ?? null;

        /// <summary> Returns the audio file name of the first beatmap in the set if one exists, otherwise null. </summary>
        public string GetAudioFileName() => Beatmaps.FirstOrDefault(beatmap => beatmap != null)?.GeneralSettings.audioFileName ?? null;

        /// <summary>
        ///     Returns the last file path matching the given search pattern, relative to the song folder.
        ///     The search pattern allows two wildcards: * = 0 or more, ? = 0 or 1.
        /// </summary>
        private string GetLastMatchingFilePath(string searchPattern)
        {
            var lastMatchingPath = Directory.EnumerateFiles(SongPath, searchPattern, SearchOption.AllDirectories).LastOrDefault();

            if (lastMatchingPath == null)
                return null;

            return PathStatic.RelativePath(lastMatchingPath, SongPath).Replace("\\", "/");
        }

        /// <summary> Returns all used hit sound files in the folder. </summary>
        public IEnumerable<string> GetUsedHitSoundFiles()
        {
            var hsFileNames = Beatmaps.SelectMany(beatmap => beatmap.HitObjects.SelectMany(hitObject => hitObject.GetUsedHitSoundFileNames())).Distinct();

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

            if (Beatmaps.Any(beatmap => beatmap.GeneralSettings.audioFileName.ToLower() == parsedPath))
                return true;

            // When the path is "go", and "go.png" is over "go.jpg" in order, then "go.jpg" will be the one used.
            // So we basically want to find the last path which matches the name.
            var lastMatchingPath = PathStatic.ParsePath(GetLastMatchingFilePath(parsedPath));

            // These are always used, but you won't be able to update them unless they have the right format.
            if (fileName.EndsWith(".osu"))
                return true;

            if (Beatmaps.Any(beatmap => beatmap.Sprites.Any(element => element.path.ToLower() == parsedPath) || beatmap.Videos.Any(element => element.path.ToLower() == parsedPath) || beatmap.Backgrounds.Any(element => element.path.ToLower() == parsedPath) || beatmap.Animations.Any(element => element.path.ToLower() == parsedPath) || beatmap.Samples.Any(element => element.path.ToLower() == parsedPath)))
                return true;

            // animations cannot be stripped of their extension
            if (Beatmaps.Any(beatmap => beatmap.Sprites.Any(element => element.strippedPath == strippedPath) || beatmap.Videos.Any(element => element.strippedPath == strippedPath) || beatmap.Backgrounds.Any(element => element.strippedPath == strippedPath) || beatmap.Samples.Any(element => element.strippedPath == strippedPath)) && parsedPath == lastMatchingPath)
                return true;

            if (Osb != null && (Osb.sprites.Any(element => element.path.ToLower() == parsedPath) || Osb.videos.Any(element => element.path.ToLower() == parsedPath) || Osb.backgrounds.Any(element => element.path.ToLower() == parsedPath) || Osb.animations.Any(element => element.path.ToLower() == parsedPath) || Osb.samples.Any(element => element.path.ToLower() == parsedPath)))
                return true;

            if (Osb != null && (Osb.sprites.Any(element => element.strippedPath == strippedPath) || Osb.videos.Any(element => element.strippedPath == strippedPath) || Osb.backgrounds.Any(element => element.strippedPath == strippedPath) || Osb.samples.Any(element => element.strippedPath == strippedPath)) && parsedPath == lastMatchingPath)
                return true;

            if (Beatmaps.Any(beatmap => beatmap.HitObjects.Any(hitObject => (hitObject.filename != null ? PathStatic.ParsePath(hitObject.filename, true) : null) == strippedPath)))
                return true;

            if (HitSoundFiles.Any(hsPath => PathStatic.ParsePath(hsPath) == parsedPath))
                return true;

            if (SkinStatic.IsUsed(fileName, this))
                return true;

            if (fileName == GetOsbFileName().ToLower() && Osb.IsUsed())
                return true;

            foreach (var beatmap in Beatmaps)
                if (IsAnimationPathUsed(parsedPath, beatmap.Animations))
                    return true;

            if (Osb != null && IsAnimationPathUsed(parsedPath, Osb.animations))
                return true;

            return false;
        }

        /// <summary> Returns whether the given path (case insensitive) is used by any of the given animations. </summary>
        private bool IsAnimationPathUsed(string filePath, List<Animation> animations)
        {
            foreach (var animation in animations)
                foreach (var framePath in animation.framePaths)
                    if (framePath.ToLower() == filePath.ToLower())
                        return true;

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