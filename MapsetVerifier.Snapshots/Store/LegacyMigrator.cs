using System.Globalization;
using Serilog;

namespace MapsetVerifier.Snapshots.Store
{
    /// <summary>
    /// Turns the old layout (one timestamped .osu per difficulty snapshot plus a hash listing for
    /// files) into the log-based store, a set at a time as it gets opened. The old folder is moved
    /// to "snapshots-legacy", never deleted.
    /// </summary>
    internal static class LegacyMigrator
    {
        private const string TimeFormat = "yyyy-MM-dd HH-mm-ss";

        private static readonly string[] Modes = ["Standard", "Taiko", "Catch", "Mania"];

        private sealed record LegacySnapshot(DateTime Time, string Path);

        public static bool HasOldSnapshots(string root) =>
            Directory.Exists(root)
            && Directory.EnumerateDirectories(root).Any(d => IsOldSetFolder(Path.GetFileName(d)));

        private static bool IsOldSetFolder(string name) => ulong.TryParse(name, out _);

        /// <summary>
        /// Converts the old snapshots of one set, if it has any. Everything in the old folder is
        /// only read until the log is written, so an interrupted conversion is simply done again.
        /// </summary>
        public static void ConvertSet(string root, string setKey)
        {
            const string prefix = "set-";

            if (!setKey.StartsWith(prefix, StringComparison.Ordinal))
                return;

            var folderName = setKey[prefix.Length..];

            // Set keys also come in through requests, so only ever a plain number becomes a path.
            if (!IsOldSetFolder(folderName))
                return;

            var directory = Path.Combine(root, folderName);

            if (!Directory.Exists(directory))
                return;

            Log.Information("Converting the old snapshots of {SetKey}", setKey);

            var imported = BuildLog(directory, setKey);

            // What's already there may have been captured since, or come from an interrupted
            // conversion, in which case it may have pins and summaries worth keeping over a new copy.
            var existing = SnapshotStore.ReadLog(setKey);
            var known = existing.Entries.Select(e => e.Id).ToHashSet();
            var merged = existing
                .Entries.Concat(imported.Entries.Where(e => !known.Contains(e.Id)))
                .OrderBy(e => e.Id, StringComparer.Ordinal)
                .ToList();

            if (merged.Count > 0)
                SnapshotStore.WriteLog(setKey, new SetLog { Entries = merged });

            var legacy = root + "-legacy";
            Directory.CreateDirectory(legacy);

            var destination = Path.Combine(legacy, folderName);

            if (Directory.Exists(destination))
                destination += "-" + DateTime.UtcNow.ToString("yyyyMMddHHmmss");

            Directory.Move(directory, destination);
        }

        /// <summary>
        /// Clears what a migration of the whole store left behind if it never finished, which the
        /// version doing that started over from on every launch. Only holds copies, so it's safe to
        /// remove, and it can be large, so that doesn't hold up anything else.
        /// </summary>
        public static void RemoveInterruptedMigration(string root)
        {
            var staging = root + "-migrating";

            if (!Directory.Exists(staging))
                return;

            Task.Run(() =>
            {
                try
                {
                    Directory.Delete(staging, true);
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "Removing the unfinished migration {Path} failed", staging);
                }
            });
        }

        private static void MigrateSet(string directory)
        {
            var setKey = "set-" + Path.GetFileName(directory);
            var log = BuildLog(directory, setKey);

            if (log.Entries.Count > 0)
                SnapshotStore.WriteLog(setKey, log);
        }

