using MapsetVerifier.Parser.Objects;

namespace MapsetVerifier.Snapshots.Objects
{
    /// <summary>
    /// Determines how a translator's diffs should be ordered within a section.
    /// </summary>
    public enum DiffSortMode
    {
        /// <summary>
        /// Diffs aren't tied to a point in time within the beatmap, so they're grouped by
        /// change type instead (additions, then updates, then deletes).
        /// </summary>
        ChangeType,

        /// <summary>
        /// Diffs are tied to a point in time within the beatmap (e.g. hit objects, timing
        /// points) and should be ordered chronologically by that timestamp instead.
        /// </summary>
        Timestamp,
    }

    public abstract class DiffTranslator
    {
        public abstract string Section { get; }
        public virtual string TranslatedSection => Section;

        /// <summary>
        /// How diffs produced by this translator should be ordered within a section.
        /// Defaults to <see cref="DiffSortMode.ChangeType"/> since most sections have no
        /// inherent timestamp to sort by.
        /// </summary>
        public virtual DiffSortMode SortMode => DiffSortMode.ChangeType;

        public abstract IEnumerable<DiffInstance> Translate(
            IEnumerable<DiffInstance> diffs,
            Beatmap beatmap
        );
    }
}
