using System.Collections.Concurrent;
using MapsetVerifier.Parser.Objects;
using MapsetVerifier.Parser.Objects.HitObjects;
using MapsetVerifier.Parser.Objects.HitObjects.Catch;
using MapsetVerifier.Parser.Objects.HitObjects.Mania;
using MapsetVerifier.Parser.Objects.TimingLines;
using MapsetVerifier.Snapshots.Diffing;
using MapsetVerifier.Snapshots.Store;

namespace MapsetVerifier.Snapshots.Compare
{
    /// <summary>
    /// Compares two snapshots of a mapset: every difficulty (parsed and diffed in parallel),
    /// the files, and what changed the same way in every difficulty, which is reported once.
    /// </summary>
    public static class SnapshotComparer
    {
        /// <summary>
        /// A window is always this many beats of the song, so every picture has the same zoom and
        /// size however many objects happen to be in it. Four beats is a measure in 4/4.
        /// </summary>
        public const int WindowBeats = 4;

        /// <summary> Scrolling moves the window this many beats per step. </summary>
        private const double ScrollBeats = 0.5;

        /// <summary> A short change sits with this many beats of the song before it. </summary>
        private const double LeadBeats = 1;

        /// <summary> A safety net for absurdly dense windows. </summary>
        private const int MaxWindowObjects = 400;

        private const int PathSamples = 24;

        /// <summary> Marks closer together than this on the change map merge into one. </summary>
        private const double MarkGapMs = 250;

        private static readonly ConcurrentDictionary<string, Parsed> ParseCache = new();
        private static readonly ConcurrentDictionary<string, Partial> CompareCache = new();

        private sealed record Parsed(Beatmap Beatmap, string Code);

        private sealed class Partial
        {
            public required StoredDifficulty Identity { get; init; }
            public DifficultyStatus Status { get; init; }
            public double? StarsBefore { get; init; }
            public double? StarsAfter { get; init; }
            public int ObjectsBefore { get; init; }
            public int ObjectsAfter { get; init; }
            public required List<Change> Changes { get; init; }
            public required List<SettingChange> Settings { get; set; }
            public required List<Rollup> Rollups { get; set; }
            public required List<HunkResult> Hunks { get; init; }
            public required List<Mark> Marks { get; init; }
            public required List<TimeRange> Kiai { get; init; }
            public required List<TimeRange> Breaks { get; init; }
            public double LengthMs { get; init; }
        }

        public static ComparisonResult Compare(
            string setKey,
            SnapshotEntry before,
            SnapshotEntry after
        )
        {
            var keys = before
                .Difficulties.Select(d => d.Key)
                .Union(after.Difficulties.Select(d => d.Key))
                .ToList();

            var partials = keys.AsParallel()
                .AsOrdered()
                .Select(key =>
                    ComparePartial(
                        setKey,
                        before.Difficulties.FirstOrDefault(d => d.Key == key),
                        after.Difficulties.FirstOrDefault(d => d.Key == key)
                    )
                )
                .Select(p => new Partial
                {
                    // Cached partials are shared, so each comparison gets its own copy to edit.
                    Identity = p.Identity,
                    Status = p.Status,
                    StarsBefore = p.StarsBefore,
                    StarsAfter = p.StarsAfter,
                    ObjectsBefore = p.ObjectsBefore,
                    ObjectsAfter = p.ObjectsAfter,
                    Changes = p.Changes,
                    Settings = p.Settings.ToList(),
                    Rollups = p.Rollups.ToList(),
                    Hunks = p.Hunks,
                    Marks = p.Marks,
                    Kiai = p.Kiai,
                    Breaks = p.Breaks,
                    LengthMs = p.LengthMs,
                })
                .ToList();

            var general = BuildGeneral(before, after, partials);

            return new ComparisonResult(
                new SnapshotInfo(before.Id, before.Time, before.Trigger, before.Pin),
                new SnapshotInfo(after.Id, after.Time, after.Trigger, after.Pin),
                general,
                partials.Select(Finish).ToList()
            );
        }

        // ---------- General and set-wide changes ----------

