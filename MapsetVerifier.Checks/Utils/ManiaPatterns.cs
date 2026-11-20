using MapsetVerifier.Parser.Objects;
using MapsetVerifier.Parser.Objects.HitObjects;
using MapsetVerifier.Parser.Objects.HitObjects.Mania;
using MapsetVerifier.Parser.Objects.TimingLines;

namespace MapsetVerifier.Checks.Utils;

/// <summary>
///     Finds the note patterns the osu!mania difficulty guidelines talk about, such as anchors, jacks,
///     trills and chords inside streams.
///     <para>
///         Gaps are measured in beats of the song, like mania mappers snap them. The Scaling BPM page treats
///         120 and 240 BPM as breaking points, so from 240 BPM a beat counts double (1/2 is as dense as 1/4)
///         and below 120 BPM it counts half (1/4 is as dense as 1/2).
///     </para>
/// </summary>
public static class ManiaPatterns
{
    /// <summary> Snaps a little off the grid still count, as long as they are within this share of the snap. </summary>
    private const double SnapTolerance = 0.1;

    public sealed record Note(HitObject Object, int Column)
    {
        public double Time => Object.time;
        public double End => Object.GetEndTime();
        public bool IsLong => Object is HoldNote;
    }

    /// <summary> Notes starting at the same time, a single note or a chord. </summary>
    public sealed record Row(double Time, List<Note> Notes)
    {
        public bool IsChord => Notes.Count > 1;
        public HitObject[] Objects => Notes.Select(note => note.Object).ToArray();
    }

    /// <summary> A run of rows or notes forming a pattern, from the first to the last, with how many it counts. </summary>
    public readonly record struct Run(HitObject[] Objects, int Count);

    public static int GetKeys(Beatmap beatmap) =>
        Math.Max(1, (int)beatmap.DifficultySettings.circleSize);

    public static List<Note> GetNotes(Beatmap beatmap)
    {
        var keys = GetKeys(beatmap);

        return beatmap
            .HitObjects.Where(hitObject => hitObject is not Spinner)
            .Select(hitObject => new Note(hitObject, ManiaUtils.getColumn(hitObject, keys)))
            .ToList();
    }

    public static List<Row> GetRows(Beatmap beatmap)
    {
        var rows = new List<Row>();

        foreach (var note in GetNotes(beatmap).OrderBy(note => note.Time))
        {
            if (rows.Count > 0 && Math.Abs(rows[^1].Time - note.Time) < 2)
                rows[^1].Notes.Add(note);
            else
                rows.Add(new Row(note.Time, [note]));
        }

        return rows;
    }

    /// <summary> The gap between two times in beats, counted as described in the class summary. </summary>
    public static double Beats(Beatmap beatmap, double from, double to)
    {
        var line = beatmap.GetTimingLine<UninheritedLine>(from);
        var beatLength = line?.msPerBeat ?? 500;
        var bpm = 60000 / beatLength;

        if (bpm >= 240)
            beatLength *= 2;
        else if (bpm < 120)
            beatLength /= 2;

        return (to - from) / beatLength;
    }

    /// <summary> Whether a gap is the given snap or faster, such as 1/4 for 4. </summary>
    public static bool IsAtLeast(Beatmap beatmap, double from, double to, double divisor) =>
        to > from && Beats(beatmap, from, to) * divisor <= 1 + SnapTolerance;

    /// <summary> Runs of rows that follow each other at the given snap or faster. Counts the rows. </summary>
    public static IEnumerable<List<Row>> RowRuns(Beatmap beatmap, double divisor)
    {
        var run = new List<Row>();

        foreach (var row in GetRows(beatmap))
        {
            if (run.Count > 0 && !IsAtLeast(beatmap, run[^1].Time, row.Time, divisor))
            {
                if (run.Count > 1)
                    yield return run;

                run = [];
            }

            run.Add(row);
        }

        if (run.Count > 1)
            yield return run;
    }

