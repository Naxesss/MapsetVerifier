using System;
using System.Collections.Generic;
using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Framework.Objects.Attributes;
using MapsetVerifier.Framework.Objects.Metadata;
using MapsetVerifier.Parser.Objects;
using MapsetVerifier.Parser.Statics;

namespace MapsetVerifier.Checks.AllModes.Timing
{
    [Check]
    public class CheckUnsnaps : BeatmapCheck
    {
        public override CheckMetadata GetMetadata() =>
            new BeatmapCheckMetadata
            {
                Category = "Timing",
                Message = "Unsnapped hit objects.",
                Author = "Naxess",

                Documentation = new Dictionary<string, string>
                {
                    {
                        "Purpose",
                        @"
                    Prevent hit objects from being unsnapped by more than 1 ms."
                    },
                    {
                        "Reasoning",
                        @"
                    Since gameplay is based on audio cues it wouldn't make much sense to have certain hit windows happen earlier or later, 
                    even if only by a few ms.
                    <br \><br \>
                    The only reason a 1 ms leniency exists is because the editor casts decimal times to integers rather than rounds them 
                    properly, which causes these 1 ms unsnaps occasionally when copying and pasting hit objects and timing lines. This bug 
                    happens so frequently that basically all ranked maps have multiple 1 ms unsnaps in them.
                    <div style=""margin:16px;"">
                        ""At overall difficulty 10, this would result in at most 2.4% chance of a 300 becoming a 100. It would change the 
                        ratio of 300s to 100s by 1.6%. This is acceptable, and a much larger variable is introduced by the possibility of 
                        lag, input latency and time calculation accuracy.""
                        <div style=""margin:8px 0px;"">
                            — peppy, 2009 in <a href=""https://osu.ppy.sh/community/forums/topics/17711"">Ctrl+V rounding error [Resolved]</a>
                        </div>
                    </div>"
                    }
                }
            };

        public override Dictionary<string, IssueTemplate> GetTemplates() =>
            new()
            {
                {
                    "Problem",
                    new IssueTemplate(Issue.Level.Problem, "{0} {1} unsnapped by {2} ms.", "timestamp - ", "object", "unsnap").WithCause("A hit object is snapped at least 2 ms too early or late for either of the 1/5, " + "1/7, 1/9, 1/12, or 1/16 beat snap divisors.")
                },

                {
                    "AiMod False Positive",
                    new IssueTemplate(Issue.Level.Info, "{0} {1} unsnapped by {2} ms. AiMod will claim that this is an issue, but that is a false-positive. So you can ignore that.", "timestamp - ", "object", "unsnap").WithCause("Same as the other check, but only for slider tails snapped > 1 ms and < 2 ms late.")
                },

                {
                    "Minor",
                    new IssueTemplate(Issue.Level.Minor, "{0} {1} unsnapped by {2} ms.", "timestamp - ", "object", "unsnap").WithCause("Same as the other check, but by 1 ms or more instead.")
                }
            };

        public override IEnumerable<Issue> GetIssues(Beatmap beatmap)
        {
            foreach (var hitObject in beatmap.HitObjects)
                foreach (var edgeTime in hitObject.GetEdgeTimes())
                    foreach (var issue in GetUnsnapIssue(hitObject.GetPartName(edgeTime), edgeTime, beatmap))
                        yield return issue;
        }

        /// <summary> Returns issues wherever the given time value is unsnapped. </summary>
        private IEnumerable<Issue> GetUnsnapIssue(string type, double time, Beatmap beatmap)
        {
            if (type.ToLower().Contains("spinner"))
                // Spinners do not need to be snapped.
                yield break;

            var unsnapIssue = beatmap.GetUnsnapIssue(time);
            var unsnap = beatmap.GetPracticalUnsnap(time);

            if (unsnapIssue != null)
            {
                yield return new Issue(GetTemplate("Problem"), beatmap, Timestamp.Get(time), type, $"{unsnap:0.###}");
            }

            else if (Math.Abs(unsnap) >= 1)
            {
                if (type == "Slider tail" && unsnap < -1)
                    yield return new Issue(GetTemplate("AiMod False Positive"), beatmap, Timestamp.Get(time), type, $"{unsnap:0.###}");

                yield return new Issue(GetTemplate("Minor"), beatmap, Timestamp.Get(time), type, $"{unsnap:0.###}");
            }
        }
    }
}