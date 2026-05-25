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

            // We expect 2 diffs: the section shift (with the modification nested), and the trailing addition
            Assert.Equal(2, result.Count);
            
            var sectionDiff = result[0];
            Assert.Contains("Section shifted in time by +5 ms", sectionDiff.Diff);
            Assert.Single(sectionDiff.Details);
            Assert.Contains("Moved from (200; 200) to (250; 250)", sectionDiff.Details[0]);

            var addedDiff = result[1];
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

            // We expect 1 section shift diff containing the intermediate addition in its details
            Assert.Single(result);
            
            var sectionDiff = result[0];
            Assert.Contains("Section shifted in time by +5 ms", sectionDiff.Diff);
            Assert.Single(sectionDiff.Details);
            Assert.Contains("Circle was added", sectionDiff.Details[0]);
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