    /// <summary> Chords in a run of rows at the given snap, other than the first and last row. </summary>
    public static IEnumerable<Row> ChordsInsideRuns(
        Beatmap beatmap,
        double divisor,
        int minRows = 3
    ) =>
        RowRuns(beatmap, divisor)
            .Where(run => run.Count >= minRows)
            .SelectMany(run => run.Skip(1).Take(run.Count - 2))
            .Where(row => row.IsChord);

    /// <summary>
    ///     Jacks: notes in the same column on consecutive rows, at the given snap or faster. Each run counts
    ///     its notes, so a minijack is 2.
    /// </summary>
    public static IEnumerable<Run> Jacks(Beatmap beatmap, double divisor)
    {
        var rows = GetRows(beatmap);
        var runs = new Dictionary<int, List<Note>>();

        for (var i = 0; i < rows.Count; i++)
        {
            var columns = rows[i].Notes.Select(note => note.Column).ToHashSet();

            foreach (var column in runs.Keys.Where(column => !columns.Contains(column)).ToList())
            foreach (var run in Finish(runs, column))
                yield return run;

            foreach (var note in rows[i].Notes)
            {
                if (
                    runs.TryGetValue(note.Column, out var run)
                    && IsAtLeast(beatmap, run[^1].Time, note.Time, divisor)
                )
                {
                    run.Add(note);
                    continue;
                }

                foreach (var finished in Finish(runs, note.Column))
                    yield return finished;

                runs[note.Column] = [note];
            }
        }

        foreach (var column in runs.Keys.ToList())
        foreach (var run in Finish(runs, column))
            yield return run;
    }

    /// <summary>
    ///     Anchors: a column hit on every other row of a stream at the given snap, so the same finger keeps
    ///     returning while the others move. A trill, where the notes in between are all in one other column,
    ///     is not an anchor. Each run counts the notes in that column.
    /// </summary>
    public static IEnumerable<Run> Anchors(Beatmap beatmap, double divisor)
    {
        foreach (var stream in RowRuns(beatmap, divisor).Where(run => run.Count >= 3))
        {
            var columns = stream
                .SelectMany(row => row.Notes)
                .Select(note => note.Column)
                .Distinct();

            foreach (var column in columns)
            {
                var chain = new List<(Note Note, int Index)>();

                for (var i = 0; i < stream.Count; i++)
                {
                    if (
                        stream[i].Notes.FirstOrDefault(note => note.Column == column)
                        is not { } note
                    )
                        continue;

                    if (chain.Count > 0 && i - chain[^1].Index != 2)
                    {
                        if (ToAnchor(stream, chain) is { } anchor)
                            yield return anchor;

                        chain = [];
                    }

                    chain.Add((note, i));
                }

                if (ToAnchor(stream, chain) is { } last)
                    yield return last;
            }
        }
    }

    private static Run? ToAnchor(List<Row> stream, List<(Note Note, int Index)> chain)
    {
        if (chain.Count < 3)
            return null;

        var between = chain.Skip(1).Select(link => stream[link.Index - 1]).ToList();
        var isTrill =
            between.All(row => !row.IsChord)
            && between.Select(row => row.Notes[0].Column).Distinct().Count() == 1;

        return isTrill
            ? null
            : new Run(chain.Select(link => link.Note.Object).ToArray(), chain.Count);
    }

    /// <summary>
    ///     Trills: single notes alternating between the same two columns at the given snap or faster. Each
    ///     run counts its notes.
    /// </summary>
    public static IEnumerable<Run> Trills(Beatmap beatmap, double divisor)
    {
        foreach (var stream in RowRuns(beatmap, divisor))
        {
            var run = new List<Note>();

            foreach (var row in stream)
            {
                var note = row.IsChord ? null : row.Notes[0];
                var continues =
                    note != null
                    && run.Count > 0
                    && note.Column != run[^1].Column
                    && (run.Count < 2 || note.Column == run[^2].Column);

                if (!continues)
                {
                    if (run.Count >= 4)
                        yield return new Run(run.Select(n => n.Object).ToArray(), run.Count);

                    run = note == null ? [] : [note];
                    continue;
                }

                run.Add(note!);
            }

            if (run.Count >= 4)
                yield return new Run(run.Select(n => n.Object).ToArray(), run.Count);
        }
    }

