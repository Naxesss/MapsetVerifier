using System.Globalization;
using MapsetVerifier.Snapshots.Diffing;

namespace MapsetVerifier.Snapshots.Compare
{
    /// <summary>
    /// Compares everything in an .osu that is not an object or a timing line: general settings,
    /// editor, metadata, difficulty, colours and events. Values stay as written; set-like values
    /// (tags, bookmarks) report what was added and removed.
    /// </summary>
    public static class SettingsDiffer
    {
        public const string General = "General";
        public const string Editor = "Editor";
        public const string Metadata = "Metadata";
        public const string Difficulty = "Difficulty";
        public const string Colours = "Colours";
        public const string Events = "Events";

        private static readonly string[] KeyedSections =
        [
            General,
            Editor,
            Metadata,
            Difficulty,
            Colours,
        ];

        /// <summary> Ids appear on submission and editor view state is not a change to the map. </summary>
        private static readonly HashSet<string> Ignored =
        [
            "BeatmapID",
            "BeatmapSetID",
            // The editor's own view state changes with every session and says nothing about the map.
            "DistanceSpacing",
            "BeatDivisor",
            "GridSize",
            "TimelineZoom",
        ];

        public static List<SettingChange> Diff(string before, string after)
        {
            var oldSections = Parse(before);
            var newSections = Parse(after);
            var changes = new List<SettingChange>();

            foreach (var section in KeyedSections)
                changes.AddRange(
                    DiffKeyed(
                        section,
                        Keyed(oldSections.GetValueOrDefault(section)),
                        Keyed(newSections.GetValueOrDefault(section))
                    )
                );

            changes.AddRange(
                DiffEvents(
                    oldSections.GetValueOrDefault(Events) ?? [],
                    newSections.GetValueOrDefault(Events) ?? []
                )
            );

            return changes;
        }

        private static Dictionary<string, List<string>> Parse(string code)
        {
            var sections = new Dictionary<string, List<string>>();
            List<string>? current = null;

            foreach (var raw in code.Replace("\r", "").Split('\n'))
            {
                var line = raw.TrimEnd();

                if (line.StartsWith('[') && line.EndsWith(']'))
                {
                    current = new List<string>();
                    sections[line[1..^1]] = current;
                }
                else if (current != null && line.Length > 0 && !line.StartsWith("//"))
                {
                    current.Add(line);
                }
            }

            return sections;
        }

        private static Dictionary<string, string> Keyed(List<string>? lines)
        {
            var result = new Dictionary<string, string>();

            foreach (var line in lines ?? [])
            {
                var split = line.IndexOf(':');

                if (split < 0)
                    continue;

                result[line[..split].Trim()] = line[(split + 1)..].Trim();
            }

            return result;
        }

        private static IEnumerable<SettingChange> DiffKeyed(
            string section,
            Dictionary<string, string> before,
            Dictionary<string, string> after
        )
        {
            foreach (var key in before.Keys.Union(after.Keys))
            {
                if (Ignored.Contains(key))
                    continue;

                var had = before.TryGetValue(key, out var oldValue);
                var has = after.TryGetValue(key, out var newValue);

                if (had && has && oldValue == newValue)
                    continue;

                if (had && has && key is "Tags" or "Bookmarks")
                {
                    var (added, removed) = SetDiff(key, oldValue!, newValue!);

                    yield return new SettingChange(
                        section,
                        key,
                        ChangeOp.Changed,
                        oldValue,
                        newValue,
                        added,
                        removed
                    );
                }
                else if (had && has)
                {
                    yield return new SettingChange(
                        section,
                        key,
                        ChangeOp.Changed,
                        oldValue,
                        newValue
                    );
                }
                else if (has)
                {
                    yield return new SettingChange(section, key, ChangeOp.Added, After: newValue);
                }
                else
                {
                    yield return new SettingChange(
                        section,
                        key,
                        ChangeOp.Removed,
                        Before: oldValue
                    );
                }
            }
        }

        private static (List<string> Added, List<string> Removed) SetDiff(
            string key,
            string before,
            string after
        )
        {
            var separator = key == "Tags" ? ' ' : ',';
            string[] Split(string value) =>
                value.Split(
                    separator,
                    StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries
                );

            var oldItems = Split(before);
            var newItems = Split(after);

            return (newItems.Except(oldItems).ToList(), oldItems.Except(newItems).ToList());
        }

        // ---------- Events ----------

