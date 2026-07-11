using MapsetVerifier.Parser.Objects;
using MapsetVerifier.Snapshots.Objects;
using static MapsetVerifier.Snapshots.Snapshotter;

namespace MapsetVerifier.Snapshots.Translators
{
    public class FilesTranslator : DiffTranslator
    {
        public override string Section => "Files";

        /// <summary>
        /// Section names this translator can assign to individual diffs, based on the
        /// changed file's extension. Used by consumers to identify file-based sections.
        /// </summary>
        public static readonly string[] FileSections =
        [
            "Osu Files",
            "Audio Files",
            "Video Files",
            "Other Files",
        ];

        private static readonly string[] AudioExtensions = [".mp3", ".wav", ".ogg", ".m4a"];
        private static readonly string[] VideoExtensions =
        [
            ".mp4",
            ".avi",
            ".mov",
            ".flv",
            ".wmv",
            ".webm",
            ".mkv",
        ];

        private static string GetFileSection(string fileName)
        {
            var extension = Path.GetExtension(fileName);

            if (extension.Equals(".osu", StringComparison.OrdinalIgnoreCase))
                return "Osu Files";

            if (AudioExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
                return "Audio Files";

            if (VideoExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
                return "Video Files";

            return "Other Files";
        }

        public override IEnumerable<DiffInstance> Translate(
            IEnumerable<DiffInstance> diffs,
            Beatmap beatmap
        )
        {
            diffs = diffs.ToArray();

            var added = diffs.Where(diff => diff.DiffType == DiffType.Added).ToList();
            var removed = diffs.Where(diff => diff.DiffType == DiffType.Removed).ToList();

            foreach (var addition in added)
            {
                var setting = new Setting(addition.Diff);
                var removal = removed.FirstOrDefault(diff =>
                    new Setting(diff.Diff).key == setting.key
                );

                var fileSection = GetFileSection(setting.key);

                if (removal != null)
                {
                    removed.Remove(removal);

                    yield return new DiffInstance(
                        "\"" + setting.key + "\" was modified.",
                        fileSection,
                        DiffType.Changed,
                        new List<string>(),
                        addition.SnapshotCreationDate
                    );
                }
                else
                {
                    yield return new DiffInstance(
                        "\"" + setting.key + "\" was added.",
                        fileSection,
                        DiffType.Added,
                        new List<string>(),
                        addition.SnapshotCreationDate
                    );
                }
            }

            foreach (var removal in removed)
            {
                var setting = new Setting(removal.Diff);

                yield return new DiffInstance(
                    "\"" + setting.key + "\" was removed.",
                    GetFileSection(setting.key),
                    DiffType.Removed,
                    new List<string>(),
                    removal.SnapshotCreationDate
                );
            }
        }
    }
}
