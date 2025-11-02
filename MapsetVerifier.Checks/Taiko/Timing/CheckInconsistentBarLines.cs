using System.Collections.Generic;
using System.Linq;
using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Framework.Objects.Attributes;
using MapsetVerifier.Framework.Objects.Metadata;
using MapsetVerifier.Parser.Objects;
using MapsetVerifier.Parser.Objects.TimingLines;
using MapsetVerifier.Parser.Statics;

namespace MapsetVerifier.Checks.Taiko.Timing
{
    [Check]
    public class CheckInconsistentBarLines : BeatmapSetCheck
    {
        public override CheckMetadata GetMetadata() =>
            new BeatmapCheckMetadata
            {
                Modes =
                [
                    Beatmap.Mode.Taiko
                ],
                Category = "Timing",
                Message = "Inconsistent omitted bar lines.",
                Author = "Naxess",

                Documentation = new Dictionary<string, string>
                {
                    {
                        "Purpose",
                        @"
                    Ensuring that bar lines are consistent between all difficulties."
                    },
                    {
                        "Reasoning",
                        @"
                    Since all difficulties in a set are based around a single song, and bar lines are meant to act as a point of reference
                    for timing in gameplay, it would make the most sense if all difficulties would use the same bar lines. For this reason,
                    if one difficulty skips one, others probably should too."
                    }
                }
            };

        public override Dictionary<string, IssueTemplate> GetTemplates() =>
            new()
            {
                {
                    "Inconsistent",
                    new IssueTemplate(Issue.Level.Problem, "{0} Inconsistent omitted bar line, see {1}.", "timestamp - ", "difficulty").WithCause("A beatmap does not omit bar line where the reference beatmap does, or visa versa.")
                }
            };

        public override IEnumerable<Issue> GetIssues(BeatmapSet beatmapSet)
        {
            var taikoBeatmaps = beatmapSet.Beatmaps.Where(beatmap => beatmap.GeneralSettings.mode == Beatmap.Mode.Taiko).ToList();

            var refBeatmap = taikoBeatmaps.First();

            foreach (var beatmap in taikoBeatmaps)
                foreach (var line in refBeatmap.TimingLines.OfType<UninheritedLine>())
                {
                    var respectiveLine = beatmap.TimingLines.OfType<UninheritedLine>().FirstOrDefault(otherLine => Timestamp.Round(otherLine.Offset) == Timestamp.Round(line.Offset));

                    if (respectiveLine == null)
                        // Inconsistent lines, which is the responsibility of another check, so we skip this case.
                        continue;

                    double offset = Timestamp.Round(line.Offset);

                    if (line.OmitsBarLine != respectiveLine.OmitsBarLine)
                        yield return new Issue(GetTemplate("Inconsistent"), beatmap, Timestamp.Get(offset), refBeatmap);
                }
        }
    }
}