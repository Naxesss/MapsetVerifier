using System.Globalization;
using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Framework.Objects.Attributes;
using MapsetVerifier.Framework.Objects.Metadata;
using MapsetVerifier.Parser.Objects;
using MapsetVerifier.Parser.Objects.TimingLines;
using MapsetVerifier.Parser.Statics;

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

                        Similar to metadata, timing (bpm/meter/offset of uninherited lines) should really just be global for the 
                        whole beatmapset rather than difficulty-specific."
                    },
                },
            };

        public override Dictionary<string, IssueTemplate> GetTemplates() =>
            new()
            {
                {
                    "Missing",
                    new IssueTemplate(
                        Issue.Level.Problem,
                        "{0} Missing uninherited line, see {1} ({2}, {3}).",
                        "timestamp -",
                        "difficulty",
                        "bpm",
                        "meter"
                    ).WithCause(
                        "A beatmap does not have an uninherited line which the reference beatmap does, or visa versa."
                    )
                },
                {
                    "Missing Minor",
                    new IssueTemplate(
                        Issue.Level.Minor,
                        "{0} Has a decimally different offset ({1}), see {2} ({3}).",
                        "timestamp -",
                        "offset",
                        "difficulty",
                        "reference offset"
                    ).WithCause(
                        "Same as the first check, except includes issues caused by decimal unsnaps of uninherited lines."
                    )
                },
                {
                    "Inconsistent Meter",
                    new IssueTemplate(
                        Issue.Level.Problem,
                        "{0} Inconsistent meter signature ({1}), see {2} ({3}).",
                        "timestamp -",
                        "meter",
                        "difficulty",
                        "reference meter"
                    ).WithCause(
                        "The meter signature of an uninherited timing line is different from the reference beatmap."
                    )
                },
                {
                    "Inconsistent BPM",
                    new IssueTemplate(
                        Issue.Level.Problem,
                        "{0} Inconsistent BPM ({1}), see {2} ({3}).",
                        "timestamp -",
                        "bpm",
                        "difficulty",
                        "reference bpm"
                    ).WithCause("Same as the meter check, except checks BPM instead.")
                },
            };

        public override IEnumerable<Issue> GetIssues(BeatmapSet beatmapSet)
        {
            var refBeatmap = beatmapSet.Beatmaps[0];

            foreach (var beatmap in beatmapSet.Beatmaps)
            {
                foreach (var line in refBeatmap.TimingLines.OfType<UninheritedLine>())
                {
                    // Use a consistent tolerance for comparing timestamps
                    var respectiveLine = beatmap
                        .TimingLines.OfType<UninheritedLine>()
                        .FirstOrDefault(otherLine =>
                            Math.Abs(otherLine.Offset - line.Offset) < 0.001 // 1ms tolerance
                        );

                    double offset = line.Offset; // Use original offset for reporting

                    if (respectiveLine == null)
                    {
                        yield return new Issue(
                            GetTemplate("Missing"),
                            beatmap,
                            Timestamp.Get(offset),
                            refBeatmap,
                            FormatBpm(line.bpm),
                            FormatMeter(line.Meter)
                        );
                    }
                    else
                    {
                        if (line.Meter != respectiveLine.Meter)
                            yield return new Issue(
                                GetTemplate("Inconsistent Meter"),
                                beatmap,
                                Timestamp.Get(offset),
                                FormatMeter(respectiveLine.Meter),
                                refBeatmap,
                                FormatMeter(line.Meter)
                            );

                        if (Math.Abs(line.msPerBeat - respectiveLine.msPerBeat) > 0.001)
                            yield return new Issue(
                                GetTemplate("Inconsistent BPM"),
                                beatmap,
                                Timestamp.Get(offset),
                                FormatBpm(respectiveLine.bpm),
                                refBeatmap,
                                FormatBpm(line.bpm)
                            );

                        // Including decimal unsnaps - use the same tolerance approach
                        var respectiveLineExact = beatmap
                            .TimingLines.OfType<UninheritedLine>()
                            .FirstOrDefault(otherLine =>
                                Math.Abs(otherLine.Offset - line.Offset) < 0.001
                            ); // Same tolerance

                        if (respectiveLineExact == null)
                            yield return new Issue(
                                GetTemplate("Missing Minor"),
                                beatmap,
                                Timestamp.Get(offset),
                                FormatOffset(respectiveLine.Offset),
                                refBeatmap,
                                FormatOffset(line.Offset)
                            );
                    }
                }

                // Check the other way around as well, to make sure the reference map has all uninherited lines this map has.
                foreach (var line in beatmap.TimingLines.OfType<UninheritedLine>())
                {
                    var respectiveLine = refBeatmap
                        .TimingLines.OfType<UninheritedLine>()
                        .FirstOrDefault(otherLine =>
                            Math.Abs(otherLine.Offset - line.Offset) < 0.001 // Same tolerance
                        );

                    double offset = line.Offset; // Use original offset for reporting

                    if (respectiveLine == null)
                    {
                        yield return new Issue(
                            GetTemplate("Missing"),
                            refBeatmap,
                            Timestamp.Get(offset),
                            beatmap,
                            FormatBpm(line.bpm),
                            FormatMeter(line.Meter)
                        );
                    }
                    else
                    {
                        // Including decimal unsnaps - use the same tolerance approach
                        var respectiveLineExact = refBeatmap
                            .TimingLines.OfType<UninheritedLine>()
                            .FirstOrDefault(otherLine =>
                                Math.Abs(otherLine.Offset - line.Offset) < 0.001
                            ); // Same tolerance

                        if (respectiveLineExact == null)
                            yield return new Issue(
                                GetTemplate("Missing Minor"),
                                refBeatmap,
                                Timestamp.Get(offset),
                                FormatOffset(respectiveLine.Offset),
                                beatmap,
                                FormatOffset(line.Offset)
                            );
                    }
                }
            }
        }

        private static string FormatBpm(double bpm) => bpm.ToString(CultureInfo.InvariantCulture);

        private static string FormatMeter(int meter) => meter + "/4";

        private static string FormatOffset(double offset) =>
            offset.ToString(CultureInfo.InvariantCulture);
    }
}
