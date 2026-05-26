using MapsetVerifier.Parser.Objects;
using MapsetVerifier.Snapshots;
using MapsetVerifier.Snapshots.Translators;
using MapsetVerifier.Snapshots.Objects;
using Xunit;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace MapsetVerifier.Parser.Tests.Objects
{
    public class HitObjectsTranslatorTests
    {
        [Fact]
        public void Translate_ExactMatch_NoChanges()
        {
            var osuCode = BuildOsu(new[]
            {
                "100,100,1000,1,0,0:0:0:0:",
                "200,200,2000,1,0,0:0:0:0:",
                "300,300,3000,1,0,0:0:0:0:"
            });

            var beatmap = new Beatmap(osuCode, "", "test.osu");
            var translator = new HitObjectsTranslator();

            var result = translator.Translate(new List<DiffInstance>(), beatmap).ToList();
            Assert.Empty(result);
        }

        [Fact]
        public void Translate_SectionalShift_CleanShift_NoObjectDiffs()
        {
            var oldOsu = BuildOsu(new[]
            {
                "100,100,1000,1,0,0:0:0:0:",
                "200,200,2000,1,0,0:0:0:0:",
                "300,300,3000,1,0,0:0:0:0:",
                "400,400,4000,1,0,0:0:0:0:",
                "500,500,5000,1,0,0:0:0:0:"
            });

            var newOsu = BuildOsu(new[]
            {
                "100,100,1005,1,0,0:0:0:0:",
                "200,200,2005,1,0,0:0:0:0:",
                "300,300,3005,1,0,0:0:0:0:",
                "400,400,4005,1,0,0:0:0:0:",
                "500,500,5005,1,0,0:0:0:0:"
            });

            var beatmap = new Beatmap(newOsu, "", "test.osu");
            var now = DateTime.UtcNow;
            var creationDate = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, now.Second, DateTimeKind.Utc);

            var diffs = new List<DiffInstance>
            {
                new DiffInstance("100,100,1005,1,0,0:0:0:0:", "HitObjects", Snapshotter.DiffType.Added, new List<string>(), creationDate),
                new DiffInstance("200,200,2005,1,0,0:0:0:0:", "HitObjects", Snapshotter.DiffType.Added, new List<string>(), creationDate),
                new DiffInstance("300,300,3005,1,0,0:0:0:0:", "HitObjects", Snapshotter.DiffType.Added, new List<string>(), creationDate),
                new DiffInstance("400,400,4005,1,0,0:0:0:0:", "HitObjects", Snapshotter.DiffType.Added, new List<string>(), creationDate),
                new DiffInstance("500,500,5005,1,0,0:0:0:0:", "HitObjects", Snapshotter.DiffType.Added, new List<string>(), creationDate),
                new DiffInstance("100,100,1000,1,0,0:0:0:0:", "HitObjects", Snapshotter.DiffType.Removed, new List<string>(), creationDate),
                new DiffInstance("200,200,2000,1,0,0:0:0:0:", "HitObjects", Snapshotter.DiffType.Removed, new List<string>(), creationDate),
                new DiffInstance("300,300,3000,1,0,0:0:0:0:", "HitObjects", Snapshotter.DiffType.Removed, new List<string>(), creationDate),
                new DiffInstance("400,400,4000,1,0,0:0:0:0:", "HitObjects", Snapshotter.DiffType.Removed, new List<string>(), creationDate),
                new DiffInstance("500,500,5000,1,0,0:0:0:0:", "HitObjects", Snapshotter.DiffType.Removed, new List<string>(), creationDate),
            };

            SetupMockSnapshot(oldOsu, creationDate);

            beatmap.MetadataSettings.beatmapSetId = 123;
            beatmap.MetadataSettings.beatmapId = 456;

            var translator = new HitObjectsTranslator();
            var result = translator.Translate(diffs, beatmap).ToList();

            Assert.Single(result);
            var sectionDiff = result[0];
            Assert.Contains("Section shifted in time by +5 ms", sectionDiff.Diff);
            Assert.Empty(sectionDiff.Details);
        }

        [Fact]
        public void Translate_SectionalShift_WithModificationAndAddition()
        {
            var oldOsu = BuildOsu(new[]
            {
                "100,100,1000,1,0,0:0:0:0:",
                "200,200,2000,1,0,0:0:0:0:",
                "300,300,3000,1,0,0:0:0:0:",
                "400,400,4000,1,0,0:0:0:0:",
                "500,500,5000,1,0,0:0:0:0:"
            });

            var newOsu = BuildOsu(new[]
            {
                "100,100,1005,1,0,0:0:0:0:",
                "250,250,2005,1,0,0:0:0:0:", // Modified (moved)
                "300,300,3005,1,0,0:0:0:0:",
                "400,400,4005,1,0,0:0:0:0:",
                "500,500,5005,1,0,0:0:0:0:",
                "600,600,6005,1,0,0:0:0:0:"  // Added trailing
            });

            var beatmap = new Beatmap(newOsu, "", "test.osu");
            var now = DateTime.UtcNow;
            var creationDate = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, now.Second, DateTimeKind.Utc);

            var diffs = new List<DiffInstance>
            {
                new DiffInstance("100,100,1005,1,0,0:0:0:0:", "HitObjects", Snapshotter.DiffType.Added, new List<string>(), creationDate),
                new DiffInstance("250,250,2005,1,0,0:0:0:0:", "HitObjects", Snapshotter.DiffType.Added, new List<string>(), creationDate),
                new DiffInstance("300,300,3005,1,0,0:0:0:0:", "HitObjects", Snapshotter.DiffType.Added, new List<string>(), creationDate),
                new DiffInstance("400,400,4005,1,0,0:0:0:0:", "HitObjects", Snapshotter.DiffType.Added, new List<string>(), creationDate),
                new DiffInstance("500,500,5005,1,0,0:0:0:0:", "HitObjects", Snapshotter.DiffType.Added, new List<string>(), creationDate),
                new DiffInstance("600,600,6005,1,0,0:0:0:0:", "HitObjects", Snapshotter.DiffType.Added, new List<string>(), creationDate),
                new DiffInstance("100,100,1000,1,0,0:0:0:0:", "HitObjects", Snapshotter.DiffType.Removed, new List<string>(), creationDate),
                new DiffInstance("200,200,2000,1,0,0:0:0:0:", "HitObjects", Snapshotter.DiffType.Removed, new List<string>(), creationDate),
                new DiffInstance("300,300,3000,1,0,0:0:0:0:", "HitObjects", Snapshotter.DiffType.Removed, new List<string>(), creationDate),
                new DiffInstance("400,400,4000,1,0,0:0:0:0:", "HitObjects", Snapshotter.DiffType.Removed, new List<string>(), creationDate),
                new DiffInstance("500,500,5000,1,0,0:0:0:0:", "HitObjects", Snapshotter.DiffType.Removed, new List<string>(), creationDate),
            };

            SetupMockSnapshot(oldOsu, creationDate);

            beatmap.MetadataSettings.beatmapSetId = 123;
            beatmap.MetadataSettings.beatmapId = 456;

            var translator = new HitObjectsTranslator();
            var result = translator.Translate(diffs, beatmap).ToList();

            // We expect 3 diffs: the (flat) section shift summary, the modified position, and the trailing addition.
            Assert.Equal(3, result.Count);

            var sectionDiff = result[0];
            Assert.Contains("Section shifted in time by +5 ms", sectionDiff.Diff);
            Assert.Empty(sectionDiff.Details);

            var moveDiff = result[1];
            Assert.Contains("Circle changed", moveDiff.Diff);
            Assert.Contains("Moved from (200; 200) to (250; 250)", moveDiff.Details[0]);

            var addedDiff = result[2];
            Assert.Contains("Circle added", addedDiff.Diff);
        }

        [Fact]
        public void Translate_SectionalShift_WithIntermediateAddition()
        {
            var oldOsu = BuildOsu(new[]
            {
                "100,100,1000,1,0,0:0:0:0:",
                "300,300,3000,1,0,0:0:0:0:",
                "400,400,4000,1,0,0:0:0:0:",
                "500,500,5000,1,0,0:0:0:0:",
                "600,600,6000,1,0,0:0:0:0:"
            });

            var newOsu = BuildOsu(new[]
            {
                "100,100,1005,1,0,0:0:0:0:",
                "200,200,2005,1,0,0:0:0:0:", // Added in between
                "300,300,3005,1,0,0:0:0:0:",
                "400,400,4005,1,0,0:0:0:0:",
                "500,500,5005,1,0,0:0:0:0:",
                "600,600,6005,1,0,0:0:0:0:"
            });

            var beatmap = new Beatmap(newOsu, "", "test.osu");
            var now = DateTime.UtcNow;
            var creationDate = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, now.Second, DateTimeKind.Utc);

            var diffs = new List<DiffInstance>
            {
                new DiffInstance("100,100,1005,1,0,0:0:0:0:", "HitObjects", Snapshotter.DiffType.Added, new List<string>(), creationDate),
                new DiffInstance("200,200,2005,1,0,0:0:0:0:", "HitObjects", Snapshotter.DiffType.Added, new List<string>(), creationDate),
                new DiffInstance("300,300,3005,1,0,0:0:0:0:", "HitObjects", Snapshotter.DiffType.Added, new List<string>(), creationDate),
                new DiffInstance("400,400,4005,1,0,0:0:0:0:", "HitObjects", Snapshotter.DiffType.Added, new List<string>(), creationDate),
                new DiffInstance("500,500,5005,1,0,0:0:0:0:", "HitObjects", Snapshotter.DiffType.Added, new List<string>(), creationDate),
                new DiffInstance("600,600,6005,1,0,0:0:0:0:", "HitObjects", Snapshotter.DiffType.Added, new List<string>(), creationDate),
                new DiffInstance("100,100,1000,1,0,0:0:0:0:", "HitObjects", Snapshotter.DiffType.Removed, new List<string>(), creationDate),
                new DiffInstance("300,300,3000,1,0,0:0:0:0:", "HitObjects", Snapshotter.DiffType.Removed, new List<string>(), creationDate),
                new DiffInstance("400,400,4000,1,0,0:0:0:0:", "HitObjects", Snapshotter.DiffType.Removed, new List<string>(), creationDate),
                new DiffInstance("500,500,5000,1,0,0:0:0:0:", "HitObjects", Snapshotter.DiffType.Removed, new List<string>(), creationDate),
                new DiffInstance("600,600,6000,1,0,0:0:0:0:", "HitObjects", Snapshotter.DiffType.Removed, new List<string>(), creationDate),
            };

            SetupMockSnapshot(oldOsu, creationDate);

            beatmap.MetadataSettings.beatmapSetId = 123;
            beatmap.MetadataSettings.beatmapId = 456;

            var translator = new HitObjectsTranslator();
            var result = translator.Translate(diffs, beatmap).ToList();

            // We expect 2 diffs: the (flat) section shift summary and the intermediate addition emitted as a separate entry.
            Assert.Equal(2, result.Count);

            var sectionDiff = result[0];
            Assert.Contains("Section shifted in time by +5 ms", sectionDiff.Diff);
            Assert.Empty(sectionDiff.Details);

            var addedDiff = result[1];
            Assert.Contains("Circle added", addedDiff.Diff);
        }

        [Fact]
        public void Translate_HistoricalShift_CleanShift_NoObjectDiffs()
        {
            var oldOsu = BuildOsu(new[]
            {
                "100,100,1000,1,0,0:0:0:0:",
                "200,200,2000,1,0,0:0:0:0:",
                "300,300,3000,1,0,0:0:0:0:",
                "400,400,4000,1,0,0:0:0:0:",
                "500,500,5000,1,0,0:0:0:0:"
            });

            var newOsu = BuildOsu(new[]
            {
                "100,100,1005,1,0,0:0:0:0:",
                "200,200,2005,1,0,0:0:0:0:",
                "300,300,3005,1,0,0:0:0:0:",
                "400,400,4005,1,0,0:0:0:0:",
                "500,500,5005,1,0,0:0:0:0:"
            });

            var currentOsu = BuildOsu(new[]
            {
                "500,500,5000,1,0,0:0:0:0:"
            });

            var beatmap = new Beatmap(currentOsu, "", "test.osu");
            var now = DateTime.UtcNow;
            var creationDate1 = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, now.Second, DateTimeKind.Utc).AddMinutes(-10);
            var creationDate2 = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, now.Second, DateTimeKind.Utc).AddMinutes(-5);

            var diffs = new List<DiffInstance>
            {
                new DiffInstance("100,100,1005,1,0,0:0:0:0:", "HitObjects", Snapshotter.DiffType.Added, new List<string>(), creationDate1),
                new DiffInstance("200,200,2005,1,0,0:0:0:0:", "HitObjects", Snapshotter.DiffType.Added, new List<string>(), creationDate1),
                new DiffInstance("300,300,3005,1,0,0:0:0:0:", "HitObjects", Snapshotter.DiffType.Added, new List<string>(), creationDate1),
                new DiffInstance("400,400,4005,1,0,0:0:0:0:", "HitObjects", Snapshotter.DiffType.Added, new List<string>(), creationDate1),
                new DiffInstance("500,500,5005,1,0,0:0:0:0:", "HitObjects", Snapshotter.DiffType.Added, new List<string>(), creationDate1),
                new DiffInstance("100,100,1000,1,0,0:0:0:0:", "HitObjects", Snapshotter.DiffType.Removed, new List<string>(), creationDate1),
                new DiffInstance("200,200,2000,1,0,0:0:0:0:", "HitObjects", Snapshotter.DiffType.Removed, new List<string>(), creationDate1),
                new DiffInstance("300,300,3000,1,0,0:0:0:0:", "HitObjects", Snapshotter.DiffType.Removed, new List<string>(), creationDate1),
                new DiffInstance("400,400,4000,1,0,0:0:0:0:", "HitObjects", Snapshotter.DiffType.Removed, new List<string>(), creationDate1),
                new DiffInstance("500,500,5000,1,0,0:0:0:0:", "HitObjects", Snapshotter.DiffType.Removed, new List<string>(), creationDate1),
            };

            SetupMockSnapshots(new[] { oldOsu, newOsu }, new[] { creationDate1, creationDate2 });

            beatmap.MetadataSettings.beatmapSetId = 123;
            beatmap.MetadataSettings.beatmapId = 456;

            var translator = new HitObjectsTranslator();
            var result = translator.Translate(diffs, beatmap).ToList();

            Assert.Single(result);
            var sectionDiff = result[0];
            Assert.Contains("Section shifted in time by +5 ms", sectionDiff.Diff);
            Assert.Empty(sectionDiff.Details);
        }

        [Fact]
        public void Translate_NoDiffs_NoCrash()
        {
            var osuCode = BuildOsu(new[] { "100,100,1000,1,0,0:0:0:0:" });
            var beatmap = new Beatmap(osuCode, "", "test.osu");
            var result = new HitObjectsTranslator().Translate(new List<DiffInstance>(), beatmap).ToList();
            Assert.Empty(result);
        }

        [Fact]
        public void Translate_NoBeatmap_NoCrash()
        {
            var result = new HitObjectsTranslator().Translate(new List<DiffInstance>(), null!).ToList();
            Assert.Empty(result);
        }

        [Fact]
        public void Translate_SingleObject_NoSnapshot_NoCrash()
        {
            // No mock snapshot - translator goes through fallback path; should not throw.
            var osuCode = BuildOsu(new[] { "100,100,1000,1,0,0:0:0:0:" });
            var beatmap = new Beatmap(osuCode, "", "test.osu");
            var creationDate = DateTime.UtcNow;
            var diffs = new List<DiffInstance>
            {
                new DiffInstance("100,100,1000,1,0,0:0:0:0:", "HitObjects", Snapshotter.DiffType.Added, new List<string>(), creationDate),
            };

            var appDataPath = Path.Combine(Path.GetTempPath(), "MapsetVerifierSnapshotsTests_HO_Empty");
            if (Directory.Exists(appDataPath)) Directory.Delete(appDataPath, true);
            Snapshotter.ConfigurePath(appDataPath, "test_folder");

            beatmap.MetadataSettings.beatmapSetId = 999;
            beatmap.MetadataSettings.beatmapId = 999;

            var result = new HitObjectsTranslator().Translate(diffs, beatmap).ToList();
            Assert.Single(result);
        }

        [Fact]
        public void Translate_EmptyOldHitObjects_NoCrash()
        {
            // Old snapshot has no hit objects at all; current has one. DTW degenerates to
            // pure insertions; RANSAC sees zero matched pairs and must degrade gracefully.
            var oldOsu = BuildOsu(System.Array.Empty<string>());
            var newOsu = BuildOsu(new[] { "100,100,1000,1,0,0:0:0:0:" });

            var beatmap = new Beatmap(newOsu, "", "test.osu");
            var creationDate = DateTime.UtcNow;
            var diffs = new List<DiffInstance>
            {
                new DiffInstance("100,100,1000,1,0,0:0:0:0:", "HitObjects", Snapshotter.DiffType.Added, new List<string>(), creationDate),
            };

            SetupMockSnapshot(oldOsu, creationDate);
            beatmap.MetadataSettings.beatmapSetId = 123;
            beatmap.MetadataSettings.beatmapId = 456;

            var result = new HitObjectsTranslator().Translate(diffs, beatmap).ToList();
            Assert.Contains(result, r => r.DiffType == Snapshotter.DiffType.Added);
        }

        [Fact]
        public void Translate_EmptyNewHitObjects_NoCrash()
        {
            var oldOsu = BuildOsu(new[] { "100,100,1000,1,0,0:0:0:0:" });
            var newOsu = BuildOsu(System.Array.Empty<string>());

            var beatmap = new Beatmap(newOsu, "", "test.osu");
            var creationDate = DateTime.UtcNow;
            var diffs = new List<DiffInstance>
            {
                new DiffInstance("100,100,1000,1,0,0:0:0:0:", "HitObjects", Snapshotter.DiffType.Removed, new List<string>(), creationDate),
            };

            SetupMockSnapshot(oldOsu, creationDate);
            beatmap.MetadataSettings.beatmapSetId = 123;
            beatmap.MetadataSettings.beatmapId = 456;

            var result = new HitObjectsTranslator().Translate(diffs, beatmap).ToList();
            Assert.Contains(result, r => r.DiffType == Snapshotter.DiffType.Removed);
        }

        [Fact]
        public void Translate_BothEmpty_NoCrash()
        {
            var oldOsu = BuildOsu(System.Array.Empty<string>());
            var newOsu = BuildOsu(System.Array.Empty<string>());

            var beatmap = new Beatmap(newOsu, "", "test.osu");
            var creationDate = DateTime.UtcNow;
            // No diffs to seed; translator should produce no output.
            var diffs = new List<DiffInstance>();

            SetupMockSnapshot(oldOsu, creationDate);
            beatmap.MetadataSettings.beatmapSetId = 123;
            beatmap.MetadataSettings.beatmapId = 456;

            var result = new HitObjectsTranslator().Translate(diffs, beatmap).ToList();
            Assert.Empty(result);
        }

        [Fact]
        public void Translate_FewObjects_NoSpuriousShift()
        {
            // Few objects with low MinShiftInliersHitObjects threshold - 4 matched pairs
            // shifted uniformly shouldn't reach the hit-object 5-inlier minimum.
            var oldOsu = BuildOsu(new[]
            {
                "100,100,1000,1,0,0:0:0:0:",
                "200,200,2000,1,0,0:0:0:0:",
                "300,300,3000,1,0,0:0:0:0:",
                "400,400,4000,1,0,0:0:0:0:",
            });
            var newOsu = BuildOsu(new[]
            {
                "100,100,1050,1,0,0:0:0:0:",
                "200,200,2050,1,0,0:0:0:0:",
                "300,300,3050,1,0,0:0:0:0:",
                "400,400,4050,1,0,0:0:0:0:",
            });

            var beatmap = new Beatmap(newOsu, "", "test.osu");
            var creationDate = DateTime.UtcNow;
            SetupMockSnapshot(oldOsu, creationDate);
            beatmap.MetadataSettings.beatmapSetId = 123;
            beatmap.MetadataSettings.beatmapId = 456;

            var diffs = new List<DiffInstance>();
            foreach (var ho in new[] { "100,100,1050,1,0,0:0:0:0:", "200,200,2050,1,0,0:0:0:0:", "300,300,3050,1,0,0:0:0:0:", "400,400,4050,1,0,0:0:0:0:" })
                diffs.Add(new DiffInstance(ho, "HitObjects", Snapshotter.DiffType.Added, new List<string>(), creationDate));
            foreach (var ho in new[] { "100,100,1000,1,0,0:0:0:0:", "200,200,2000,1,0,0:0:0:0:", "300,300,3000,1,0,0:0:0:0:", "400,400,4000,1,0,0:0:0:0:" })
                diffs.Add(new DiffInstance(ho, "HitObjects", Snapshotter.DiffType.Removed, new List<string>(), creationDate));

            var result = new HitObjectsTranslator().Translate(diffs, beatmap).ToList();
            // 4 < 5 inliers required, so no section-shift entry. Each object gets its own.
            Assert.DoesNotContain(result, r => r.Diff.Contains("Section shifted"));
        }

        [Fact]
        public void Translate_ManiaChordShift_ChordCountedAsOne()
        {
            // Mania chord-note dedup: 5 chord positions, each with 4 concurrent notes,
            // all shifted uniformly. Without dedup the 5x4=20 votes would trivially pass
            // the 5-inlier threshold; with dedup we only get 5 distinct time slots, which
            // is exactly the minimum — confirming chords don't multi-vote.
            string[] OldChord(int t) => new[]
            {
                $"64,192,{t},1,0,0:0:0:0:",
                $"192,192,{t},1,0,0:0:0:0:",
                $"320,192,{t},1,0,0:0:0:0:",
                $"448,192,{t},1,0,0:0:0:0:",
            };
            string[] NewChord(int t) => OldChord(t);

            var oldNotes = new List<string>();
            var newNotes = new List<string>();
            int[] times = { 1000, 2000, 3000, 4000, 5000 };
            foreach (var t in times) oldNotes.AddRange(OldChord(t));
            foreach (var t in times) newNotes.AddRange(NewChord(t + 9));

            var oldOsu = BuildManiaOsu(oldNotes.ToArray());
            var newOsu = BuildManiaOsu(newNotes.ToArray());

            var beatmap = new Beatmap(newOsu, "", "test.osu");
            var now = DateTime.UtcNow;
            // Snapshot filenames are at second precision; drop ms so the diff timestamp
            // matches the on-disk snapshot when the translator looks it up.
            var creationDate = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, now.Second, DateTimeKind.Utc);
            SetupMockSnapshot(oldOsu, creationDate);
            beatmap.MetadataSettings.beatmapSetId = 123;
            beatmap.MetadataSettings.beatmapId = 456;

            var diffs = new List<DiffInstance>();
            foreach (var n in newNotes) diffs.Add(new DiffInstance(n, "HitObjects", Snapshotter.DiffType.Added, new List<string>(), creationDate));
            foreach (var n in oldNotes) diffs.Add(new DiffInstance(n, "HitObjects", Snapshotter.DiffType.Removed, new List<string>(), creationDate));

            var result = new HitObjectsTranslator().Translate(diffs, beatmap).ToList();

            var shiftEntry = result.FirstOrDefault(r => r.Diff.Contains("Section shifted"));
            Assert.NotNull(shiftEntry);
            Assert.Contains("+9 ms", shiftEntry!.Diff);
            // 5 chord positions x 4 notes = 20 matches; the matchedCount displayed in the
            // section-shift summary counts all matched pairs (the dedup only collapses the
            // RANSAC vote, not the inlier count post-selection).
            Assert.Contains("(20 objects)", shiftEntry.Diff);
        }

        private static string BuildManiaOsu(string[] hitObjects) =>
            string.Join(
                "\n",
                new[]
                {
                    "osu file format v14",
                    "[General]",
                    "AudioFilename: audio.mp3",
                    "Mode: 3", // mania
                    "[Metadata]",
                    "Title:Title",
                    "Artist:Artist",
                    "Creator:Creator",
                    "Version:Mania",
                    "[Difficulty]",
                    "HPDrainRate:5",
                    "CircleSize:4",
                    "OverallDifficulty:5",
                    "ApproachRate:5",
                    "SliderMultiplier:1.4",
                    "SliderTickRate:1",
                    "[TimingPoints]",
                    "0,500,4,2,0,100,1,0",
                    "[HitObjects]",
                }.Concat(hitObjects)
            );

        private static void SetupMockSnapshots(string[] osuCodes, DateTime[] creationDates)
        {
            var appDataPath = Path.Combine(Path.GetTempPath(), "MapsetVerifierSnapshotsTests_HitObjects");
            Snapshotter.ConfigurePath(appDataPath, "test_folder");

            var saveDirectory = Path.Combine(appDataPath, "test_folder", "snapshots", "123", "456");
            if (Directory.Exists(appDataPath))
            {
                Directory.Delete(appDataPath, true);
            }
            Directory.CreateDirectory(saveDirectory);
            for (int i = 0; i < osuCodes.Length; i++)
            {
                var saveName = creationDates[i].ToString("yyyy-MM-dd HH-mm-ss") + ".osu";
                File.WriteAllText(Path.Combine(saveDirectory, saveName), osuCodes[i]);
            }
        }

        private static void SetupMockSnapshot(string oldOsu, DateTime creationDate)
        {
            SetupMockSnapshots(new[] { oldOsu }, new[] { creationDate });
        }

        private static string BuildOsu(string[] hitObjects) =>
            string.Join(
                "\n",
                new[]
                {
                    "osu file format v14",
                    "[General]",
                    "AudioFilename: audio.mp3",
                    "Mode: 0",
                    "[Metadata]",
                    "Title:Title",
                    "Artist:Artist",
                    "Creator:Creator",
                    "Version:Diff",
                    "[Difficulty]",
                    "HPDrainRate:5",
                    "CircleSize:4",
                    "OverallDifficulty:5",
                    "ApproachRate:5",
                    "SliderMultiplier:1.4",
                    "SliderTickRate:1",
                    "[TimingPoints]",
                    "0,500,4,2,0,100,1,0",
                    "[HitObjects]",
                }.Concat(hitObjects)
            );
    }
}
