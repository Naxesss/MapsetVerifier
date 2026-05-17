using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Framework.Objects.Attributes;
using MapsetVerifier.Framework.Objects.Metadata;
using MapsetVerifier.Parser.Objects;
using MapsetVerifier.Parser.Objects.TimingLines;
using MapsetVerifier.Parser.Statics;
using MathNet.Numerics;
using static MapsetVerifier.Checks.Utils.ManiaUtils;

namespace MapsetVerifier.Checks.Mania.Timing
{
    [Check]
    public class CheckVariableBpm : BeatmapSetCheck
    {
        public override CheckMetadata GetMetadata() =>
            new BeatmapCheckMetadata
            {
                Modes = [Beatmap.Mode.Mania],
                Category = "Timing",
                Message = "Unnormalized inherited timing lines found.",
                Author = "RandomeLoL",

                Documentation = new Dictionary<string, string>
                {
                    {
                        "Purpose",
                        @"
                    Variable BPM songs usually should be normalized."
                    },
                    {
                        "Reasoning",
                        @"
                    Charts that do not exploit green lines and slider velocity changes despite having variable BPM should be normalized more often than not. This check will ensure to raise a warning if a variable BPM map is wrongly normalized, or if its normalizing green lines aren't right on top of the red lines that precede them."
                    },
                },
            };

        public override Dictionary<string, IssueTemplate> GetTemplates()
        {
            return new Dictionary<string, IssueTemplate>
            {
                {
                    "Unnormal Value Warning",
                    new IssueTemplate(
                        Issue.Level.Warning,
                        "{0} Isn't normalized. Ensure that the value makes sense.",
                        "timestamp"
                    ).WithCause("Unnormalized timing line found.")
                },
                {
                    "Normalized Value Moved Problem",
                    new IssueTemplate(
                        Issue.Level.Problem,
                        "{0} Isn't on top of the previous uninherited timing line. Its offset should be set to {1}.",
                        "timestamp",
                        "newOffset"
                    ).WithCause("Normalized timing line not on top of an uninherited timing line.")
                },
                {
                    "Green Line Not Found",
                    new IssueTemplate(
                        Issue.Level.Warning,
                        "{0} Isn't followed by any normalizing green line. An inherited timing line with a multiplier of {1} should be added on top.",
                        "timestamp",
                        "multiplier"
                    ).WithCause("Uninherited line is unnormalized.")
                },
            };
        }

        public override IEnumerable<Issue> GetIssues(BeatmapSet beatmapSet)
        {
            foreach (var beatmap in beatmapSet.Beatmaps)
            {
                if (beatmap.GeneralSettings.mode != Beatmap.Mode.Mania)
                {
                    continue;
                }
                var difficulty = beatmap.GetDifficulty();
                if (
                    beatmap == beatmapSet.Beatmaps.First()
                    && (
                        difficulty == Beatmap.Difficulty.Easy
                        | difficulty == Beatmap.Difficulty.Normal
                    )
                )
                    continue;

                var baseBpm = GetMostCommonBeatLength(beatmap); // Theoretical BPM to normalize the chart to
                var timingLineList = beatmap.TimingLines; // Caling the "timingLines" list once

                // Instanciate needed variables. These will keep track of the previous RedLine which the GreenLines will be relative to.
                double prevOffset = 0; // RedLine Timestamp
                double prevBpm = 0; // RedLine beatLength
                var firstTimingLine = false; // Control bit in charge of turning on once the first GreenLine after a RedLine is parsed
                for (var i = 0; i < timingLineList.Count - 1; i++)
                {
                    if (timingLineList[i].Uninherited == false)
                    {
                        if (prevBpm > 0)
                        {
                            var correctMultiplier = Math.Round(baseBpm / prevBpm, 2); // Theoretical correct multiplier.
                            var currentMultiplier = Math.Round(timingLineList[i].SvMult, 2); // Current multiplier being used.

                            // Check for unnormalized values
                            if (!almostEquals(currentMultiplier, correctMultiplier, 0.02))
                                yield return new Issue(
                                    GetTemplate("Unnormal Value Warning"),
                                    beatmap,
                                    Timestamp.Get(timingLineList[i].Offset)
                                );
                            // Check for normalizing GreenLines not being right on top of the previous RedLine
                            else if (
                                !firstTimingLine
                                && !timingLineList[i].Offset.AlmostEqual(prevOffset)
                                && !almostEquals(baseBpm, prevBpm, 1)
                            )
                                yield return new Issue(
                                    GetTemplate("Normalized Value Moved Problem"),
                                    beatmap,
                                    Timestamp.Get(timingLineList[i].Offset),
                                    prevOffset
                                );
                        }

                        firstTimingLine = true;
                    }
                    else
                    {
                        // Adapt variables to new RedLine.
                        var prevUninheritedLine = (UninheritedLine)timingLineList[i]; // RedLine Initialization
                        prevOffset = prevUninheritedLine.Offset;
                        prevBpm = prevUninheritedLine.bpm;

                        // Reset control bit for the first GreenLine found after a RedLine.
                        firstTimingLine = false;

                        // Check if a RedLine needs to be normalized if it doesn't have any GreenLines on it.
                        if (
                            timingLineList[i + 1] is UninheritedLine
                            && !almostEquals(baseBpm, prevUninheritedLine.bpm, 1)
                        )
                            yield return new Issue(
                                GetTemplate("Green Line Not Found"),
                                beatmap,
                                Timestamp.Get(prevOffset),
                                Math.Round(baseBpm / prevBpm, 2)
                            );
                    }
                }
            }
        }
    }
}