        private static GeneralComparison BuildGeneral(
            SnapshotEntry before,
            SnapshotEntry after,
            List<Partial> partials
        )
        {
            var changed = partials.Where(p => p.Status == DifficultyStatus.Changed).ToList();
            var rollups = new List<Rollup>();
            var settings = new List<SettingChange>();

            if (changed.Count >= 2)
            {
                // The same time shift in every changed difficulty is one change to the whole set.
                var shifts = changed
                    .Select(p => p.Rollups.FirstOrDefault(r => r.Kind == RollupKind.Offset))
                    .ToList();

                if (
                    shifts.All(s => s != null)
                    && shifts.Select(s => s!.Amount).Distinct().Count() == 1
                )
                {
                    rollups.Add(shifts[0]! with { Absorbed = shifts.Sum(s => s!.Absorbed) });

                    foreach (var p in changed)
                        p.Rollups = p.Rollups.Where(r => r.Kind != RollupKind.Offset).ToList();
                }

                // So is a setting changed identically everywhere; AR, OD and the like stay per difficulty.
                var groups = changed
                    .SelectMany(p => p.Settings.Select(s => (Partial: p, Setting: s)))
                    .Where(x => x.Setting.Section != SettingsDiffer.Difficulty)
                    .GroupBy(x => SettingKey(x.Setting));

                foreach (var group in groups)
                {
                    if (group.Select(x => x.Partial).Distinct().Count() != changed.Count)
                        continue;

                    settings.Add(group.First().Setting with { AppliesTo = changed.Count });

                    foreach (var p in changed)
                        p.Settings = p.Settings.Where(s => SettingKey(s) != group.Key).ToList();
                }
            }

            var files = FileDiffer.Diff(before.Files, after.Files);
            var counts = new ChangeCounts(
                files.Count(f => f.Op == ChangeOp.Added)
                    + settings.Count(s => s.Op == ChangeOp.Added),
                files.Count(f => f.Op == ChangeOp.Removed)
                    + settings.Count(s => s.Op == ChangeOp.Removed),
                files.Count(f => f.Op == ChangeOp.Changed)
                    + settings.Count(s => s.Op == ChangeOp.Changed)
                    + rollups.Count
            );

            return new GeneralComparison(rollups, settings, files, counts);
        }

        private static string SettingKey(SettingChange s) =>
            string.Join(
                "|",
                s.Section,
                s.Key,
                s.Op,
                s.After,
                string.Join(",", s.Added ?? []),
                string.Join(",", s.Removed ?? [])
            );

        private static DifficultyComparison Finish(Partial p)
        {
            var minor = p.Changes.Where(c => c.Minor).ToList();
            var counts = Count(p.Changes, p.Settings);

            if (p.Status == DifficultyStatus.Added)
                counts = new ChangeCounts(p.ObjectsAfter, 0, 0);
            else if (p.Status == DifficultyStatus.Removed)
                counts = new ChangeCounts(0, p.ObjectsBefore, 0);

            return new DifficultyComparison(
                p.Identity.Key,
                p.Identity.Version,
                p.Identity.Mode,
                p.Identity.BeatmapId,
                p.Status,
                p.StarsBefore,
                p.StarsAfter,
                p.ObjectsBefore,
                p.ObjectsAfter,
                counts,
                CountGroups(minor),
                p.Hunks,
                minor,
                p.Settings,
                p.Rollups,
                p.Marks,
                p.Kiai,
                p.Breaks,
                p.LengthMs
            );
        }

        // ---------- One difficulty ----------

