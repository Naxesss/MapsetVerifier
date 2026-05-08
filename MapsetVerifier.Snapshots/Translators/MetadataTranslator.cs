using MapsetVerifier.Parser.Objects;
using MapsetVerifier.Snapshots.Objects;

namespace MapsetVerifier.Snapshots.Translators
{
    public class MetadataTranslator : DiffTranslator
    {
        public override string Section => "Metadata";

        public override IEnumerable<DiffInstance> Translate(
            IEnumerable<DiffInstance> diffs,
            Beatmap beatmap
        )
        {
            foreach (var diff in Snapshotter.TranslateSettings(Section, diffs, TranslateKey))
                yield return diff;
        }

        private static string TranslateKey(string key) =>
            key == "Title" ? "Romanized title"
            : key == "TitleUnicode" ? "Unicode title"
            : key == "Artist" ? "Romanized artist"
            : key == "ArtistUnicode" ? "Unicode artist"
            : key == "Version" ? "Difficulty name"
            : key == "BeatmapID" ? "Beatmap ID"
            : key == "BeatmapSetID" ? "Beatmapset ID"
            : key;
    }
}
