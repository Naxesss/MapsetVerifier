using System.IO;
using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Framework.Objects.Attributes;
using MapsetVerifier.Framework.Objects.Metadata;
using MapsetVerifier.Parser.Objects;
using MapsetVerifier.Parser.Statics;
using MathNet.Numerics;

namespace MapsetVerifier.Checks.Mania.HitSounds
{
    // [Check]
    // temporarily disabled due to flakiness, will need a rework.
    // Ref: https://github.com/Naxesss/MapsetVerifier/issues/100
    public class CheckHitSoundInconsistencies : BeatmapSetCheck
    {
        private record HitSoundEvent(
            HitObject.HitSounds Type,
            double Time,
            string? FileName,
            HitSample.SamplesetType SampleSet,
            string CustomIndex
        );

        private record SampleEvent(string? FileName, double Time, HitObject.HitSounds Sound);

        public override CheckMetadata GetMetadata() =>
            new BeatmapCheckMetadata
            {
                Modes = [Beatmap.Mode.Mania],
                Category = "Hit Sounds",
                Message = "Hitsound inconsistency detected",
                Author = "Tailsdk, Greaper",
            };

        public override Dictionary<string, IssueTemplate> GetTemplates()
        {
            return new Dictionary<string, IssueTemplate>
            {
                {
                    "Warning",
                    new IssueTemplate(
                        Issue.Level.Warning,
                        "{0} has a hitsound inconsistency with {1}.",
                        "timestamp",
                        "beatmap"
                    ).WithCause("There is a hitsound inconsistency")
                },
                {
                    "Problem",
                    new IssueTemplate(
                        Issue.Level.Problem,
                        "{0} is used at {1} but does not exist.",
                        "hitsound",
                        "timestamp"
                    ).WithCause("Missing hitsound")
                },
                {
                    "Double Hitsound",
                    new IssueTemplate(
                        Issue.Level.Warning,
                        "{0} you have 2 of the same samples here make sure this is intentional {1}",
                        "timestamp",
                        "hitsound"
                    ).WithCause("Double hitsound")
                },
            };
        }

        public override IEnumerable<Issue> GetIssues(BeatmapSet beatmapSet)
        {
            var hitSoundFiles = beatmapSet.HitSoundFiles;

            var beatmaps = beatmapSet.Beatmaps.ToList();

            var hitSoundMaps = new List<List<HitSoundEvent>>();
            var beatmapIndex = new List<int>();

            foreach (var (beatmap, i) in beatmaps.Select((b, i) => (b, i)))
            {
                beatmapIndex.Add(i);

                if (beatmap.GeneralSettings.mode != Beatmap.Mode.Mania)
                    continue;

                var hitEvents = new List<HitSoundEvent>();

                var usedAtTime = new HashSet<(HitObject.HitSounds, string?, string, string)>();
                var sampleUsed = new HashSet<(string?, HitObject.HitSounds)>();

                double lastTime = -1;

                foreach (var ho in beatmap.HitObjects)
                {
                    if (ho.time.AlmostEqual(lastTime))
                    {
                        usedAtTime.Clear();
                        sampleUsed.Clear();
                    }

                    lastTime = ho.time;

                    foreach (var type in EnumerateHitSounds(ho))
                    {
                        var ev = BuildEvent(ho, type);

                        if (
                            !usedAtTime.Add(
                                (ev.Type, ev.FileName, ev.SampleSet.ToString(), ev.CustomIndex)
                            )
                        )
                        {
                            yield return new Issue(
                                GetTemplate("Double Hitsound"),
                                beatmap,
                                Timestamp.Get(ev.Time),
                                ho.hitSound
                            );
                            continue;
                        }

                        hitEvents.Add(ev);

                        if (
                            ev.Type != HitObject.HitSounds.None
                            && ev.CustomIndex != "0"
                            && !SampleBaseNameExists(
                                $"{ev.SampleSet.ToString().ToLower()}-hit{ev.Type.ToString().ToLower()}{ev.CustomIndex}",
                                hitSoundFiles,
                                beatmapSet.SongPath
                            )
                        )
                        {
                            yield return new Issue(
                                GetTemplate("Problem"),
                                beatmap,
                                $"{ev.SampleSet.ToString().ToLower()}-hit{ev.Type.ToString().ToLower()}{ev.CustomIndex}.wav/ogg",
                                Timestamp.Get(ev.Time)
                            );
                        }
                    }

                    if (!sampleUsed.Add((ho.filename, ho.hitSound)))
                        continue;

                    if (ho.filename != null && !HitsoundFileExistsInSet(beatmapSet, ho.filename))
                    {
                        yield return new Issue(
                            GetTemplate("Problem"),
                            beatmap,
                            ho.filename,
                            Timestamp.Get(ho.time)
                        );
                    }
                }

                hitSoundMaps.Add(hitEvents);
            }

            foreach (var issue in CompareBeatmaps(hitSoundMaps, beatmapIndex, beatmapSet))
                yield return issue;
        }