        private static Partial ComparePartial(
            string setKey,
            StoredDifficulty? before,
            StoredDifficulty? after
        )
        {
            var identity = after ?? before!;
            var cacheKey = setKey + "|" + before?.Blob + "|" + after?.Blob;

            if (CompareCache.TryGetValue(cacheKey, out var cached))
                return cached;

            var oldMap = before == null ? null : Parse(setKey, before);
            var newMap = after == null ? null : Parse(setKey, after);
            var shown = (newMap ?? oldMap)!.Beatmap;

            var partial = new Partial
            {
                Identity = identity,
                Status =
                    before == null ? DifficultyStatus.Added
                    : after == null ? DifficultyStatus.Removed
                    : before.Blob == after.Blob ? DifficultyStatus.Unchanged
                    : DifficultyStatus.Changed,
                // Older snapshots may not have their rating stored; it is worked out from the file then.
                StarsBefore = before?.Stars ?? StarsOf(oldMap?.Beatmap),
                StarsAfter = after?.Stars ?? StarsOf(newMap?.Beatmap),
                ObjectsBefore = oldMap?.Beatmap.HitObjects.Count ?? 0,
                ObjectsAfter = newMap?.Beatmap.HitObjects.Count ?? 0,
                Changes = [],
                Settings = [],
                Rollups = [],
                Hunks = [],
                Marks = [],
                Kiai = KiaiRanges(shown),
                Breaks = shown.Breaks.Select(b => new TimeRange(b.time, b.endTime)).ToList(),
                LengthMs =
                    shown.HitObjects.Count == 0 ? 0 : shown.HitObjects.Max(o => o.GetEndTime()),
            };

            if (oldMap != null && newMap != null && partial.Status == DifficultyStatus.Changed)
            {
                var result = BeatmapDiffer.Diff(oldMap.Beatmap, newMap.Beatmap);
                var hunks = HunkBuilder.Build(result.Changes);

                partial.Changes.AddRange(result.Changes);
                partial.Rollups.AddRange(result.Rollups);
                partial.Settings.AddRange(SettingsDiffer.Diff(oldMap.Code, newMap.Code));
                partial.Marks.AddRange(BuildMarks(result.Changes));
                partial.Hunks.AddRange(hunks.Select(ToResult));
            }

            CompareCache[cacheKey] = partial;

            if (CompareCache.Count > 400)
                CompareCache.Clear();

            return partial;
        }

        private static Parsed Parse(string setKey, StoredDifficulty difficulty)
        {
            var key = setKey + "|" + difficulty.Blob;

            if (ParseCache.TryGetValue(key, out var cached))
                return cached;

            var code = SnapshotStore.ReadBlob(setKey, difficulty.Blob);
            var parsed = new Parsed(new Beatmap(code, "snapshot", difficulty.File), code);

            ParseCache[key] = parsed;

            if (ParseCache.Count > 96)
                ParseCache.Clear();

            return parsed;
        }

        private static List<TimeRange> KiaiRanges(Beatmap beatmap)
        {
            var ranges = new List<TimeRange>();
            double? start = null;

            foreach (var line in beatmap.TimingLines.OrderBy(l => l.Offset))
            {
                if (line.Kiai && start == null)
                    start = line.Offset;
                else if (!line.Kiai && start != null)
                {
                    ranges.Add(new TimeRange(start.Value, line.Offset));
                    start = null;
                }
            }

            if (start != null)
                ranges.Add(
                    new TimeRange(
                        start.Value,
                        Math.Max(
                            start.Value,
                            beatmap.HitObjects.Count == 0
                                ? start.Value
                                : beatmap.HitObjects.Max(o => o.GetEndTime())
                        )
                    )
                );

            return ranges;
        }

        // ---------- Counts and marks ----------

        private static string GroupKey(Change c) =>
            c.Object != null
                ? c.Object.Stamp
                : "line|" + c.Time + "|" + c.Field?.Replace("RedLine", "").Replace("GreenLine", "");

        private static int CountGroups(IEnumerable<Change> changes) =>
            changes.GroupBy(GroupKey).Count();

        public static ChangeCounts Count(
            IEnumerable<Change> changes,
            IEnumerable<SettingChange>? settings = null
        )
        {
            int added = 0,
                removed = 0,
                changed = 0;

            foreach (var group in changes.GroupBy(GroupKey))
            {
                var items = group.ToList();

                if (items.Count == 1 && items[0].Op == ChangeOp.Added)
                    added++;
                else if (items.Count == 1 && items[0].Op == ChangeOp.Removed)
                    removed++;
                else
                    changed++;
            }

            foreach (var setting in settings ?? [])
            {
                switch (setting.Op)
                {
                    case ChangeOp.Added:
                        added++;
                        break;
                    case ChangeOp.Removed:
                        removed++;
                        break;
                    default:
                        changed++;
                        break;
                }
            }

            return new ChangeCounts(added, removed, changed);
        }

