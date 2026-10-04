using System.Collections.Concurrent;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using MapsetVerifier.Parser.Objects;
using Serilog;

namespace MapsetVerifier.Snapshots.Store
{
    /// <summary>
    /// One append-only log per mapset plus a folder of immutable, gzipped file contents keyed by
    /// hash. Identical content is stored once, so undoing an edit costs nothing.
    /// <code>
    /// snapshots/index.json
    /// snapshots/set-1004293/log.json
    /// snapshots/set-1004293/blobs/3fa9c1e0….gz
    /// snapshots/local-7c1e9a52/…            (an unsubmitted mapset)
    /// </code>
    /// </summary>
    public static class SnapshotStore
    {
        private const string DirectoryName = "snapshots";
        private const string IndexFile = "index.json";
        private const string LogFile = "log.json";
        private const string BlobsFolder = "blobs";
        private const string IdFormat = "yyyyMMddHHmmssfff";

        private static readonly object Gate = new();
        private static readonly JsonSerializerOptions Json = new() { WriteIndented = true };

        private static readonly ConcurrentDictionary<
            string,
            (long Size, DateTime Write, string Hash)
        > HashCache = new();

        private static string? root;
        private static bool ready;

        public static string Root =>
            root ?? throw new InvalidOperationException("Snapshot directory not set up");

        public static void ConfigurePath(string appDataPath, string externalFolderName)
        {
            lock (Gate)
            {
                root = Path.Combine(appDataPath, externalFolderName, DirectoryName);
                ready = false;
            }

            Log.Information("Snapshot directory: {Path}", root);

            // Migrating a big old history takes a while, so it starts now rather than on first use.
            Task.Run(() =>
            {
                try
                {
                    EnsureReady();
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Preparing the snapshot store failed");
                }
            });
        }

        /// <summary> Points the store at another folder (used while migrating); returns the old one. </summary>
        internal static string SwapRoot(string newRoot)
        {
            var old = Root;
            root = newRoot;

            return old;
        }

        /// <summary> Runs the one-time migration of the old layout; every public call waits for it. </summary>
        private static void EnsureReady()
        {
            if (ready)
                return;

            lock (Gate)
            {
                if (ready)
                    return;

                try
                {
                    LegacyMigrator.MigrateIfNeeded(Root);
                    LegacyMigrator.RepairIfNeeded(Root);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Migrating the old snapshots failed; they stay where they are.");
                }

                Directory.CreateDirectory(Root);

                if (!File.Exists(Path.Combine(Root, IndexFile)))
                    WriteIndex(new StoreIndex { Migration = StoreIndex.CurrentMigration });

                ready = true;
            }
        }

        // ---------- Keys ----------

        public static string SetKeyForId(ulong beatmapSetId) => "set-" + beatmapSetId;

        public static string DifficultyKey(ulong? beatmapId, string version) =>
            beatmapId is > 0 ? "b-" + beatmapId : "u-" + Sanitize(version);

        private static string Sanitize(string value)
        {
            var invalid = Path.GetInvalidFileNameChars();
            var builder = new StringBuilder(value.Length);

            foreach (var c in value)
                builder.Append(invalid.Contains(c) ? '_' : c);

            return builder.Length == 0 ? "unnamed" : builder.ToString();
        }

        /// <summary> The key a mapset is stored under: its set id, or a local key tied to its folder. </summary>
        public static string ResolveSetKey(BeatmapSet set)
        {
            EnsureReady();

            var setId = set
                .Beatmaps.Select(b => b.MetadataSettings.beatmapSetId)
                .FirstOrDefault(i => i is > 0);

            if (setId != null)
                return SetKeyForId(setId.Value);

            lock (Gate)
            {
                var index = ReadIndex();
                var folder = NormalizeFolder(set.SongPath);

                if (index.Folders.TryGetValue(folder, out var existing))
                    return existing;

                var key = "local-" + Guid.NewGuid().ToString("N")[..8];
                index.Folders[folder] = key;
                WriteIndex(index);

                return key;
            }
        }

        private static string NormalizeFolder(string path) =>
            Path.GetFullPath(path).TrimEnd('\\', '/').ToLowerInvariant();

