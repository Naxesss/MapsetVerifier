using MapsetVerifier.Parser.Settings;
using Xunit;

namespace MapsetVerifier.Parser.Tests.Settings
{
    public class MetadataSettingsTests
    {
        [Fact]
        public void Ids_AreParsed_WhenPublished()
        {
            var settings = Create("BeatmapID:12345", "BeatmapSetID:23456");

            Assert.Equal(12345ul, settings.beatmapId);
            Assert.Equal(23456ul, settings.beatmapSetId);
        }

        [Theory]
        // Placeholders written for non-published maps, in either field.
        [InlineData("-1")]
        [InlineData("0")]
        // Broken or truncated files.
        [InlineData("")]
        [InlineData("not a number")]
        [InlineData("99999999999999999999999999")]
        [InlineData("-2")]
        public void Ids_AreNull_WhenNotAPositiveNumber(string value)
        {
            var settings = Create("BeatmapID:" + value, "BeatmapSetID:" + value);

            Assert.Null(settings.beatmapId);
            Assert.Null(settings.beatmapSetId);
        }

        [Fact]
        public void Ids_AreNull_WhenAbsent()
        {
            var settings = Create();

            Assert.Null(settings.beatmapId);
            Assert.Null(settings.beatmapSetId);
        }

        private static MetadataSettings Create(params string[] idLines)
        {
            var lines = new List<string>
            {
                "Title:Testing",
                "Artist:Testing",
                "Creator:Testing",
                "Version:Test",
                "Source:",
                "Tags:",
            };
            lines.AddRange(idLines);

            return new MetadataSettings(lines.ToArray());
        }
    }
}