        private static IEnumerable<Mark> BuildMarks(IEnumerable<Change> changes)
        {
            foreach (var group in changes.GroupBy(c => (c.Kind, c.Minor)))
            {
                var ordered = group.OrderBy(c => c.Time).ToList();
                var start = ordered[0].Time;
                var end = ordered[0].EndTime ?? ordered[0].Time;

                foreach (var change in ordered.Skip(1))
                {
                    if (change.Time - end > MarkGapMs)
                    {
                        yield return new Mark(group.Key.Kind, start, end, group.Key.Minor);
                        start = change.Time;
                        end = change.EndTime ?? change.Time;
                    }
                    else
                    {
                        end = Math.Max(end, change.EndTime ?? change.Time);
                    }
                }

                yield return new Mark(group.Key.Kind, start, end, group.Key.Minor);
            }
        }

        // ---------- Hunks and their pictures ----------

        private static HunkResult ToResult(Hunk hunk) =>
            new(hunk.Start, hunk.End, hunk.Label, hunk.Kinds, hunk.Changes, Count(hunk.Changes));

        /// <summary>
        /// A fixed stretch of one difficulty (<see cref="WindowBeats" /> beats) around a change,
        /// before and after. A short change is centred in it; a longer one starts one beat in. The
        /// offset scrolls the window along the song, <see cref="ScrollBeats" /> per step (negative
        /// goes back).
        /// </summary>
        public static WindowResult GetWindow(
            string setKey,
            SnapshotEntry before,
            SnapshotEntry after,
            string difficultyKey,
            double anchor,
            double anchorEnd,
            int offset
        )
        {
            var oldDifficulty = before.Difficulties.FirstOrDefault(d => d.Key == difficultyKey);
            var newDifficulty = after.Difficulties.FirstOrDefault(d => d.Key == difficultyKey);
            var oldMap = oldDifficulty == null ? null : Parse(setKey, oldDifficulty).Beatmap;
            var newMap = newDifficulty == null ? null : Parse(setKey, newDifficulty).Beatmap;
            var reference = newMap ?? oldMap ?? throw new ArgumentException("Unknown difficulty.");

            // The beat length at the change sets the size of the window, so a window of a slow part
            // and one of a fast part both show the same number of beats.
            var reds = reference
                .TimingLines.Where(l => l.Uninherited)
                .OrderBy(l => l.Offset)
                .ToList();
            var red = reds.LastOrDefault(l => l.Offset <= anchor) ?? reds.FirstOrDefault();
            var beatLength = red == null ? 500 : TimingBeatLength(red);
            var span = WindowBeats * beatLength;

            var length = Math.Max(anchorEnd - anchor, 0);
            var lead =
                length >= span - 2 * LeadBeats * beatLength
                    ? LeadBeats * beatLength
                    : (span - length) / 2;

            var from = anchor - lead + offset * ScrollBeats * beatLength;
            var to = from + span;

            // Each side is read a beat past both ends of the window. An object right at an edge can sit
            // just inside in one version and just outside in the other (after a retime or a shift),
            // and would look added or removed; with the margin it finds its counterpart, and the
            // client then shows only what belongs in the window.
            var margin = beatLength;

            // Objects count when they are in view at all, so a slider that began earlier is shown.
            List<VisualObject> Objects(Beatmap? map) =>
                map == null ? []
                : ComboNumbers(map) is var combos
                    ? map
                        .HitObjects.Where(o =>
                            o.time < to + margin && o.GetEndTime() >= from - margin
                        )
                        .OrderBy(o => o.time)
                        .Take(MaxWindowObjects)
                        .Select(o => ToVisual(o, map, combos.GetValueOrDefault(o)))
                        .ToList()
                : [];

            var shown = Objects(newMap ?? oldMap)
                .Where(o => o.Time < to && (o.EndTime ?? o.Time) >= from)
                .ToList();

            return new WindowResult(
                from,
                to,
                shown.Count,
                reference.HitObjects.Any(o => o.GetEndTime() < from),
                reference.HitObjects.Any(o => o.time >= to),
                new HunkVisual(
                    Objects(oldMap),
                    Objects(newMap),
                    false,
                    oldMap == null ? [] : TimingFor(oldMap, from, to),
                    newMap == null ? [] : TimingFor(newMap, from, to)
                )
            );
        }

