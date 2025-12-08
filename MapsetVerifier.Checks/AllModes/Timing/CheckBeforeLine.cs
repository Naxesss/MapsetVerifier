using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Framework.Objects.Attributes;
using MapsetVerifier.Framework.Objects.Metadata;
using MapsetVerifier.Parser.Objects;
using MapsetVerifier.Parser.Objects.HitObjects;
using MapsetVerifier.Parser.Objects.TimingLines;
using MapsetVerifier.Parser.Statics;

namespace MapsetVerifier.Checks.AllModes.Timing
{
    [Check]
    public class CheckBeforeLine : BeatmapCheck
    {
        public override CheckMetadata GetMetadata() =>
            new BeatmapCheckMetadata
            {
                // Mania doesn't have this issue since SV affects scroll speed rather than properties of objects.
                Modes =
                [
                    Beatmap.Mode.Standard,
                    Beatmap.Mode.Taiko,
                    Beatmap.Mode.Catch
                ],

                Category = "Timing",
                Message = "Hit object is unaffected by a line very close to it.",
                Author = "Naxess",

                Documentation = new Dictionary<string, string>
                {
                    {
                        "Purpose",
                        @"
                        Preventing unintentional side effects like wrong snappings and slider velocities 
                        caused by hit objects or timing lines being slightly unsnapped."
                    },
                    {
                        "Reasoning",
                        @"
                        Sliders before a timing line (even if just by 1 ms or less), will not be affected by its slider velocity. 
                        With 1 ms unsnaps being common for objects and lines due to rounding errors when copy pasting, this in turn 
                        becomes a common issue for all game modes (excluding mania since SV works differently there).

                        > If bpm changes, this will still keep track of the effective slider velocity, thereby preventing false positives. So if it wouldn't make a difference, it's not pointed out."
                    }
                }
            };

        public override Dictionary<string, IssueTemplate> GetTemplates() =>
            new()
            {
                {
                    "Before",
                    new IssueTemplate(Issue.Level.Warning, "{0} {1} is snapped {2} ms before a line which would modify its slider velocity.", "timestamp -", "object", "unsnap")
                        .WithCause("A hit object is snapped 5 ms or less behind a timing line which would otherwise modify its slider velocity. For standard and catch this only looks at slider heads.")
                },

                {
                    "After",
                    new IssueTemplate(Issue.Level.Warning, "{0} {1} is snapped {2} ms after a line which would modify its slider velocity.", "timestamp -", "object", "unsnap")
                        .WithCause("Same as the other check, except after instead of before. Only applies to taiko.")
                }
            };

        public override IEnumerable<Issue> GetIssues(Beatmap beatmap)
        {
            foreach (var hitObject in beatmap.HitObjects)
            {
                var type = hitObject is Circle ? "Circle" :
                    hitObject is Slider ? "Slider head" :
                    hitObject is HoldNote ? "Hold note" : "Spinner";

                // SV in taiko and mania speed up all objects, whereas in catch and standard it only affects sliders.
                if (hitObject is not Slider && !IsSVAffectingAR(beatmap))
                    continue;

                foreach (var issue in GetIssue(type, hitObject.time, beatmap))
                    yield return issue;
            }
        }

        /// <summary> Returns an issue if this time is very close behind to a timing line which would modify objects. </summary>
        private IEnumerable<Issue> GetIssue(string type, double time, Beatmap beatmap)
        {
            var unsnap = beatmap.GetPracticalUnsnap(time);

            var curLine = beatmap.GetTimingLine(time);
            var nextLine = curLine?.Next(true);

            if (curLine == null || nextLine == null)
                yield break;

            var curEffectiveBPM = curLine.SvMult * beatmap.GetTimingLine<UninheritedLine>(time)!.bpm;
            var nextEffectiveBPM = nextLine.SvMult * beatmap.GetTimingLine<UninheritedLine>(nextLine.Offset)!.bpm;

            var deltaEffectiveBPM = curEffectiveBPM - nextEffectiveBPM;

            var timeDiff = nextLine.Offset - time;

            if (timeDiff is > 0 and <= 5 && Math.Abs(unsnap) <= 1 && Math.Abs(deltaEffectiveBPM) > 1)
                yield return new Issue(GetTemplate("Before"), beatmap, Timestamp.Get(time), type, $"{timeDiff:0.##}");

            // Modes where SV affects AR would be impacted even if the object was right after the line.
            if (IsSVAffectingAR(beatmap) && timeDiff is < 0 and >= -5 && Math.Abs(unsnap) <= 1 && Math.Abs(deltaEffectiveBPM) > 1)
                yield return new Issue(GetTemplate("After"), beatmap, Timestamp.Get(time), type, $"{timeDiff:0.##}");
        }

        private static bool IsSVAffectingAR(Beatmap beatmap) =>
            beatmap.GeneralSettings.mode is Beatmap.Mode.Taiko or Beatmap.Mode.Mania;
    }
}