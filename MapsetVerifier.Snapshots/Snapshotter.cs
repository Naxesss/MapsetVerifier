using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using MapsetVerifier.Parser.Objects;
using MapsetVerifier.Parser.Statics;
using MapsetVerifier.Snapshots.Objects;

namespace MapsetVerifier.Snapshots
{
    public class Snapshotter
    {
        public enum DiffType
        {
            Added,
            Removed,
            Changed
        }

        private const string fileNameFormat = "yyyy-MM-dd HH-mm-ss";
        public static string RelativeDirectory { get; set; } = "";

        public static void SnapshotBeatmapSet(BeatmapSet beatmapSet)
        {
            var creationDate = DateTime.UtcNow;

            foreach (var beatmap in beatmapSet.Beatmaps)
            {
                var beatmapSetId = beatmap.MetadataSettings.beatmapSetId.ToString();
                var beatmapId = beatmap.MetadataSettings.beatmapId.ToString();
                
                // If either is null, we can't save snapshots
                if (beatmapSetId == null || beatmapId == null)
                    continue;

                foreach (var otherBeatmap in beatmapSet.Beatmaps)
                    if (otherBeatmap.MetadataSettings.beatmapId == beatmap.MetadataSettings.beatmapId && beatmap.MapPath != null && otherBeatmap.MapPath != null)
                    {
                        var date = File.GetCreationTimeUtc(beatmap.MapPath);
                        var otherDate = File.GetCreationTimeUtc(otherBeatmap.MapPath);

                        // We only save the beatmap id, so if we have two of the same beatmap
                        // in the folder, we should only save the newest one.
                        if (date < otherDate)
                            return;
                    }

                var snapshots = GetSnapshots(beatmapSetId, beatmapId).ToList();
                var shouldSave = true;

                // If our snapshot is up to date, saving is redundant.
                foreach (var snapshot in snapshots)
                    if (snapshot.creationTime == snapshots.Max(snapshot => snapshot.creationTime) && snapshot.code == beatmap.Code)
                        shouldSave = false;

                if (!shouldSave)
                    continue;

                // ./snapshots/571202/258378/2019-01-26 22-12-49
                var saveDirectory = Path.Combine(RelativeDirectory, "snapshots", beatmapSetId, beatmapId);
                var saveName = creationDate.ToString(fileNameFormat) + ".osu";

                if (!Directory.Exists(saveDirectory))
                    Directory.CreateDirectory(saveDirectory);

                File.WriteAllText(saveDirectory + Path.DirectorySeparatorChar + saveName, beatmap.Code);
            }

            SnapshotFiles(beatmapSet, creationDate);
        }

        private static void SnapshotFiles(BeatmapSet beatmapSet, DateTime creationTime)
        {
            var beatmapSetId = beatmapSet.Beatmaps?.First().MetadataSettings.beatmapSetId?.ToString();
            if (beatmapSetId == null)
                return;

            var fileSnapshot = new StringBuilder("[Files]\r\n");

            // We track even .osu and .osb files as we have no other way of determining if they've been renamed/added/removed.
            foreach (var filePath in beatmapSet.SongFilePaths)
            {
                var fileName = filePath.Split('/', '\\').Last();

                // Storing the complete file would quickly take up a lot of memory so we hash it instead.
                var bytes = File.ReadAllBytes(filePath);
                var hashBytes = SHA1.Create().ComputeHash(bytes);

                var hash = new StringBuilder();

                foreach (var hashByte in hashBytes)
                    hash.Append(hashByte.ToString("X2"));

                fileSnapshot.Append(fileName + ": " + hash + "\r\n");
            }

            var fileSnapshotString = fileSnapshot.ToString();

            if (fileSnapshotString.Length <= 0)
                return;

            var snapshots = GetSnapshots(beatmapSetId, "files").ToList();
            var shouldSave = true;

            foreach (var snapshot in snapshots)
                if (snapshot.creationTime == snapshots.Max(snapshot => snapshot.creationTime) && snapshot.code == fileSnapshotString)
                    shouldSave = false;

            if (!shouldSave)
                return;

            var filesSnapshotDirectory = Path.Combine(RelativeDirectory, "snapshots", beatmapSetId, "files");

            var filesSnapshotName = filesSnapshotDirectory + Path.DirectorySeparatorChar + creationTime.ToString(fileNameFormat) + ".txt";

            if (!Directory.Exists(filesSnapshotDirectory))
                Directory.CreateDirectory(filesSnapshotDirectory);

            File.WriteAllText(filesSnapshotName, fileSnapshotString);
        }