        // ---------- Reading ----------

        public static SetLog Load(string setKey)
        {
            EnsureReady();

            lock (Gate)
                return ReadLog(setKey);
        }

        public static string ReadBlob(string setKey, string hash)
        {
            EnsureReady();

            using var file = File.OpenRead(BlobPath(setKey, hash));
            using var gzip = new GZipStream(file, CompressionMode.Decompress);
            using var reader = new StreamReader(gzip, Encoding.UTF8);

            return reader.ReadToEnd();
        }

        // ---------- Capturing ----------

        /// <summary>
        /// Stores the mapset's current state if anything changed since the latest snapshot.
        /// Returns the new snapshot, or null when it would have been identical. Passing a pin
        /// labels the latest snapshot instead when nothing changed.
        /// </summary>
        public static SnapshotEntry? Capture(
            BeatmapSet set,
            string trigger,
            string? pin = null,
            DateTime? now = null
        )
        {
            EnsureReady();

            var setKey = ResolveSetKey(set);
            var time = now ?? DateTime.UtcNow;
            var blobs = new Dictionary<string, string>();

            var difficulties = BuildDifficulties(set, blobs);
            var files = BuildFiles(set, blobs);

            lock (Gate)
            {
                var merged = MergeLocalHistoryIfSubmitted(set, setKey);

                var log = ReadLog(setKey);
                var last = log.Entries.LastOrDefault();
                var unchanged = last != null && IsSameState(last, difficulties, files);

                if (unchanged)
                {
                    if (pin == null)
                        return null;

                    last!.Pin = pin;
                    WriteLog(setKey, log);

                    return last;
                }

                foreach (var (hash, text) in blobs)
                    WriteBlob(setKey, hash, text);

                var entry = new SnapshotEntry
                {
                    Id = time.ToString(IdFormat),
                    Time = DateTime.SpecifyKind(time, DateTimeKind.Utc),
                    Trigger = trigger,
                    Pin = pin,
                    Difficulties = difficulties,
                    Files = files,
                };

                // The first snapshot that knows its beatmap ids is the one that was submitted.
                var knowsIds = difficulties.Any(d => d.BeatmapId != null);
                var hadIds =
                    !merged && log.Entries.Any(e => e.Difficulties.Any(d => d.BeatmapId != null));

                if (entry.Pin == null && knowsIds && !hadIds && log.Entries.Count > 0)
                    entry.Pin = "Submitted";

                // Two captures in the same millisecond cannot both be kept.
                if (last != null && string.CompareOrdinal(entry.Id, last.Id) <= 0)
                    entry.Id = (long.Parse(last.Id) + 1).ToString();

                log.Entries.Add(entry);
                WriteLog(setKey, log);

                return entry;
            }
        }

        private static bool IsSameState(
            SnapshotEntry last,
            List<StoredDifficulty> difficulties,
            Dictionary<string, StoredFile> files
        )
        {
            if (last.Difficulties.Count != difficulties.Count || last.Files.Count != files.Count)
                return false;

            foreach (var d in difficulties)
            {
                var other = last.Difficulties.FirstOrDefault(x => x.Key == d.Key);

                if (other == null || other.Blob != d.Blob)
                    return false;
            }

            foreach (var (name, file) in files)
                if (!last.Files.TryGetValue(name, out var other) || other.Hash != file.Hash)
                    return false;

            return true;
        }

        private static List<StoredDifficulty> BuildDifficulties(
            BeatmapSet set,
            Dictionary<string, string> blobs
        )
        {
            // Two files for the same beatmap id are the same difficulty; the newest one counts.
            var byKey =
                new Dictionary<
                    string,
                    (StoredDifficulty Difficulty, DateTime Created, string Code)
                >();

            foreach (var beatmap in set.Beatmaps)
            {
                var metadata = beatmap.MetadataSettings;
                var key = DifficultyKey(metadata.beatmapId, metadata.version);
                var created = File.GetCreationTimeUtc(
                    Path.Combine(beatmap.SongPath, beatmap.MapPath ?? "")
                );

                if (byKey.TryGetValue(key, out var existing) && existing.Created >= created)
                    continue;

                byKey[key] = (
                    new StoredDifficulty
                    {
                        Key = key,
                        BeatmapId = metadata.beatmapId is > 0 ? metadata.beatmapId : null,
                        Version = metadata.version,
                        Mode = beatmap.GeneralSettings.mode.ToString(),
                        File = beatmap.MapPath ?? "",
                        Stars = TryStars(beatmap),
                    },
                    created,
                    beatmap.Code
                );
            }

            var result = new List<StoredDifficulty>();

            foreach (var (difficulty, _, code) in byKey.Values)
            {
                difficulty.Blob = Hash(code);
                blobs[difficulty.Blob] = code;
                result.Add(difficulty);
            }

            return result;
        }

