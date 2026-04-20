using MapsetVerifier.Parser.Objects;
using Xunit;

namespace MapsetVerifier.Parser.Tests.Objects
{
    public class BeatmapBackgroundTests
    {
        [Fact]
        public void GetBackgroundFilePath_ReturnsExactConfiguredBackground()
        {
            using var context = BeatmapBackgroundTestContext.Create("bg.png", new[] { "bg.png" });

            Assert.Equal(Path.Combine(context.RootPath, "bg.png"), context.Beatmap.GetBackgroundFilePath());
        }

        [Fact]
        public void GetBackgroundFilePath_ResolvesExtensionlessConfiguredBackground()
        {
            using var context = BeatmapBackgroundTestContext.Create("background", new[] { "background.jpg" });

            Assert.Equal(Path.Combine(context.RootPath, "background.jpg"), context.Beatmap.GetBackgroundFilePath());
        }

        private sealed class BeatmapBackgroundTestContext : IDisposable
        {
            public string RootPath { get; }
            public Beatmap Beatmap { get; }

            private BeatmapBackgroundTestContext(string rootPath, Beatmap beatmap)
            {
                RootPath = rootPath;
                Beatmap = beatmap;
            }

            public static BeatmapBackgroundTestContext Create(string backgroundReference, string[] files)
            {
                var rootPath = Path.Combine(Path.GetTempPath(), "MapsetVerifierTests", Guid.NewGuid().ToString());
                Directory.CreateDirectory(rootPath);

                foreach (var file in files)
                    File.WriteAllText(Path.Combine(rootPath, file), string.Empty);

                var osuCode = BuildOsu(backgroundReference);
                const string mapPath = "Test.osu";
                File.WriteAllText(Path.Combine(rootPath, mapPath), osuCode);

                return new BeatmapBackgroundTestContext(rootPath, new Beatmap(osuCode, rootPath, mapPath));
            }

            public void Dispose()
            {
                if (Directory.Exists(RootPath))
                    Directory.Delete(RootPath, true);
            }

            private static string BuildOsu(string backgroundReference) => string.Join("\n", new[]
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
                "[Events]",
                $"0,0,\"{backgroundReference}\",0,0",
                "[TimingPoints]",
                "0,500,4,2,0,100,1,0",
                "[HitObjects]"
            });
        }
    }
}