        public static IEnumerable<Snapshot> GetSnapshots(Beatmap beatmap) =>
            GetSnapshots(beatmap.MetadataSettings.beatmapSetId.ToString(), beatmap.MetadataSettings.beatmapId.ToString());

        public static IEnumerable<Snapshot> GetSnapshots(string? beatmapSetId, string? beatmapId)
        {
            // If either is null, we can't get snapshots
            if (beatmapSetId == null || beatmapId == null)
                yield break;
            
            var saveDirectory = Path.Combine(RelativeDirectory, "snapshots", beatmapSetId, beatmapId);

            if (!Directory.Exists(saveDirectory)) yield break;

            var filePaths = Directory.GetFiles(saveDirectory);

            foreach (var path in filePaths)
            {
                var forwardSlash = path.LastIndexOf("/", StringComparison.Ordinal);
                var backSlash = path.LastIndexOf("\\", StringComparison.Ordinal);

                var saveName = path[(Math.Max(forwardSlash, backSlash) + 1)..];
                var code = File.ReadAllText(path);

                var creationTime = DateTime.ParseExact(saveName.Split('.')[0], fileNameFormat, null);

                yield return new Snapshot(creationTime, beatmapSetId, beatmapId, saveName, code);
            }
        }

        public static IEnumerable<DiffInstance> Compare(Snapshot snapshot, string currentCode)
        {
            // Helper to get section name, stripping brackets if present
            string GetSectionName(string section) =>
                section.StartsWith("[") && section.EndsWith("]")
                    ? section.Substring(1, section.Length - 2)
                    : section;

            // Parse both snapshots into sections
            var oldSections = ParseIntoSections(snapshot.code);
            var newSections = ParseIntoSections(currentCode);

            // Create dictionaries for quick lookup by section name
            var oldSectionDict = oldSections.ToDictionary(s => s.Name, s => s);
            var newSectionDict = newSections.ToDictionary(s => s.Name, s => s);

            // Get all unique section names from both snapshots
            var allSectionNames = oldSectionDict.Keys.Union(newSectionDict.Keys).ToHashSet();

            foreach (var sectionName in allSectionNames)
            {
                var hasOldSection = oldSectionDict.TryGetValue(sectionName, out var oldSection);
                var hasNewSection = newSectionDict.TryGetValue(sectionName, out var newSection);

                if (!hasOldSection && hasNewSection)
                {
                    // Section was added - report all lines as added
                    foreach (var line in newSection!.Lines)
                    {
                        if (!string.IsNullOrWhiteSpace(line))
                            yield return new DiffInstance(line, GetSectionName(sectionName), DiffType.Added, new List<string>(), snapshot.creationTime);
                    }
                }
                else if (hasOldSection && !hasNewSection)
                {
                    // Section was removed - report all lines as removed
                    foreach (var line in oldSection!.Lines)
                    {
                        if (!string.IsNullOrWhiteSpace(line))
                            yield return new DiffInstance(line, GetSectionName(sectionName), DiffType.Removed, new List<string>(), snapshot.creationTime);
                    }
                }
                else if (hasOldSection && hasNewSection)
                {
                    // Section exists in both - compare line by line within the section
                    var oldLines = oldSection!.Lines;
                    var newLines = newSection!.Lines;

                    var maxLength = Math.Max(oldLines.Count, newLines.Count);
                    var minLength = Math.Min(oldLines.Count, newLines.Count);
                    var offset = 0;

                    for (var i = 0; i < maxLength; ++i)
                    {
                        if (i >= minLength || i + offset >= newLines.Count)
                        {
                            if (newLines.Count - oldLines.Count - offset > 0 && i + offset < newLines.Count)
                            {
                                // A line was added at the end of the section
                                var line = newLines[i + offset];
                                if (!string.IsNullOrWhiteSpace(line))
                                    yield return new DiffInstance(line, GetSectionName(sectionName), DiffType.Added, new List<string>(), snapshot.creationTime);
                            }

                            if (oldLines.Count - newLines.Count > 0 && i < oldLines.Count)
                            {
                                // A line was removed from the end of the section
                                var line = oldLines[i];
                                if (!string.IsNullOrWhiteSpace(line))
                                    yield return new DiffInstance(line, GetSectionName(sectionName), DiffType.Removed, new List<string>(), snapshot.creationTime);
                            }
                        }
                        else
                        {
                            if (oldLines[i] == newLines[i + offset])
                                continue;

                            var originalOffset = offset;

                            // Try to find the old line in the new lines (looking ahead)
                            for (; offset < minLength - i; ++offset)
                                if (oldLines[i] == newLines[i + offset])
                                    break;

                            if (offset >= minLength - i)
                            {
                                // Line was removed
                                offset = originalOffset;
                                --offset;

                                var line = oldLines[i];
                                if (!string.IsNullOrWhiteSpace(line))
                                    yield return new DiffInstance(line, GetSectionName(sectionName), DiffType.Removed, new List<string>(), snapshot.creationTime);
                            }
                            else
                            {
                                // Lines were added
                                for (var j = originalOffset; j < offset; j++)
                                {
                                    var line = newLines[i + j];
                                    if (!string.IsNullOrWhiteSpace(line))
                                        yield return new DiffInstance(line, GetSectionName(sectionName), DiffType.Added, new List<string>(), snapshot.creationTime);
                                }
                            }
                        }
                    }
                }
            }
        }

