using MapsetVerifier.Parser.Objects;
using MapsetVerifier.Snapshots;
using MapsetVerifier.Snapshots.Translators;
using MapsetVerifier.Snapshots.Objects;
using Xunit;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MapsetVerifier.Snapshots.Tests
{
    public class TimingTranslatorTests
    {
        [Fact]
        public void Translate_ExactMatch_NoChanges()
        {
            var osuCode = BuildOsu(new[]
            {
                "100,500,4,2,0,100,1,0",
                "200,-100,4,2,0,100,0,0"
            });

            var beatmap = new Beatmap(osuCode, "", "test.osu");
            var translator = new TimingTranslator();

            var result = translator.Translate(new List<DiffInstance>(), beatmap).ToList();
            Assert.Empty(result);
        }

        [Fact]
        public void Translate_SectionalShift_CleanShift_NoDetails()
        {
            var oldOsu = BuildOsu(
                new[]
                {
                    "100,500,4,2,0,100,1,0",
                    "200,-100,4,2,0,100,0,0"
                },
                new[]
                {
                    "100,100,1000,1,0,0:0:0:0:",
                    "200,200,2000,1,0,0:0:0:0:",
                    "300,300,3000,1,0,0:0:0:0:",
                    "400,400,4000,1,0,0:0:0:0:",
                    "500,500,5000,1,0,0:0:0:0:"
                }
            );

            var newOsu = BuildOsu(
                new[]
                {
                    "105,500,4,2,0,100,1,0",
                    "205,-100,4,2,0,100,0,0"
                },
                new[]
                {
                    "100,100,1005,1,0,0:0:0:0:",
                    "200,200,2005,1,0,0:0:0:0:",
                    "300,300,3005,1,0,0:0:0:0:",
                    "400,400,4005,1,0,0:0:0:0:",
                    "500,500,5005,1,0,0:0:0:0:"
                }
            );

            var beatmap = new Beatmap(newOsu, "", "test.osu");
            var now = DateTime.UtcNow;
            var creationDate = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, now.Second, DateTimeKind.Utc);

            var diffs = new List<DiffInstance>
            {
                new DiffInstance("105,500,4,2,0,100,1,0", "TimingPoints", Snapshotter.DiffType.Added, new List<string>(), creationDate),
                new DiffInstance("205,-100,4,2,0,100,0,0", "TimingPoints", Snapshotter.DiffType.Added, new List<string>(), creationDate),
                new DiffInstance("100,500,4,2,0,100,1,0", "TimingPoints", Snapshotter.DiffType.Removed, new List<string>(), creationDate),
                new DiffInstance("200,-100,4,2,0,100,0,0", "TimingPoints", Snapshotter.DiffType.Removed, new List<string>(), creationDate),
            };

            SetupMockSnapshot(oldOsu, creationDate);

            beatmap.MetadataSettings.beatmapSetId = 123;
            beatmap.MetadataSettings.beatmapId = 456;

            var translator = new TimingTranslator();
            var result = translator.Translate(diffs, beatmap).ToList();

            Assert.Single(result);
            var sectionDiff = result[0];
            Assert.Contains("Section shifted in time by +5 ms (2 timing points)", sectionDiff.Diff);
            Assert.Empty(sectionDiff.Details);
        }

        [Fact]
        public void Translate_IndividualShift_WithBpmChange()
        {
            var oldOsu = BuildOsu(new[]
            {
                "100,500,4,2,0,100,1,0"
            });

            var newOsu = BuildOsu(new[]
            {
                "105,400,4,2,0,100,1,0" // shifted by 5ms and BPM changed (since msPerBeat changed from 500 to 400)
            });

            var beatmap = new Beatmap(newOsu, "", "test.osu");
            var now = DateTime.UtcNow;
            var creationDate = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, now.Second, DateTimeKind.Utc);

            var diffs = new List<DiffInstance>
            {
                new DiffInstance("105,400,4,2,0,100,1,0", "TimingPoints", Snapshotter.DiffType.Added, new List<string>(), creationDate),
                new DiffInstance("100,500,4,2,0,100,1,0", "TimingPoints", Snapshotter.DiffType.Removed, new List<string>(), creationDate),
            };

            SetupMockSnapshot(oldOsu, creationDate);

            beatmap.MetadataSettings.beatmapSetId = 123;
            beatmap.MetadataSettings.beatmapId = 456;

            var translator = new TimingTranslator();
            var result = translator.Translate(diffs, beatmap).ToList();

            Assert.Single(result);
            var shiftDiff = result[0];
            // Since it's < 2 matched timing points, it is treated as individual shift
            Assert.Contains("Uninherited line changed", shiftDiff.Diff);
            Assert.Equal(2, shiftDiff.Details.Count);
            Assert.Contains("Time changed from 100 ms to 105 ms", shiftDiff.Details[0]);
            Assert.Contains("BPM changed from 120 to 150", shiftDiff.Details[1]);
        }

        [Fact]
        public void Translate_HistoricalShift_CleanShift_NoDetails()
        {
            var oldOsu = BuildOsu(
                new[]
                {
                    "100,500,4,2,0,100,1,0",
                    "200,-100,4,2,0,100,0,0"
                },
                new[]
                {
                    "100,100,1000,1,0,0:0:0:0:",
                    "200,200,2000,1,0,0:0:0:0:",
                    "300,300,3000,1,0,0:0:0:0:",
                    "400,400,4000,1,0,0:0:0:0:",
                    "500,500,5000,1,0,0:0:0:0:"
                }
            );

            var newOsu = BuildOsu(
                new[]
                {
                    "105,500,4,2,0,100,1,0",
                    "205,-100,4,2,0,100,0,0"
                },
                new[]
                {
                    "100,100,1005,1,0,0:0:0:0:",
                    "200,200,2005,1,0,0:0:0:0:",
                    "300,300,3005,1,0,0:0:0:0:",
                    "400,400,4005,1,0,0:0:0:0:",
                    "500,500,5005,1,0,0:0:0:0:"
                }
            );

            var currentOsu = BuildOsu(new[]
            {
                "500,500,4,2,0,100,1,0"
            });

            var beatmap = new Beatmap(currentOsu, "", "test.osu");
            var now = DateTime.UtcNow;
            var creationDate1 = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, now.Second, DateTimeKind.Utc).AddMinutes(-10);
            var creationDate2 = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, now.Second, DateTimeKind.Utc).AddMinutes(-5);

            var diffs = new List<DiffInstance>
            {
                new DiffInstance("105,500,4,2,0,100,1,0", "TimingPoints", Snapshotter.DiffType.Added, new List<string>(), creationDate1),
                new DiffInstance("205,-100,4,2,0,100,0,0", "TimingPoints", Snapshotter.DiffType.Added, new List<string>(), creationDate1),
                new DiffInstance("100,500,4,2,0,100,1,0", "TimingPoints", Snapshotter.DiffType.Removed, new List<string>(), creationDate1),
                new DiffInstance("200,-100,4,2,0,100,0,0", "TimingPoints", Snapshotter.DiffType.Removed, new List<string>(), creationDate1),
            };

            SetupMockSnapshots(new[] { oldOsu, newOsu }, new[] { creationDate1, creationDate2 });

            beatmap.MetadataSettings.beatmapSetId = 123;
            beatmap.MetadataSettings.beatmapId = 456;

            var translator = new TimingTranslator();
            var result = translator.Translate(diffs, beatmap).ToList();

            Assert.Single(result);
            var sectionDiff = result[0];
            Assert.Contains("Section shifted in time by +5 ms (2 timing points)", sectionDiff.Diff);
            Assert.Empty(sectionDiff.Details);
        }

        [Fact]
        public void Translate_LargeShift_WithAddedVolumeRampAndVolumeChanges_FlatReporting()
        {
            // Inspired by the tatoebanohanashi case: ~+2009ms global shift, new inherited
            // "volume ramp" lines without old counterparts, and volume changes on matched lines.
            var oldTimingLines = new[]
            {
                "-12,309.27835,4,2,1,90,1,0",
                "1225,-100,4,2,11,90,0,0",
                "1534,-100,4,2,1,90,0,0",
                "8570,-100,4,2,1,90,0,0",
                "11199,-100,4,2,1,90,0,0",
                "18544,-100,4,2,10,90,0,0",
            };
            // Each old line is shifted by +2009ms, volume drops from 90 -> 55, and two
            // brand-new volume-ramp inherited lines are inserted within the shift range.
            var newTimingLines = new[]
            {
                "1997,309.27835,4,2,1,55,1,0",
                "3234,-100,4,2,11,55,0,0",
                "3543,-100,4,2,1,55,0,0",
                "10579,-100,4,2,1,55,0,0",
                "13208,-100,4,2,1,55,0,0",
                "20553,-100,4,2,10,55,0,0",
                "20862,-100,4,2,10,60,0,0", // new (volume ramp)
                "21172,-100,4,2,10,65,0,0", // new (volume ramp)
            };

            var oldOsu = BuildOsu(oldTimingLines);
            var newOsu = BuildOsu(newTimingLines);

            var beatmap = new Beatmap(newOsu, "", "test.osu");
            var now = DateTime.UtcNow;
            var creationDate = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, now.Second, DateTimeKind.Utc);

            var diffs = new List<DiffInstance>();
            foreach (var line in newTimingLines)
                diffs.Add(new DiffInstance(line, "TimingPoints", Snapshotter.DiffType.Added, new List<string>(), creationDate));
            foreach (var line in oldTimingLines)
                diffs.Add(new DiffInstance(line, "TimingPoints", Snapshotter.DiffType.Removed, new List<string>(), creationDate));

            SetupMockSnapshot(oldOsu, creationDate);

            beatmap.MetadataSettings.beatmapSetId = 123;
            beatmap.MetadataSettings.beatmapId = 456;

            var translator = new TimingTranslator();
            var result = translator.Translate(diffs, beatmap).ToList();

            // Expect: one flat section-shift summary, one entry per volume change on a
            // matched line, and one Added entry per net-new line. No nested details.
            var sectionShift = result.FirstOrDefault(r => r.Diff.Contains("Section shifted in time"));
            Assert.NotNull(sectionShift);
            Assert.Contains("+2009 ms", sectionShift!.Diff);
            Assert.Empty(sectionShift.Details);

            // Every matched line should now have an entry for "Volume changed from 90 to 55"
            // since the shift is flat-reported and property changes surface separately.
            // The volume diff lives in the entry's Details now that single-change matched
            // entries are emitted with a "{line type} changed." title for clarity.
            var volumeEntries = result.Where(
                r => r.Details.Any(d => d.Contains("Volume changed from 90 to 55"))
            ).ToList();
            Assert.Equal(oldTimingLines.Length, volumeEntries.Count);

            // No drift note should appear since all matched lines are within +/-2ms of the section shift.
            Assert.DoesNotContain(result, r => r.Diff.Contains("drifts from section shift"));

            // The two net-new inherited lines should be Added entries (flat).
            var addedEntries = result.Where(r => r.DiffType == Snapshotter.DiffType.Added).ToList();
            Assert.Equal(2, addedEntries.Count);
            Assert.All(addedEntries, r => Assert.Contains("Inherited line added", r.Diff));
        }

        [Fact]
        public void Translate_NoDiffs_NoCrash()
        {
            var osuCode = BuildOsu(new[] { "0,500,4,2,0,100,1,0" });
            var beatmap = new Beatmap(osuCode, "", "test.osu");
            var translator = new TimingTranslator();

            // No diffs - translator should yield no output without crashing.
            var result = translator.Translate(new List<DiffInstance>(), beatmap).ToList();
            Assert.Empty(result);
        }

        [Fact]
        public void Translate_NoBeatmap_NoCrash()
        {
            var translator = new TimingTranslator();

            // Null beatmap - translator should bail out gracefully.
            var result = translator.Translate(new List<DiffInstance>(), null!).ToList();
            Assert.Empty(result);
        }

        [Fact]
        public void Translate_SingleTimingLine_NoSnapshot_NoCrash()
        {
            // No mock snapshot set up; translator goes through the fallback (no DTW) path.
            var osuCode = BuildOsu(new[] { "100,500,4,2,0,100,1,0" });
            var beatmap = new Beatmap(osuCode, "", "test.osu");
            var creationDate = DateTime.UtcNow;
            var diffs = new List<DiffInstance>
            {
                new DiffInstance("100,500,4,2,0,100,1,0", "TimingPoints", Snapshotter.DiffType.Added, new List<string>(), creationDate),
            };

            // Configure a fresh snapshot path with no data.
            var appDataPath = Path.Combine(Path.GetTempPath(), "MapsetVerifierSnapshotsTests_Timing_Empty");
            if (Directory.Exists(appDataPath)) Directory.Delete(appDataPath, true);
            Snapshotter.ConfigurePath(appDataPath, "test_folder");

            beatmap.MetadataSettings.beatmapSetId = 999;
            beatmap.MetadataSettings.beatmapId = 999;

            var translator = new TimingTranslator();
            var result = translator.Translate(diffs, beatmap).ToList();

            // Single added line, no DTW path - should emit one Added entry.
            Assert.Single(result);
            Assert.Equal(Snapshotter.DiffType.Added, result[0].DiffType);
        }

        [Fact]
        public void Translate_EmptyOldTimingPoints_NoCrash()
        {
            // Old snapshot has no timing points (edge of malformed file). RANSAC has no
            // matched pairs and the dominant-shift detector must degrade gracefully.
            var oldOsu = BuildOsu(System.Array.Empty<string>());
            var newOsu = BuildOsu(new[] { "100,500,4,2,0,100,1,0" });

            var beatmap = new Beatmap(newOsu, "", "test.osu");
            var creationDate = DateTime.UtcNow;
            var diffs = new List<DiffInstance>
            {
                new DiffInstance("100,500,4,2,0,100,1,0", "TimingPoints", Snapshotter.DiffType.Added, new List<string>(), creationDate),
            };

            SetupMockSnapshot(oldOsu, creationDate);
            beatmap.MetadataSettings.beatmapSetId = 123;
            beatmap.MetadataSettings.beatmapId = 456;

            var translator = new TimingTranslator();
            var result = translator.Translate(diffs, beatmap).ToList();
            // No crash; the one new line should surface as an Added entry.
            Assert.Contains(result, r => r.DiffType == Snapshotter.DiffType.Added);
        }

        [Fact]
        public void Translate_EmptyNewTimingPoints_NoCrash()
        {
            var oldOsu = BuildOsu(new[] { "100,500,4,2,0,100,1,0" });
            var newOsu = BuildOsu(System.Array.Empty<string>());

            var beatmap = new Beatmap(newOsu, "", "test.osu");
            var creationDate = DateTime.UtcNow;
            var diffs = new List<DiffInstance>
            {
                new DiffInstance("100,500,4,2,0,100,1,0", "TimingPoints", Snapshotter.DiffType.Removed, new List<string>(), creationDate),
            };

            SetupMockSnapshot(oldOsu, creationDate);
            beatmap.MetadataSettings.beatmapSetId = 123;
            beatmap.MetadataSettings.beatmapId = 456;

            var translator = new TimingTranslator();
            var result = translator.Translate(diffs, beatmap).ToList();
            // The single old line should surface as a Removed entry without crashing.
            Assert.Contains(result, r => r.DiffType == Snapshotter.DiffType.Removed);
        }

        [Fact]
        public void Translate_FewTimingLines_NoSpuriousShift()
        {
            // Two unrelated timing changes shouldn't be misinterpreted as a section shift
            // because RANSAC's MinShiftInliers requires at least 2 *matched* pairs voting
            // for the same shift.
            var oldOsu = BuildOsu(new[]
            {
                "100,500,4,2,0,100,1,0",
                "1000,-100,4,2,0,100,0,0",
            });
            var newOsu = BuildOsu(new[]
            {
                "150,500,4,2,0,100,1,0",   // shifted +50
                "1500,-100,4,2,0,100,0,0", // shifted +500 (different magnitude)
            });

            var beatmap = new Beatmap(newOsu, "", "test.osu");
            var creationDate = DateTime.UtcNow;
            SetupMockSnapshot(oldOsu, creationDate);
            beatmap.MetadataSettings.beatmapSetId = 123;
            beatmap.MetadataSettings.beatmapId = 456;

            var diffs = new List<DiffInstance>
            {
                new DiffInstance("150,500,4,2,0,100,1,0", "TimingPoints", Snapshotter.DiffType.Added, new List<string>(), creationDate),
                new DiffInstance("1500,-100,4,2,0,100,0,0", "TimingPoints", Snapshotter.DiffType.Added, new List<string>(), creationDate),
                new DiffInstance("100,500,4,2,0,100,1,0", "TimingPoints", Snapshotter.DiffType.Removed, new List<string>(), creationDate),
                new DiffInstance("1000,-100,4,2,0,100,0,0", "TimingPoints", Snapshotter.DiffType.Removed, new List<string>(), creationDate),
            };

            var result = new TimingTranslator().Translate(diffs, beatmap).ToList();
            // Two distinct shifts, no consensus -> no section-shift entry.
            Assert.DoesNotContain(result, r => r.Diff.Contains("Section shifted"));
        }

        private static void SetupMockSnapshots(string[] osuCodes, DateTime[] creationDates)
        {
            var appDataPath = Path.Combine(Path.GetTempPath(), "MapsetVerifierSnapshotsTests_Timing");
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

        private static string BuildOsu(string[] timingPoints, string[]? hitObjects = null) =>
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
                    "[TimingPoints]"
                }.Concat(timingPoints).Concat(new[] {
                    "[HitObjects]"
                }).Concat(hitObjects ?? new[] { "100,100,1000,1,0,0:0:0:0:" })
            );
    }
}
