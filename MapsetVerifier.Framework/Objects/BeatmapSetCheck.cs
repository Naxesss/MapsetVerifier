using System.Collections.Generic;
using MapsetVerifier.Parser.Objects;

namespace MapsetVerifier.Framework.Objects
{
    public abstract class BeatmapSetCheck : Check
    {
        public abstract IEnumerable<Issue> GetIssues(BeatmapSet beatmapSet);
    }
}