using System.Collections.Generic;

namespace MapsetVerifier.Snapshots.Objects
{
    public abstract class DiffTranslator
    {
        public abstract string Section { get; }
        public virtual string TranslatedSection => Section;

        public abstract IEnumerable<DiffInstance> Translate(IEnumerable<DiffInstance> diffs);
    }
}