        private static double TimingBeatLength(TimingLine line) =>
            double.Parse(
                line.Code.Split(',')[1],
                System.Globalization.CultureInfo.InvariantCulture
            );

        /// <summary> The red lines that shape the beat grid over a stretch of the song. </summary>
        private static List<TimingMark> TimingFor(Beatmap map, double from, double to)
        {
            var reds = map.TimingLines.Where(l => l.Uninherited).OrderBy(l => l.Offset).ToList();
            var first = reds.LastOrDefault(l => l.Offset <= from) ?? reds.FirstOrDefault();

            return (first == null ? [] : new List<TimingLine> { first })
                .Concat(reds.Where(l => l.Offset > from && l.Offset <= to && l != first))
                .Select(l => new TimingMark(l.Offset, TimingBeatLength(l), l.Meter))
                .Where(m => m.BeatLength > 0)
                .ToList();
        }

        /// <summary>
        /// The fruits that start a hyperdash, which the game gives a red glow: the fruit itself, or
        /// a juice stream's head, reverses and tail. Droplets never do.
        /// </summary>
        private static List<double>? HyperTimes(HitObject o)
        {
            var times = new List<double>();

            if (o is ICatchHitObject { MovementType: CatchMovementType.Hyperdash } self)
                times.Add(self.Time);

            if (o is JuiceStream stream)
                times.AddRange(
                    stream
                        .Parts.Where(p =>
                            p.Kind != JuiceStream.JuiceStreamPart.PartKind.Droplet
                            && p.MovementType == CatchMovementType.Hyperdash
                        )
                        .Select(p => p.Time)
                );

            return times.Count > 0 ? times : null;
        }

        /// <summary>
        /// The number each object shows in the editor: it counts up from 1 and starts over at a new
        /// combo. The object after a spinner starts a new combo too. Only osu! shows them.
        /// </summary>
        private static Dictionary<HitObject, int> ComboNumbers(Beatmap map)
        {
            var numbers = new Dictionary<HitObject, int>();
            if (map.GeneralSettings.mode != Beatmap.Mode.Standard)
                return numbers;

            var number = 0;
            var startsCombo = true;

            foreach (var o in map.HitObjects.OrderBy(o => o.time))
            {
                if (o.type.HasFlag(HitObject.Types.Spinner))
                {
                    startsCombo = true;
                    continue;
                }

                number = startsCombo || o.type.HasFlag(HitObject.Types.NewCombo) ? 1 : number + 1;
                startsCombo = false;
                numbers[o] = number;
            }

            return numbers;
        }

        /// <summary>
        /// The ticks of a slider: every beat divided by the tick rate, counted again from the start
        /// of each slide, and left out when they would sit right at the end of a slide.
        /// </summary>
        private static List<double>? SliderTicks(HitObject o, Beatmap map)
        {
            var line = map.GetTimingLine<UninheritedLine>(o.time);
            var rate = map.DifficultySettings.sliderTickRate;
            var end = o.GetEndTime();
            var slides = o.GetEdgeTimes().Count() - 1;
            if (line == null || rate <= 0 || slides < 1 || end <= o.time)
                return null;

            var interval = line.msPerBeat / rate;
            var span = (end - o.time) / slides;
            var ticks = new List<double>();

            for (var slide = 0; slide < slides; slide++)
            for (var k = 1; k * interval < span - 10; k++)
                ticks.Add(Math.Round(o.time + slide * span + k * interval, 1));

            return ticks.Count > 0 ? ticks : null;
        }

