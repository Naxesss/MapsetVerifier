using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Framework.Objects.Attributes;
using MapsetVerifier.Framework.Objects.Metadata;
using MapsetVerifier.Parser.Objects;
using MapsetVerifier.Parser.Objects.TimingLines;
using MapsetVerifier.Parser.Statics;

using static MapsetVerifier.Checks.Utils.ManiaUtils;

namespace MapsetVerifier.Checks.Mania.Timing
{
    [Check]
    public class CheckEasySliderVelocity : BeatmapSetCheck
    {
        public override CheckMetadata GetMetadata() => new BeatmapCheckMetadata
        {
            Modes = [Beatmap.Mode.Mania],
            Difficulties = [Beatmap.Difficulty.Easy, Beatmap.Difficulty.Normal],
            Category = "Timing",
            Message = "Abnormal Slider Velocity changes on lower difficulties found.",
            Author = "RandomeLoL",

            Documentation = new Dictionary<string, string>
            {
                {
                    "Purpose",
                    @"
                    Lower difficulties must not use inherited timing points on the lowest Easy/Normal difficulty of a set."
                },
                {
                    "Reasoning",
                    @"
                    Adding inherited timing points on the lowest Easy or Normal difficulty of a set can be disorienting for newer players not exposed to the gimmick. Therefore, inherited timing points must only be used to normalize the speed of a variable BPM song."
                }
            }
        };

        public override Dictionary<string, IssueTemplate> GetTemplates()
        {
            return new Dictionary<string, IssueTemplate>
            {
                {
                "Normalization Problem",
                    new IssueTemplate(Issue.Level.Problem,
                        "{1} {0} has an inaccurate normalized multiplier of {3}. Consider changing the multiplier to {2}.", "difficulty", "timestamp", "correctMultiplier", "currentMultiplier")
                    .WithCause("Wrongly normalized uninherited timing line.")
                }
            };
        }

        public override IEnumerable<Issue> GetIssues(BeatmapSet beatmapSet)
        {
            List<float> keymodes = [];
            foreach (var beatmap in beatmapSet.Beatmaps)
            {
                var difficulty = beatmap.GetDifficulty();

                if ((difficulty != Beatmap.Difficulty.Easy && difficulty != Beatmap.Difficulty.Normal) ||
                    keymodes.Contains(beatmap.DifficultySettings.circleSize)) continue;
                keymodes.Add(beatmap.DifficultySettings.circleSize);
                if (difficulty == Beatmap.Difficulty.Easy | difficulty == Beatmap.Difficulty.Normal)
                {
                    var baseBpm = GetMostCommonBeatLength(beatmap); // Theoretical BPM to normalize the chart to    
                    var timingLineList = beatmap.TimingLines;       // Calling the "timingLines" list once

                    // Instantiate needed variables. These will keep track of the previous RedLine which the GreenLines will be relative to.
                    double prevUninheritedBpm = 0;

                    foreach (var timingLine in timingLineList)
                    {
                        if (timingLine.Uninherited)
                        {
                            var prevUninheritedLine = (UninheritedLine) timingLine;
                            prevUninheritedBpm = prevUninheritedLine.bpm;
                        }
                        else
                        {
                            var correctMultiplier = Math.Round(baseBpm / prevUninheritedBpm, 2); // Theoretical correct multiplier.
                            var currentMultiplier = Math.Round(timingLine.SvMult, 2);            // Current multiplier being used.

                            if (!almostEquals(currentMultiplier, correctMultiplier, 0.02))
                                yield return new Issue(GetTemplate("Normalization Problem"), beatmap, beatmap.MetadataSettings.version, Timestamp.Get(timingLine.Offset), correctMultiplier, currentMultiplier);
                        }
                    }
                }
            }
        }
    }
}