        private static SetLog BuildLog(string directory, string setKey)
        {
            var difficulties = new Dictionary<string, List<LegacySnapshot>>();
            var files = new List<LegacySnapshot>();

            foreach (var sub in Directory.GetDirectories(directory))
            {
                var name = Path.GetFileName(sub);
                var isFiles = name == "files";
                var snapshots = Directory
                    .GetFiles(sub, isFiles ? "*.txt" : "*.osu")
                    .Select(ParseSnapshot)
                    .OfType<LegacySnapshot>()
                    .OrderBy(s => s.Time)
                    .ToList();

                if (isFiles)
                    files = snapshots;
                else if (snapshots.Count > 0)
                    difficulties[name] = snapshots;
            }

            var times = difficulties
                .Values.SelectMany(l => l)
                .Concat(files)
                .Select(s => s.Time)
                .Distinct()
                .OrderBy(t => t)
                .ToList();

            var log = new SetLog();
            SnapshotEntry? previous = null;
            var cache = new Dictionary<string, (string Hash, string Version, int Mode)>();

            foreach (var time in times)
            {
                var entry = new SnapshotEntry
                {
                    Id = time.ToString("yyyyMMddHHmmssfff"),
                    Time = time,
                    Trigger = "import",
                };

                var latestFiles = files.LastOrDefault(s => s.Time <= time);
                var listing = latestFiles == null ? [] : ReadListing(latestFiles.Path);
                var candidates =
                    new List<(
                        string Folder,
                        LegacySnapshot Latest,
                        (string Hash, string Version, int Mode) Info
                    )>();

                foreach (var (folder, snapshots) in difficulties)
                {
                    var latest = snapshots.LastOrDefault(s => s.Time <= time);

                    if (latest == null)
                        continue;

                    // The same snapshot carries over to many timeline steps; read it once.
                    if (!cache.TryGetValue(latest.Path, out var info))
                    {
                        var code = File.ReadAllText(latest.Path);
                        info = (
                            SnapshotStore.Hash(code),
                            HeaderValue(code, "Version:"),
                            int.TryParse(HeaderValue(code, "Mode:"), out var parsedMode)
                                ? parsedMode
                                : 0
                        );
                        cache[latest.Path] = info;
                        SnapshotStore.WriteBlob(setKey, info.Hash, code);
                    }

                    candidates.Add((folder, latest, info));
                }

                // The old layout never recorded a difficulty being deleted or re-uploaded under a
                // new id, so its last snapshot would live on forever. The files listing says which
                // difficulties existed at the time; when it names none of them, trust everything.
                var alive = candidates
                    .Where(c => listing.Any(n => IsFileOf(n, c.Info.Version)))
                    .ToList();

                if (alive.Count == 0)
                    alive = candidates;

                // Two folders for one difficulty name (re-uploaded under a new id): the newest wins.
                foreach (
                    var group in alive.GroupBy(
                        c => c.Info.Version,
                        StringComparer.OrdinalIgnoreCase
                    )
                )
                {
                    var (folder, _, info) = group.OrderByDescending(c => c.Latest.Time).First();
                    var isId = ulong.TryParse(folder, out var beatmapId);

                    entry.Difficulties.Add(
                        new StoredDifficulty
                        {
                            Key = isId
                                ? "b-" + beatmapId
                                : "u-" + folder.Replace("unsubmitted-", ""),
                            BeatmapId = isId ? beatmapId : null,
                            Version = info.Version.Length > 0 ? info.Version : folder,
                            Mode = Modes[Math.Clamp(info.Mode, 0, 3)],
                            Blob = info.Hash,
                        }
                    );
                }

                if (latestFiles != null)
                    foreach (var line in File.ReadAllLines(latestFiles.Path).Skip(1))
                    {
                        var split = line.LastIndexOf(": ", StringComparison.Ordinal);

                        if (split > 0)
                            entry.Files[line[..split]] = new StoredFile
                            {
                                Hash = line[(split + 2)..].Trim().ToLowerInvariant(),
                            };
                    }

                if (entry.Difficulties.Count == 0 && entry.Files.Count == 0)
                    continue;

                if (previous != null && SameState(previous, entry))
                    continue;

                log.Entries.Add(entry);
                previous = entry;
            }

            return log;
        }

        private static List<string> ReadListing(string path) =>
            File.ReadAllLines(path)
                .Skip(1)
                .Select(line =>
                {
                    var split = line.LastIndexOf(": ", StringComparison.Ordinal);
                    return split > 0 ? line[..split] : "";
                })
                .Where(name => name.EndsWith(".osu", StringComparison.OrdinalIgnoreCase))
                .ToList();

