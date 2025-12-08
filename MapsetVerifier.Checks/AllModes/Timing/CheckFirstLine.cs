using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Framework.Objects.Attributes;
using MapsetVerifier.Framework.Objects.Metadata;
using MapsetVerifier.Parser.Objects;
using MapsetVerifier.Parser.Statics;

namespace MapsetVerifier.Checks.AllModes.Timing
{
    [Check]
    public class CheckFirstLine : BeatmapCheck
    {
        public override CheckMetadata GetMetadata() =>
            new BeatmapCheckMetadata
            {
                Category = "Timing",
                Message = "First line toggles kiai or is inherited.",
                Author = "Naxess",

                Documentation = new Dictionary<string, string>
                {
                    {
                        "Purpose",
                        @"
                        Preventing effects from happening and inherited lines before the first uninherited line."
                    },
                    {
                        "Reasoning",
                        @"
                        If you toggle kiai on the first line, then when the player starts the beatmap, kiai will instantly trigger and apply 
                        from the beginning until the next line. 
                        ![](https://i.imgur.com/9F3LoR3.png)
                        The game preventing you from enabling kiai on the first timing line.

                        If you place an inherited line before the first uninherited line, then the game will 
                        think the whole section isn't timed, causing the default bpm to be used and the inherited line to malfunction since 
                        it has nothing to inherit.

                        ![](https://i.imgur.com/yqSEObl.png)
                        The first line being inherited, as seen from the timing view."
                    }
                }
            };

        public override Dictionary<string, IssueTemplate> GetTemplates() =>
            new()
            {
                {
                    "Inherited",
                    new IssueTemplate(Issue.Level.Problem, "{0} First timing line is inherited.", "timestamp -")
                        .WithCause("The first timing line of a beatmap is inherited.")
                },

                {
                    "Toggles Kiai",
                    new IssueTemplate(Issue.Level.Problem, "{0} First timing line toggles kiai.", "timestamp -")
                        .WithCause("The first timing line of a beatmap has kiai enabled.")
                },

                {
                    "No Lines",
                    new IssueTemplate(Issue.Level.Problem, "There are no timing lines.")
                        .WithCause("A beatmap has no timing lines.")
                }
            };

        public override IEnumerable<Issue> GetIssues(Beatmap beatmap)
        {
            if (beatmap.TimingLines.Count == 0)
            {
                yield return new Issue(GetTemplate("No Lines"), beatmap);

                yield break;
            }

            var line = beatmap.TimingLines[0];

            if (!line.Uninherited)
                yield return new Issue(GetTemplate("Inherited"), beatmap, Timestamp.Get(line.Offset));
            else if (line.Kiai)
                yield return new Issue(GetTemplate("Toggles Kiai"), beatmap, Timestamp.Get(line.Offset));
        }
    }
}