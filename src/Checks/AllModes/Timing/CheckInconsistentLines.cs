using System.Collections.Generic;
using System.Linq;
using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Framework.Objects.Attributes;
using MapsetVerifier.Framework.Objects.Metadata;
using MapsetVerifier.Parser.Objects;
using MapsetVerifier.Parser.Objects.TimingLines;
using MapsetVerifier.Parser.Statics;
using MathNet.Numerics;

namespace MapsetVerifier.Checks.AllModes.Timing
{
    [Check]
    public class CheckInconsistentLines : BeatmapSetCheck
    {
        public override CheckMetadata GetMetadata() =>
            new BeatmapCheckMetadata
            {
                Category = "Timing",
                Message = "Inconsistent uninherited lines, meter signatures, or BPM.",
                Author = "Naxess",

                Documentation = new Dictionary<string, string>
                {
                    {
                        "Purpose",
                        @"
                    Ensuring that the song is timed consistently for all difficulties."
                    },
                    {
                        "Reasoning",
                        @"
                    Since all difficulties in a set are based around a single song, they should all use the same base timing, 
                    which is made from uninherited lines. Even if a line isn't used by some difficulty due to there being a 
                    break or similar, they still affect things like the main menu flashing and beats/snares/finishes in the 
                    nightcore mod.
                    <br \><br \>
                    Similar to metadata, timing (bpm/meter/offset of uninherited lines) should really just be global for the 
                    whole beatmapset rather than difficulty-specific."
                    }
                }
            };

        public override Dictionary<string, IssueTemplate> GetTemplates() =>
            new()
            {
                {
                    "Missing",
                    new IssueTemplate(Issue.Level.Problem, "{0} Missing uninherited line, see {1}.", "timestamp - ", "difficulty").WithCause("A beatmap does not have an uninherited line which the reference beatmap does, or visa versa.")
                },

                {
                    "Missing Minor",
                    new IssueTemplate(Issue.Level.Minor, "{0} Has a decimally different offset to the one in {1}.", "timestamp - ", "difficulty").WithCause("Same as the first check, except includes issues caused by decimal unsnaps of uninherited lines.")
                },

                {
                    "Inconsistent Meter",
                    new IssueTemplate(Issue.Level.Problem, "{0} Inconsistent meter signature, see {1}.", "timestamp - ", "difficulty").WithCause("The meter signature of an uninherited timing line is different from the reference beatmap.")
                },

                {
                    "Inconsistent BPM",
                    new IssueTemplate(Issue.Level.Problem, "{0} Inconsistent BPM, see {1}.", "timestamp - ", "difficulty").WithCause("Same as the meter check, except checks BPM instead.")
                }
            };

        public override IEnumerable<Issue> GetIssues(BeatmapSet beatmapSet)
        {
            var refBeatmap = beatmapSet.Beatmaps[0];

            foreach (var beatmap in beatmapSet.Beatmaps)
            {
                foreach (var line in refBeatmap.TimingLines.OfType<UninheritedLine>())
                {
                    var respectiveLine = beatmap.TimingLines.OfType<UninheritedLine>().FirstOrDefault(otherLine => Timestamp.Round(otherLine.Offset) == Timestamp.Round(line.Offset));

                    double offset = Timestamp.Round(line.Offset);

                    if (respectiveLine == null)
                    {
                        yield return new Issue(GetTemplate("Missing"), beatmap, Timestamp.Get(offset), refBeatmap);
                    }
                    else
                    {
                        if (line.Meter != respectiveLine.Meter)
                            yield return new Issue(GetTemplate("Inconsistent Meter"), beatmap, Timestamp.Get(offset), refBeatmap);

                        if (!line.msPerBeat.AlmostEqual(respectiveLine.msPerBeat))
                            yield return new Issue(GetTemplate("Inconsistent BPM"), beatmap, Timestamp.Get(offset), refBeatmap);

                        // Including decimal unsnaps
                        var respectiveLineExact = beatmap.TimingLines.OfType<UninheritedLine>().FirstOrDefault(otherLine => otherLine.Offset.AlmostEqual(line.Offset));

                        if (respectiveLineExact == null)
                            yield return new Issue(GetTemplate("Missing Minor"), beatmap, Timestamp.Get(offset), refBeatmap);
                    }
                }

                // Check the other way around as well, to make sure the reference map has all uninherited lines this map has.
                foreach (var line in beatmap.TimingLines.OfType<UninheritedLine>())
                {
                    var respectiveLine = refBeatmap.TimingLines.OfType<UninheritedLine>().FirstOrDefault(otherLine => Timestamp.Round(otherLine.Offset) == Timestamp.Round(line.Offset));

                    double offset = Timestamp.Round(line.Offset);

                    if (respectiveLine == null)
                    {
                        yield return new Issue(GetTemplate("Missing"), refBeatmap, Timestamp.Get(offset), beatmap);
                    }
                    else
                    {
                        // Including decimal unsnaps
                        var respectiveLineExact = refBeatmap.TimingLines.OfType<UninheritedLine>().FirstOrDefault(otherLine => otherLine.Offset.AlmostEqual(line.Offset));

                        if (respectiveLineExact == null)
                            yield return new Issue(GetTemplate("Missing Minor"), refBeatmap, Timestamp.Get(offset), beatmap);
                    }
                }
            }
        }
    }
}