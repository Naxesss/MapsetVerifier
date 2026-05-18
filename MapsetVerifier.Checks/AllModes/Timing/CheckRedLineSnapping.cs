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
    public class CheckRedLineSnapping : BeatmapCheck
    {
        private const double SnappedThresholdMs = 2;

        public override CheckMetadata GetMetadata() =>
            new BeatmapCheckMetadata
            {
                Category = "Timing",
                Message = "Hit objects potentially snapped to the wrong red line.",
                Author = "Hivie",

                Documentation = new Dictionary<string, string>
                {
                    {
                        "Purpose",
                        @"
                        Detecting hit objects that appear snapped under the current uninherited timing line, but would be 
                        unsnapped relative to an upcoming misaligned red line within a short lookahead window."
                    },
                    {
                        "Reasoning",
                        @"
                        On complex or unquantized timing, new red lines may not align with previous ones. Objects resnapped 
                        against the earlier line can look correct locally while being wrong for the closer upcoming timing, 
                        which is tedious to find manually and harms playability."
                    },
                },
            };

        public override Dictionary<string, IssueTemplate> GetTemplates() =>
            new()
            {
                {
                    "Upcoming Red Line",
                    new IssueTemplate(
                        Issue.Level.Warning,
                        "{0} {1} appears snapped to the current timing but would be unsnapped by {2} ms relative to the upcoming red line at {3}.",
                        "timestamp -",
                        "object",
                        "unsnap",
                        "timestamp -"
                    ).WithCause(
                        @"A hit object edge is within max(50 ms, 1/8 beat) before a misaligned upcoming uninherited line, appears snapped on the current red line, but would be unsnapped by 2 ms or more if evaluated against the upcoming line's grid."
                    )
                },
            };

        public override IEnumerable<Issue> GetIssues(Beatmap beatmap)
        {
            foreach (var hitObject in beatmap.HitObjects)
            foreach (var edgeTime in hitObject.GetEdgeTimes())
            {
                var issue = TryGetIssue(hitObject.GetPartName(edgeTime), edgeTime, beatmap);
                if (issue != null)
                    yield return issue;
            }
        }

        private Issue? TryGetIssue(string partName, double time, Beatmap beatmap)
        {
            if (beatmap.GetUnsnapIssue(time) != null)
                return null;

            var currentLine = beatmap.GetTimingLine<UninheritedLine>(time);
            var nextLine = GetNextRedLineAfter(beatmap, time);

            if (currentLine == null || nextLine == null)
                return null;

            var lookaheadMs = GetLookaheadMs(currentLine);
            if (nextLine.Offset - time > lookaheadMs)
                return null;

            if (DownbeatsAlign(currentLine, nextLine))
                return null;

            var unsnapCurrent = Math.Abs(beatmap.GetPracticalUnsnap(time));
            var unsnapNext = Math.Abs(beatmap.GetPracticalUnsnap(time, nextLine));

            if (unsnapCurrent >= SnappedThresholdMs || unsnapNext < SnappedThresholdMs)
                return null;

            return new Issue(
                GetTemplate("Upcoming Red Line"),
                beatmap,
                Timestamp.Get(time),
                partName,
                $"{unsnapNext:0.###}",
                Timestamp.Get(nextLine.Offset)
            );
        }

        private static double GetLookaheadMs(UninheritedLine line) => Math.Max(50, line.msPerBeat / 8);

        private static UninheritedLine? GetNextRedLineAfter(Beatmap beatmap, double time)
        {
            var nextLine = beatmap.GetNextTimingLine<UninheritedLine>(time);

            if (nextLine == null || nextLine.Offset <= time)
                return null;

            return nextLine;
        }

        private static bool DownbeatsAlign(UninheritedLine line, UninheritedLine otherLine)
        {
            var negligibleDownbeatOffset = GetBeatOffset(otherLine, line, otherLine.Meter) <= 1;

            return otherLine.bpm.AlmostEqual(line.bpm)
                && otherLine.Meter == line.Meter
                && negligibleDownbeatOffset;
        }

        private static double GetBeatOffset(
            UninheritedLine line,
            UninheritedLine nextLine,
            double beatModulo
        )
        {
            var beatsIn = (nextLine.Offset - line.Offset) / line.msPerBeat;
            var offset = beatsIn % beatModulo;

            return Math.Min(Math.Abs(offset), Math.Abs(offset - beatModulo)) * line.msPerBeat;
        }
    }
}