        /// <summary> The hit sounds of a slider's head, reverses and tail, and its body's whistle. </summary>
        private static List<SoundPart> SoundParts(Slider slider)
        {
            var edges = slider.GetEdgeTimes().ToList();
            var parts = new List<SoundPart>
            {
                new(
                    slider.time,
                    "Head",
                    slider.StartHitSound.ToString(),
                    slider.GetStartSampleset().ToString(),
                    slider.GetStartSampleset(true).ToString()
                ),
            };

            for (var i = 1; i < edges.Count - 1; i++)
                parts.Add(
                    new SoundPart(
                        edges[i],
                        "Repeat",
                        (
                            i - 1 < slider.ReverseHitSounds.Count
                                ? slider.ReverseHitSounds[i - 1]
                                : HitObject.HitSounds.None
                        ).ToString(),
                        slider.GetReverseSampleset(i - 1).ToString(),
                        slider.GetReverseSampleset(i - 1, true).ToString()
                    )
                );

            parts.Add(
                new SoundPart(
                    slider.GetEndTime(),
                    "Tail",
                    slider.EndHitSound.ToString(),
                    slider.GetEndSampleset().ToString(),
                    slider.GetEndSampleset(true).ToString()
                )
            );
            parts.Add(
                new SoundPart(
                    slider.time,
                    "Body",
                    slider.hitSound.ToString(),
                    slider.GetSampleset().ToString(),
                    slider.GetSampleset(true).ToString()
                )
            );

            return parts;
        }

        private static double? StarsOf(Beatmap? beatmap)
        {
            if (beatmap == null)
                return null;

            try
            {
                return Math.Round(beatmap.StarRating, 2);
            }
            catch
            {
                return null;
            }
        }

        private static VisualObject ToVisual(HitObject o, Beatmap map, int combo = 0)
        {
            var type = o.GetObjectType();
            var end = o.GetEndTime();
            int? column =
                map.GeneralSettings.mode == Beatmap.Mode.Mania
                    ? ManiaExtensions.GetColumn(o, map.DifficultySettings.circleSize)
                    : null;

            List<double[]>? path = null;
            List<SoundPart>? sounds = null;

            if (type == "Slider")
            {
                var slider = o as Slider ?? new Slider(o.code.Split(','), map);
                var points = slider.PathPxPositions;
                var step = Math.Max(1, points.Count / PathSamples);

                path = points
                    .Where((_, i) => i % step == 0 || i == points.Count - 1)
                    .Select(p => new double[] { Math.Round(p.X, 1), Math.Round(p.Y, 1) })
                    .ToList();

                if (map.GeneralSettings.mode is Beatmap.Mode.Standard or Beatmap.Mode.Catch)
                    sounds = SoundParts(slider);
            }

            if (sounds == null && type != "Spinner")
            {
                sounds =
                [
                    new SoundPart(
                        o.time,
                        "Head",
                        o.hitSound.ToString(),
                        o.GetSampleset().ToString(),
                        o.GetSampleset(true).ToString()
                    ),
                ];
            }

            return new VisualObject(
                o.time,
                end > o.time ? end : null,
                type,
                Math.Round(o.Position.X, 1),
                Math.Round(o.Position.Y, 1),
                column,
                o.hitSound.ToString(),
                path,
                type == "Slider"
                    ? o.GetEdgeTimes().Skip(1).SkipLast(1).ToList() is { Count: > 0 } edges
                        ? edges
                        : null
                    : null,
                HyperTimes(o),
                combo > 0 ? combo : null,
                o is JuiceStream juice
                    ? juice
                        .Parts.Where(p => p.Kind == JuiceStream.JuiceStreamPart.PartKind.Droplet)
                        .Select(p => p.Time)
                        .ToList()
                        is { Count: > 0 } droplets
                        ? droplets
                        : null
                    : type == "Slider" && map.GeneralSettings.mode == Beatmap.Mode.Standard
                        ? SliderTicks(o, map)
                        : null,
                o.volume ?? (int?)Math.Round(map.GetTimingLine(o.time, true)?.Volume ?? 0),
                sounds
            );
        }
    }
}