        private static double? TryStars(Beatmap beatmap)
        {
            try
            {
                return Math.Round(beatmap.StarRating, 2);
            }
            catch
            {
                return null;
            }
        }

        private static Dictionary<string, StoredFile> BuildFiles(
            BeatmapSet set,
            Dictionary<string, string> blobs
        )
        {
            var files = new Dictionary<string, StoredFile>();

            foreach (var path in set.SongFilePaths)
            {
                var name = path.Split('/', '\\').Last();
                var info = new FileInfo(path);

                if (!info.Exists)
                    continue;

                files[name] = new StoredFile { Hash = HashFile(info), Size = info.Length };

                // Storyboards are small text; keep them whole so they can be compared later.
                if (name.EndsWith(".osb", StringComparison.OrdinalIgnoreCase))
                {
                    var text = File.ReadAllText(path);
                    files[name].Hash = Hash(text);
                    blobs[files[name].Hash] = text;
                }
            }

            return files;
        }

        // ---------- Pins and check results ----------

        public static bool SetPin(string setKey, string id, string? pin)
        {
            EnsureReady();

            lock (Gate)
            {
                var log = ReadLog(setKey);
                var entry = log.Entries.FirstOrDefault(e => e.Id == id);

                if (entry == null)
                    return false;

                entry.Pin = string.IsNullOrWhiteSpace(pin) ? null : pin.Trim();
                WriteLog(setKey, log);

                return true;
            }
        }

        /// <summary> Stores the check results of the run that just happened with the latest snapshot. </summary>
        public static void AttachChecks(string setKey, StoredChecks checks)
        {
            EnsureReady();

            lock (Gate)
            {
                var log = ReadLog(setKey);
                var latest = log.Entries.LastOrDefault();

                if (latest == null)
                    return;

                latest.Checks = checks;
                WriteLog(setKey, log);
            }
        }

        public static void SaveSummaries(
            string setKey,
            IReadOnlyDictionary<string, StoredSummary> summaries
        )
        {
            if (summaries.Count == 0)
                return;

            EnsureReady();

            lock (Gate)
            {
                var log = ReadLog(setKey);

                foreach (var entry in log.Entries)
                    if (summaries.TryGetValue(entry.Id, out var summary))
                        entry.Summary = summary;

                WriteLog(setKey, log);
            }
        }

        // ---------- Unsubmitted to submitted ----------

        /// <summary>
        /// When a set that was stored under a local key gets its ids, its history moves into the
        /// set's own log and the difficulty keys follow the new beatmap ids.
        /// </summary>
        private static bool MergeLocalHistoryIfSubmitted(BeatmapSet set, string setKey)
        {
            if (!setKey.StartsWith("set-"))
                return false;

            var index = ReadIndex();
            var folder = NormalizeFolder(set.SongPath);

            if (!index.Folders.TryGetValue(folder, out var localKey) || localKey == setKey)
                return false;

            var localDirectory = SetDirectory(localKey);

            if (Directory.Exists(localDirectory))
            {
                var local = ReadLog(localKey);
                var target = ReadLog(setKey);
                var idsByVersion = set
                    .Beatmaps.Where(b => b.MetadataSettings.beatmapId is > 0)
                    .GroupBy(b => b.MetadataSettings.version)
                    .ToDictionary(g => g.Key, g => g.First().MetadataSettings.beatmapId);

                var blobDirectory = Path.Combine(SetDirectory(setKey), BlobsFolder);
                Directory.CreateDirectory(blobDirectory);

                var localBlobs = Path.Combine(localDirectory, BlobsFolder);

                if (Directory.Exists(localBlobs))
                    foreach (var blob in Directory.GetFiles(localBlobs, "*.gz"))
                    {
                        var destination = Path.Combine(blobDirectory, Path.GetFileName(blob));

                        if (!File.Exists(destination))
                            File.Copy(blob, destination);
                    }

                foreach (var entry in local.Entries)
                {
                    foreach (var difficulty in entry.Difficulties)
                        if (idsByVersion.TryGetValue(difficulty.Version, out var id))
                        {
                            difficulty.BeatmapId = id;
                            difficulty.Key = DifficultyKey(id, difficulty.Version);
                        }

                    entry.Summary = null;
                }

                target.Entries = target
                    .Entries.Concat(local.Entries)
                    .OrderBy(e => e.Id, StringComparer.Ordinal)
                    .ToList();
                WriteLog(setKey, target);

                Directory.Delete(localDirectory, true);
            }

            index.Folders.Remove(folder);
            WriteIndex(index);

            return true;
        }

