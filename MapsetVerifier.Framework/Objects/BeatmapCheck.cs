using System.Collections.Generic;
using MapsetVerifier.Parser.Objects;

namespace MapsetVerifier.Framework.Objects
{
    public abstract class BeatmapCheck : Check
    {
        public abstract IEnumerable<Issue> GetIssues(Beatmap beatmap);
    }
}