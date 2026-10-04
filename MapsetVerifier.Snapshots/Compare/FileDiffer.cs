using MapsetVerifier.Snapshots.Diffing;
using MapsetVerifier.Snapshots.Store;

namespace MapsetVerifier.Snapshots.Compare
{
    /// <summary> Compares the files of two snapshots by hash, saying what kind of file changed and how its size moved. </summary>
    public static class FileDiffer
    {
        private static readonly string[] Audio = [".mp3", ".wav", ".ogg", ".m4a"];
        private static readonly string[] Video =
        [
            ".mp4",
            ".avi",
            ".mov",
            ".flv",
            ".wmv",
            ".webm",
            ".mkv",
        ];
        private static readonly string[] Images =
        [
            ".jpg",
            ".jpeg",
            ".png",
            ".gif",
            ".bmp",
            ".webp",
        ];

        public static string Category(string name)
        {
            var extension = Path.GetExtension(name).ToLowerInvariant();

            if (extension == ".osu")
                return "Difficulty";

            if (extension == ".osb")
                return "Storyboard";

            if (Audio.Contains(extension))
                return "Audio";

            if (Video.Contains(extension))
                return "Video";

            return Images.Contains(extension) ? "Image" : "Other";
        }

        public static List<FileChange> Diff(
            IReadOnlyDictionary<string, StoredFile> before,
            IReadOnlyDictionary<string, StoredFile> after
        )
        {
            var changes = new List<FileChange>();

            foreach (
                var name in before
                    .Keys.Union(after.Keys)
                    .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
            )
            {
                var had = before.TryGetValue(name, out var oldFile);
                var has = after.TryGetValue(name, out var newFile);
                var category = Category(name);

                if (had && has)
                {
                    // Difficulty files already have their own tab; only added or removed ones matter here.
                    if (oldFile!.Hash == newFile!.Hash || category == "Difficulty")
                        continue;

                    changes.Add(
                        new FileChange(
                            name,
                            category,
                            ChangeOp.Changed,
                            Size(oldFile),
                            Size(newFile)
                        )
                    );
                }
                else if (has)
                {
                    changes.Add(
                        new FileChange(name, category, ChangeOp.Added, null, Size(newFile!))
                    );
                }
                else
                {
                    changes.Add(
                        new FileChange(name, category, ChangeOp.Removed, Size(oldFile!), null)
                    );
                }
            }

            return changes;
        }

        // Migrated snapshots only kept hashes, so a size of 0 means unknown.
        private static long? Size(StoredFile file) => file.Size > 0 ? file.Size : null;
    }
}