        /// <summary> Difficulty files are named "Artist - Title (Mapper) [Version].osu". </summary>
        private static bool IsFileOf(string fileName, string version) =>
            version.Length > 0
            && fileName.EndsWith("[" + version + "].osu", StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// Rebuilds stores that were migrated before deleted difficulties were told apart (they
        /// showed long-gone difficulties as "removed"). The history comes from the kept old folder
        /// again; every snapshot taken since the first migration is carried over untouched.
        /// </summary>
        public static void RepairIfNeeded(string root)
        {
            var indexPath = Path.Combine(root, "index.json");
            var legacy = root + "-legacy";

            if (!File.Exists(indexPath))
                return;

            var index = SnapshotStore.ReadIndexFile(indexPath);

            if (index.Migration >= StoreIndex.CurrentMigration)
                return;

            if (!Directory.Exists(legacy))
            {
                index.Migration = StoreIndex.CurrentMigration;
                SnapshotStore.WriteIndex(index);
                return;
            }

            Log.Information("Rebuilding the migrated snapshots from {Legacy}", legacy);

            var staging = root + "-repairing";

            if (Directory.Exists(staging))
                Directory.Delete(staging, true);

            Directory.CreateDirectory(staging);

            var previousRoot = SnapshotStore.SwapRoot(staging);

            try
            {
                foreach (
                    var directory in Directory
                        .GetDirectories(legacy)
                        .Where(d => ulong.TryParse(Path.GetFileName(d), out _))
                )
                    MigrateSet(directory);

                foreach (var current in Directory.GetDirectories(root))
                    CarryOver(current, Path.Combine(staging, Path.GetFileName(current)));

                index.Migration = StoreIndex.CurrentMigration;
                SnapshotStore.WriteIndex(index);
            }
            finally
            {
                SnapshotStore.SwapRoot(previousRoot);
            }

            var superseded = root + "-superseded";

            if (Directory.Exists(superseded))
                superseded += "-" + DateTime.UtcNow.ToString("yyyyMMddHHmmss");

            Directory.Move(root, superseded);
            Directory.Move(staging, root);

            Log.Information("Snapshots rebuilt; the previous store was kept as {Path}", superseded);
        }

        /// <summary> Adds what was captured after the first migration to the rebuilt history. </summary>
        private static void CarryOver(string currentDirectory, string rebuiltDirectory)
        {
            var oldLog = SnapshotStore.ReadLogFile(Path.Combine(currentDirectory, "log.json"));
            var rebuilt = SnapshotStore.ReadLogFile(Path.Combine(rebuiltDirectory, "log.json"));
            var captured = oldLog.Entries.Where(e => e.Trigger != "import").ToList();

            // Milestones named on imported snapshots stay on the same moment.
            foreach (
                var pinned in oldLog.Entries.Where(e => e.Trigger == "import" && e.Pin != null)
            )
            {
                var match = rebuilt.Entries.FirstOrDefault(e => e.Id == pinned.Id);

                if (match != null)
                    match.Pin = pinned.Pin;
            }

            var oldBlobs = Path.Combine(currentDirectory, "blobs");

            if (Directory.Exists(oldBlobs))
            {
                var newBlobs = Path.Combine(rebuiltDirectory, "blobs");
                Directory.CreateDirectory(newBlobs);

                foreach (var blob in Directory.GetFiles(oldBlobs, "*.gz"))
                {
                    var destination = Path.Combine(newBlobs, Path.GetFileName(blob));

                    if (!File.Exists(destination))
                        File.Copy(blob, destination);
                }
            }

            foreach (var entry in captured)
                entry.Summary = null;

            rebuilt.Entries = rebuilt
                .Entries.Concat(captured)
                .OrderBy(e => e.Id, StringComparer.Ordinal)
                .ToList();

            if (rebuilt.Entries.Count > 0)
                SnapshotStore.WriteLog(Path.GetFileName(rebuiltDirectory), rebuilt);
        }

        /// <summary> The value of a "Key:" line, found without scanning the (huge) object list. </summary>
        private static string HeaderValue(string code, string key)
        {
            var start = code.IndexOf("\n" + key, StringComparison.Ordinal);

            if (start < 0)
                return "";

            start += key.Length + 1;
            var end = code.IndexOf('\n', start);

            return code[start..(end < 0 ? code.Length : end)].Trim();
        }

        private static bool SameState(SnapshotEntry a, SnapshotEntry b) =>
            a.Difficulties.Count == b.Difficulties.Count
            && a.Files.Count == b.Files.Count
            && a.Difficulties.All(d => b.Difficulties.Any(o => o.Key == d.Key && o.Blob == d.Blob))
            && a.Files.All(f => b.Files.TryGetValue(f.Key, out var o) && o.Hash == f.Value.Hash);

        private static LegacySnapshot? ParseSnapshot(string path)
        {
            var name = Path.GetFileNameWithoutExtension(path);

            return DateTime.TryParseExact(
                name,
                TimeFormat,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var time
            )
                ? new LegacySnapshot(DateTime.SpecifyKind(time, DateTimeKind.Utc), path)
                : null;
        }
    }
}