        // ---------- Files ----------

        internal static string SetDirectory(string setKey) => Path.Combine(Root, setKey);

        private static string BlobPath(string setKey, string hash) =>
            Path.Combine(SetDirectory(setKey), BlobsFolder, hash + ".gz");

        internal static void WriteBlob(string setKey, string hash, string text)
        {
            var path = BlobPath(setKey, hash);

            if (File.Exists(path))
                return;

            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            var temp = path + ".tmp";

            using (var file = File.Create(temp))
            using (var gzip = new GZipStream(file, CompressionLevel.Optimal))
            {
                var bytes = Encoding.UTF8.GetBytes(text);
                gzip.Write(bytes, 0, bytes.Length);
            }

            File.Move(temp, path, true);
        }

        internal static SetLog ReadLog(string setKey) =>
            ReadLogFile(Path.Combine(SetDirectory(setKey), LogFile));

        internal static SetLog ReadLogFile(string path)
        {
            if (!File.Exists(path))
                return new SetLog();

            try
            {
                return JsonSerializer.Deserialize<SetLog>(File.ReadAllText(path), Json)
                    ?? new SetLog();
            }
            catch (JsonException ex)
            {
                Log.Error(ex, "Snapshot log {Path} is unreadable", path);
                return new SetLog();
            }
        }

        internal static void WriteLog(string setKey, SetLog log) =>
            WriteAtomically(
                Path.Combine(SetDirectory(setKey), LogFile),
                JsonSerializer.Serialize(log, Json)
            );

        private static StoreIndex ReadIndex() => ReadIndexFile(Path.Combine(Root, IndexFile));

        internal static StoreIndex ReadIndexFile(string path)
        {
            try
            {
                return File.Exists(path)
                    ? JsonSerializer.Deserialize<StoreIndex>(File.ReadAllText(path), Json)
                        ?? new StoreIndex()
                    : new StoreIndex();
            }
            catch (JsonException)
            {
                return new StoreIndex();
            }
        }

        internal static void WriteIndex(StoreIndex index) =>
            WriteAtomically(Path.Combine(Root, IndexFile), JsonSerializer.Serialize(index, Json));

        private static void WriteAtomically(string path, string content)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);

            // A check run and the page opening can capture at the same moment, so the log is
            // written to a temporary file first and swapped in.
            var temp = path + ".tmp";
            File.WriteAllText(temp, content);
            File.Move(temp, path, true);
        }

        // ---------- Hashing ----------

        internal static string Hash(string text) =>
            Convert.ToHexString(SHA1.HashData(Encoding.UTF8.GetBytes(text))).ToLowerInvariant();

        private static string HashFile(FileInfo info)
        {
            if (
                HashCache.TryGetValue(info.FullName, out var cached)
                && cached.Size == info.Length
                && cached.Write == info.LastWriteTimeUtc
            )
                return cached.Hash;

            using var stream = info.OpenRead();
            var hash = Convert.ToHexString(SHA1.HashData(stream)).ToLowerInvariant();
            HashCache[info.FullName] = (info.Length, info.LastWriteTimeUtc, hash);

            return hash;
        }
    }
}