        private static List<Section> ParseIntoSections(string code)
        {
            var lines = code.Replace("\r", "").Split('\n');
            var sections = new List<Section>();
            var currentSectionName = "No Section";
            var currentSectionLines = new List<string>();

            foreach (var line in lines)
            {
                if (line.StartsWith("[") && line.EndsWith("]"))
                {
                    // Save the previous section if it has content
                    if (currentSectionLines.Count > 0 || currentSectionName != "No Section")
                    {
                        sections.Add(new Section(currentSectionName, currentSectionLines));
                    }

                    // Start a new section
                    currentSectionName = line;
                    currentSectionLines = new List<string>();
                }
                else
                {
                    // Add line to current section (including empty lines for proper comparison)
                    currentSectionLines.Add(line);
                }
            }

            // Add the last section
            if (currentSectionLines.Count > 0 || currentSectionName != "No Section")
            {
                sections.Add(new Section(currentSectionName, currentSectionLines));
            }

            return sections;
        }

        private class Section
        {
            public string Name { get; }
            public List<string> Lines { get; }

            public Section(string name, List<string> lines)
            {
                Name = name;
                Lines = lines;
            }
        }

        public static IEnumerable<DiffInstance> TranslateComparison(IEnumerable<DiffInstance> diffs)
        {
            foreach (var diffsBySection in diffs.GroupBy(diff => diff.Section).Distinct())
            {
                var translator = TranslatorRegistry.GetTranslators().FirstOrDefault(translator => translator.Section == diffsBySection.Key);

                if (translator != null)
                    foreach (var diff in translator.Translate(diffsBySection))
                    {
                        // Since all translators should be able to translate sections, we do that here.
                        diff.Section = translator.TranslatedSection;

                        yield return diff;
                    }
                else
                    foreach (var diff in diffsBySection)
                        yield return diff;
            }
        }

        private static DiffInstance GetTranslatedSettingDiff(string sectionName, Func<string, string> translateFunc, Setting setting, DiffInstance diff, Setting? otherSetting = null, DiffInstance? otherDiff = null)
        {
            var key = translateFunc(setting.key);

            if (otherSetting != null && otherDiff != null)
                return new DiffInstance($"{key} was changed from \"{otherSetting.GetValueOrDefault().value}\" to \"{setting.value}\".", sectionName, DiffType.Changed, new List<string>(), diff.SnapshotCreationDate);

            if (diff.DiffType == DiffType.Added)
                return new DiffInstance($"{key} was added and set to \"{setting.value}\".", sectionName, DiffType.Added, new List<string>(), diff.SnapshotCreationDate);

            return new DiffInstance($"{key} was removed and is no longer set to \"{setting.value}\".", sectionName, DiffType.Removed, new List<string>(), diff.SnapshotCreationDate);
        }