        private static IEnumerable<HitObject.HitSounds> EnumerateHitSounds(HitObject ho)
        {
            if (ho.HasHitSound(HitObject.HitSounds.Clap))
                yield return HitObject.HitSounds.Clap;
            if (ho.HasHitSound(HitObject.HitSounds.Normal))
                yield return HitObject.HitSounds.Normal;
            if (ho.HasHitSound(HitObject.HitSounds.None))
                yield return HitObject.HitSounds.None;
            if (ho.HasHitSound(HitObject.HitSounds.Whistle))
                yield return HitObject.HitSounds.Whistle;
            if (ho.HasHitSound(HitObject.HitSounds.Finish))
                yield return HitObject.HitSounds.Finish;
        }

        private static HitSoundEvent BuildEvent(HitObject ho, HitObject.HitSounds type)
        {
            return new HitSoundEvent(
                type == HitObject.HitSounds.None && ho.filename != null
                    ? HitObject.HitSounds.None
                    : type,
                ho.time,
                ho.filename,
                ResolveSampleSet(ho, type),
                ResolveIndex(ho)
            );
        }

        /// <summary>
        ///     Match <see cref="HitObject.GetEdgeSample" /> sampleset selection (addition applies to hitsound additions, not hitnormal).
        /// </summary>
        private static HitSample.SamplesetType ResolveSampleSet(
            HitObject ho,
            HitObject.HitSounds type
        )
        {
            if (type is HitObject.HitSounds.Normal or HitObject.HitSounds.None)
                return ho.GetSampleset(false);

            return ho.GetSampleset(true);
        }

        /// <summary>
        ///     Match <see cref="HitObject.GetEdgeSample" />: custom index comes from
        ///     <see cref="Beatmap.GetTimingLine" /> with hit sound leniency.
        /// </summary>
        private static string ResolveIndex(HitObject ho)
        {
            var line = ho.beatmap.GetTimingLine(ho.time, true);
            if (line == null)
                return "";

            return line.CustomIndex == 1 ? "" : line.CustomIndex.ToString();
        }

        private IEnumerable<Issue> CompareBeatmaps(
            List<List<HitSoundEvent>> hs,
            List<int> indices,
            BeatmapSet set
        )
        {
            for (int i = 0; i < hs.Count; i++)
            for (int j = 0; j < hs.Count; j++)
            {
                if (i == j)
                    continue;

                foreach (var e in hs[i])
                {
                    if (e.Type == HitObject.HitSounds.None)
                        continue;

                    bool hasNote = false;

                    foreach (var o in hs[j])
                    {
                        if (SameHit(e, o))
                            break;

                        if (SameTimeDifferentObject(e, o))
                            hasNote = true;

                        if (o.Time > e.Time)
                        {
                            if (hasNote)
                            {
                                yield return new Issue(
                                    GetTemplate("Warning"),
                                    set.Beatmaps[indices[i]],
                                    Timestamp.Get(e.Time),
                                    set.Beatmaps[indices[j]]
                                );
                            }
                            break;
                        }
                    }
                }
            }
        }

        private static bool SameHit(HitSoundEvent a, HitSoundEvent b) =>
            a.Time.AlmostEqual(b.Time)
            && a.Type == b.Type
            && a.SampleSet == b.SampleSet
            && a.CustomIndex == b.CustomIndex;

        private static bool SameTimeDifferentObject(HitSoundEvent a, HitSoundEvent b) =>
            a.Time.AlmostEqual(b.Time) && b.FileName == null && a.SampleSet == b.SampleSet;

        /// <summary>
        ///     Whether the set lists a hitsound file whose basename (no extension) matches the osu sample basename,
        ///     or the song folder contains such a file (fallback when indexing missed it).
        /// </summary>
        private static bool SampleBaseNameExists(
            string baseName,
            IReadOnlyList<string> hitSoundFiles,
            string songFolder
        )
        {
            var normalized = PathStatic.ParsePath(baseName, true);
            if (string.IsNullOrEmpty(normalized))
                return false;

            if (
                hitSoundFiles.Any(hs =>
                    PathStatic.ParsePath(PathStatic.CutPath(hs), true) == normalized
                )
            )
                return true;

            return FileBasenameExistsOnDisk(normalized, songFolder);
        }

        /// <summary>
        ///     Whether <paramref name="filename"/> (as in an .osu hit object) matches a path in
        ///     <see cref="BeatmapSet.HitSoundFiles" /> or exists on disk under the song folder.
        /// </summary>
        private static bool HitsoundFileExistsInSet(BeatmapSet beatmapSet, string filename)
        {
            var needle = PathStatic.ParsePath(PathStatic.CutPath(filename));
            if (
                beatmapSet.HitSoundFiles.Any(hs =>
                    PathStatic.ParsePath(PathStatic.CutPath(hs)) == needle
                )
            )
                return true;

            var withoutExt = PathStatic.ParsePath(PathStatic.CutPath(filename), true);
            return !string.IsNullOrEmpty(withoutExt)
                && FileBasenameExistsOnDisk(withoutExt, beatmapSet.SongPath);
        }

        private static bool FileBasenameExistsOnDisk(string normalizedBaseNoExt, string songFolder)
        {
            try
            {
                if (!Directory.Exists(songFolder))
                    return false;

                return Directory
                    .EnumerateFiles(songFolder, "*.*", SearchOption.AllDirectories)
                    .Any(fullPath =>
                        PathStatic.ParsePath(PathStatic.CutPath(fullPath), true)
                        == normalizedBaseNoExt
                    );
            }
            catch
            {
                return false;
            }
        }
    }
}