        private static IEnumerable<SettingChange> DiffEvents(
            List<string> before,
            List<string> after
        )
        {
            // Storyboard commands are indented lines under their sprite; the sprite line counts.
            bool IsTop(string line) => line.Length > 0 && line[0] != ' ' && line[0] != '_';

            var oldLines = before.Where(IsTop).ToList();
            var newLines = after.Where(IsTop).ToList();
            var removed = oldLines.Except(newLines).ToList();
            var added = newLines.Except(oldLines).ToList();

            string Type(string line) => line.Split(',')[0];

            // Backgrounds and videos: one removed plus one added is a replacement.
            foreach (var (id, name) in new[] { ("0", "Background"), ("1", "Video") })
            {
                var oldItems = removed.Where(l => Type(l) == id).ToList();
                var newItems = added.Where(l => Type(l) == id).ToList();

                if (oldItems.Count == 1 && newItems.Count == 1)
                {
                    // The same file with a different line: it is the position or start time that
                    // changed, not the file, so say that instead of showing the same name twice.
                    if (File(oldItems[0]) == File(newItems[0]))
                    {
                        var oldParts = oldItems[0].Split(',').Select(p => p.Trim()).ToArray();
                        var newParts = newItems[0].Split(',').Select(p => p.Trim()).ToArray();
                        string Part(string[] parts, int i) => i < parts.Length ? parts[i] : "0";
                        string Offset(string[] parts) => Part(parts, 3) + ", " + Part(parts, 4);

                        if (Offset(oldParts) != Offset(newParts))
                            yield return new SettingChange(
                                Events,
                                name + " position",
                                ChangeOp.Changed,
                                Offset(oldParts),
                                Offset(newParts)
                            );

                        if (Part(oldParts, 1) != Part(newParts, 1))
                            yield return new SettingChange(
                                Events,
                                name + " start time",
                                ChangeOp.Changed,
                                Part(oldParts, 1) + " ms",
                                Part(newParts, 1) + " ms"
                            );

                        if (
                            Offset(oldParts) == Offset(newParts)
                            && Part(oldParts, 1) == Part(newParts, 1)
                        )
                            yield return new SettingChange(
                                Events,
                                name,
                                ChangeOp.Changed,
                                oldItems[0],
                                newItems[0]
                            );
                    }
                    else
                        yield return new SettingChange(
                            Events,
                            name,
                            ChangeOp.Changed,
                            File(oldItems[0]),
                            File(newItems[0])
                        );
                }
                else
                {
                    foreach (var line in oldItems)
                        yield return new SettingChange(
                            Events,
                            name,
                            ChangeOp.Removed,
                            Before: File(line)
                        );

                    foreach (var line in newItems)
                        yield return new SettingChange(
                            Events,
                            name,
                            ChangeOp.Added,
                            After: File(line)
                        );
                }
            }

            // Breaks pair up by a shared start or end, like moving one edge of a break.
            var oldBreaks = removed
                .Where(l => Type(l) is "2" or "Break")
                .Select(BreakRange)
                .ToList();
            var newBreaks = added.Where(l => Type(l) is "2" or "Break").Select(BreakRange).ToList();

            foreach (var newBreak in newBreaks.ToList())
            {
                var match = oldBreaks.FirstOrDefault(o =>
                    o.Start == newBreak.Start || o.End == newBreak.End
                );

                if (match == default)
                    continue;

                oldBreaks.Remove(match);
                newBreaks.Remove(newBreak);

                yield return new SettingChange(
                    Events,
                    "Break",
                    ChangeOp.Changed,
                    Range(match),
                    Range(newBreak)
                );
            }

            foreach (var item in oldBreaks)
                yield return new SettingChange(
                    Events,
                    "Break",
                    ChangeOp.Removed,
                    Before: Range(item)
                );

            foreach (var item in newBreaks)
                yield return new SettingChange(Events, "Break", ChangeOp.Added, After: Range(item));

            // Storyboard objects are summarised; the lines themselves are not meaningful to read.
            var oldStoryboard = removed.Count(l => Type(l) is not ("0" or "1" or "2" or "Break"));
            var newStoryboard = added.Count(l => Type(l) is not ("0" or "1" or "2" or "Break"));

            if (oldStoryboard + newStoryboard > 0)
                yield return new SettingChange(
                    Events,
                    "Storyboard",
                    ChangeOp.Changed,
                    Added: newStoryboard > 0
                        ? [newStoryboard + " sprites, samples or animations added"]
                        : null,
                    Removed: oldStoryboard > 0 ? [oldStoryboard + " removed"] : null
                );
        }

        private static string File(string line)
        {
            var parts = line.Split(',');

            return parts.Length > 2 ? parts[2].Trim('"') : line;
        }

        private static (double Start, double End) BreakRange(string line)
        {
            var parts = line.Split(',');

            return (
                double.Parse(parts[1], CultureInfo.InvariantCulture),
                double.Parse(parts[2], CultureInfo.InvariantCulture)
            );
        }

        private static string Range((double Start, double End) range) =>
            range.Start.ToString(CultureInfo.InvariantCulture)
            + "-"
            + range.End.ToString(CultureInfo.InvariantCulture);
    }
}
