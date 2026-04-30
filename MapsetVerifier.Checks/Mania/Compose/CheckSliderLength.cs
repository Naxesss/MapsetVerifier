using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Framework.Objects.Attributes;
using MapsetVerifier.Framework.Objects.Metadata;
using MapsetVerifier.Parser.Objects;
using MapsetVerifier.Parser.Objects.HitObjects;
using MapsetVerifier.Parser.Statics;

namespace MapsetVerifier.Checks.Mania.Compose
{
    [Check]
    public class CheckSliderLength : BeatmapCheck 
    {
        public override CheckMetadata GetMetadata() => new BeatmapCheckMetadata
        {
            Modes = [Beatmap.Mode.Mania],
            Category = "Compose",
            Message = "Too short long notes (less than 30ms).",
            Author = "RandomeLoL",

            Documentation = new Dictionary<string, string>
            {
                {
                    "Purpose",
                    @"
                    Prevent abnormally short long notes from being used. 
                    Displays a Problem for long notes held for less than 20ms, and a Warning for long notes held less than 30ms.
                    <image>
                        https://i.imgur.com/S5KYuDJ.png
                        Examples of extremely short LNs, both beyond the Warning and Problem thresholds.
                    </image>"
                },
                {
                    "Reasoning",
                    @"
                    Due to the human and hardware limitations in osu!, osu!mania long notes held for shorter than 1/12 of a beat at 180bpm (~30ms) are unreasonable to play with full accuracy."
                }
            }
        };

        public override Dictionary<string, IssueTemplate> GetTemplates()
        {
            return new Dictionary<string, IssueTemplate>
            {
                {
                "Warning",
                    new IssueTemplate(Issue.Level.Warning,
                        "{0} Long note held for only ({1} ms)",
                        "timestamp", "current length")
                    .WithCause("The long note is a bit shorter than recommended.")
                },

                {
                "Problem",
                    new IssueTemplate(Issue.Level.Problem,
                        "{0} Long note held for only ({1} ms)",
                        "timestamp", "current length")
                    .WithCause("The long note is much shorter than recommended.")
                }
            };
        }

        public override IEnumerable<Issue> GetIssues(Beatmap beatmap)
        {
            //Minimum slider length allowance in milliseconds.
            const int WarningThreshold = 30; // WARNING
            const int ProblemThreshold = 20; // PROBLEM

            foreach (var holdNote in beatmap.HitObjects.OfType<HoldNote>()) 
            {
                double LNLength = holdNote.endTime - holdNote.time;

                if (ProblemThreshold > LNLength)
                    yield return new Issue(GetTemplate("Problem"),
                        beatmap, Timestamp.Get(holdNote), LNLength);

                else if (WarningThreshold > LNLength)
                    yield return new Issue(GetTemplate("Warning"),
                        beatmap, Timestamp.Get(holdNote), LNLength);
            }
        }
    }
}
