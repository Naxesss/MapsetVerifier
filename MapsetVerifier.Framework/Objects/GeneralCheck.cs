using System.Collections.Generic;
using MapsetVerifier.Parser.Objects;

namespace MapsetVerifier.Framework.Objects
{
    public abstract class GeneralCheck : Check
    {
        public abstract IEnumerable<Issue> GetIssues(BeatmapSet beatmapSet);
    }
}