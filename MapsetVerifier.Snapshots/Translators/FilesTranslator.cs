using System.Collections.Generic;
using System.Linq;
using MapsetVerifier.Snapshots.Objects;
using static MapsetVerifier.Snapshots.Snapshotter;

namespace MapsetVerifier.Snapshots.Translators
{
    public class FilesTranslator : DiffTranslator
    {
        public override string Section => "Files";

        public override IEnumerable<DiffInstance> Translate(IEnumerable<DiffInstance> diffs)
        {
            diffs = diffs.ToArray();
            
            var added = diffs.Where(diff => diff.DiffType == DiffType.Added).ToList();
            var removed = diffs.Where(diff => diff.DiffType == DiffType.Removed).ToList();

            foreach (var addition in added)
            {
                var setting = new Setting(addition.Diff);
                var removal = removed.FirstOrDefault(diff => new Setting(diff.Diff).key == setting.key);

                if (removal != null && removal.Diff != null)
                {
                    removed.Remove(removal);

                    yield return new DiffInstance("\"" + setting.key + "\" was modified.", Section, DiffType.Changed, new List<string>(), addition.SnapshotCreationDate);
                }
                else
                {
                    yield return new DiffInstance("\"" + setting.key + "\" was added.", Section, DiffType.Added, new List<string>(), addition.SnapshotCreationDate);
                }
            }

            foreach (var removal in removed)
            {
                var setting = new Setting(removal.Diff);

                yield return new DiffInstance("\"" + setting.key + "\" was removed.", Section, DiffType.Removed, new List<string>(), removal.SnapshotCreationDate);
            }
        }
    }
}