    /// <summary> Long notes held for less than the given number of beats. </summary>
    public static IEnumerable<Note> ShortLongNotes(Beatmap beatmap, double minBeats) =>
        GetNotes(beatmap)
            .Where(note => note.IsLong)
            .Where(note => Beats(beatmap, note.Time, note.End) < minBeats * (1 - SnapTolerance));

    /// <summary> Pairs of long notes released at different times, less than the given number of beats apart. </summary>
    public static IEnumerable<(Note First, Note Second)> CloseReleases(
        Beatmap beatmap,
        double minBeats
    )
    {
        var longNotes = GetNotes(beatmap)
            .Where(note => note.IsLong)
            .OrderBy(note => note.End)
            .ToList();

        for (var i = 1; i < longNotes.Count; i++)
        {
            var (first, second) = (longNotes[i - 1], longNotes[i]);

            if (second.End - first.End < 2)
                continue;

            if (Beats(beatmap, first.End, second.End) < minBeats * (1 - SnapTolerance))
                yield return (first, second);
        }
    }

    /// <summary> Long notes in neighbouring columns that are held at the same time but released apart. </summary>
    public static IEnumerable<(Note First, Note Second)> AdjacentReleaseMismatches(Beatmap beatmap)
    {
        var longNotes = GetNotes(beatmap)
            .Where(note => note.IsLong)
            .OrderBy(note => note.Time)
            .ToList();

        for (var i = 0; i < longNotes.Count; i++)
        for (var j = i + 1; j < longNotes.Count && longNotes[j].Time < longNotes[i].End; j++)
        {
            var (first, second) = (longNotes[i], longNotes[j]);

            if (
                Math.Abs(first.Column - second.Column) == 1
                && Math.Abs(first.End - second.End) >= 2
            )
                yield return (first, second);
        }
    }

    /// <summary>
    ///     Stretches where the scroll speed stays at one multiplier, compared to the main BPM, together with
    ///     how many beats they last and the objects in them. Normalising lines for other BPMs give 1x.
    /// </summary>
    public static IEnumerable<(
        double Multiplier,
        double Beats,
        List<HitObject> Objects
    )> ScrollSections(Beatmap beatmap)
    {
        var baseBpm = ManiaUtils.GetMostCommonBeatLength(beatmap);
        var lines = beatmap.TimingLines.OrderBy(line => line.Offset).ToList();
        var end = beatmap.HitObjects.LastOrDefault()?.GetEndTime() ?? 0;

        if (baseBpm <= 0 || lines.Count == 0)
            yield break;

        double? sectionStart = null;
        double sectionMultiplier = 1;
        var bpm = baseBpm;

        foreach (var (line, index) in lines.Select((line, index) => (line, index)))
        {
            if (line is UninheritedLine uninherited)
                bpm = uninherited.bpm;

            var multiplier = Math.Round(bpm / baseBpm * (line.Uninherited ? 1 : line.SvMult), 2);

            if (sectionStart is { } start && Math.Abs(multiplier - sectionMultiplier) > 0.001)
            {
                yield return Section(start, line.Offset);
                sectionStart = null;
            }

            if (sectionStart == null)
            {
                sectionStart = line.Offset;
                sectionMultiplier = multiplier;
            }
        }

        if (sectionStart is { } lastStart && end > lastStart)
            yield return Section(lastStart, end);

        (double, double, List<HitObject>) Section(double from, double to) =>
            (
                sectionMultiplier,
                Beats(beatmap, from, to),
                beatmap
                    .HitObjects.Where(hitObject => hitObject.time >= from && hitObject.time < to)
                    .ToList()
            );
    }

    private static IEnumerable<Run> Finish(Dictionary<int, List<Note>> runs, int column)
    {
        if (runs.Remove(column, out var run) && run.Count >= 2)
            yield return new Run(run.Select(note => note.Object).ToArray(), run.Count);
    }
}