        public static IEnumerable<DiffInstance> TranslateSettings(string sectionName, IEnumerable<DiffInstance> diffs, Func<string, string> translateFunc)
        {
            diffs = diffs.ToArray();

            var added = diffs.Where(diff => diff.DiffType == DiffType.Added).ToList();
            var removed = diffs.Where(diff => diff.DiffType == DiffType.Removed).ToList();

            foreach (var addition in added)
            {
                var setting = new Setting(addition.Diff);
                var removal = removed.FirstOrDefault(diff => new Setting(diff.Diff).key == setting.key);

                if (removal != null && removal.Diff != null)
                {
                    var removedSetting = new Setting(removal.Diff);

                    removed.Remove(removal);

                    // Skip if the values are actually the same (can happen due to whitespace differences in the raw line)
                    if (setting.value == removedSetting.value)
                        continue;

                    switch (removedSetting.key)
                    {
                        case "Bookmarks":
                        {
                            var prevBookmarks = removedSetting.value.Split(',').Select(value => double.Parse(value.Trim())).ToArray();

                            var curBookmarks = setting.value.Split(',').Select(value => double.Parse(value.Trim())).ToArray();

                            var removedBookmarks = prevBookmarks.Except(curBookmarks).ToArray();
                            var addedBookmarks = curBookmarks.Except(prevBookmarks).ToArray();

                            var details = new List<string>();

                            if (addedBookmarks.Any())
                                details.Add($"Added {string.Join(", ", addedBookmarks.Select(mark => Timestamp.Get(mark)))}");

                            if (removedBookmarks.Any())
                                details.Add($"Removed {string.Join(", ", removedBookmarks.Select(mark => Timestamp.Get(mark)))}");

                            yield return new DiffInstance($"{translateFunc(setting.key)} were changed.", sectionName, DiffType.Changed, details, addition.SnapshotCreationDate);

                            break;
                        }

                        case "Tags":
                        {
                            var prevTags = removedSetting.value.Split(' ').Select(value => $"\"{value}\"").ToArray();
                            var curTags = setting.value.Split(' ').Select(value => $"\"{value}\"").ToArray();

                            var removedTags = prevTags.Except(curTags).ToArray();
                            var addedTags = curTags.Except(prevTags).ToArray();

                            var details = new List<string>();

                            if (addedTags.Any())
                                details.Add($"Added {string.Join(", ", addedTags)}");

                            if (removedTags.Any())
                                details.Add($"Removed {string.Join(", ", removedTags)}");

                            yield return new DiffInstance($"{translateFunc(setting.key)} were changed.", sectionName, DiffType.Changed, details, addition.SnapshotCreationDate);

                            break;
                        }

                        default:
                            yield return GetTranslatedSettingDiff(sectionName, translateFunc, setting, addition, removedSetting, removal);

                            break;
                    }
                }
                else
                {
                    yield return GetTranslatedSettingDiff(sectionName, translateFunc, setting, addition);
                }
            }

            foreach (var removal in removed)
            {
                var setting = new Setting(removal.Diff);

                yield return GetTranslatedSettingDiff(sectionName, translateFunc, setting, removal);
            }
        }

        public struct Snapshot
        {
            public readonly DateTime creationTime;
            public readonly string beatmapSetId;
            public readonly string beatmapId;
            public readonly string saveName;
            public readonly string code;

            public Snapshot(DateTime creationTime, string beatmapSetId, string beatmapId, string saveName, string code)
            {
                this.creationTime = creationTime;
                this.beatmapSetId = beatmapSetId;
                this.beatmapId = beatmapId;
                this.saveName = saveName;
                this.code = code;
            }
        }

        public struct Setting
        {
            public readonly string key;
            public readonly string value;

            public Setting(string code)
            {
                if (code.IndexOf(":", StringComparison.Ordinal) != -1)
                {
                    key = code[..code.IndexOf(":", StringComparison.Ordinal)].Trim();
                    value = code[(code.IndexOf(":", StringComparison.Ordinal) + ":".Length)..].Trim();
                }
                else
                {
                    key = "A line";
                    value = code;
                }
            }
        }
    }
}