using System;
using System.Collections.Generic;
using System.Linq;
using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Framework.Objects.Attributes;
using MapsetVerifier.Framework.Objects.Metadata;
using MapsetVerifier.Parser.Objects;
using MapsetVerifier.Parser.Statics;
using MathNet.Numerics;

namespace MapsetVerifier.Checks.AllModes.Timing
{
    [Check]
    public class CheckKiaiUnsnap : BeatmapCheck
    {
        public override CheckMetadata GetMetadata() =>
            new BeatmapCheckMetadata
            {
                Category = "Timing",
                Message = "Unsnapped kiai.",
                Author = "Naxess",

                Documentation = new Dictionary<string, string>
                {
                    {
                        "Purpose",
                        @"
                    Ensuring kiai starts on a distinct sound."
                    },
                    {
                        "Reasoning",
                        @"
                    Since kiai is visual, unlike hit sounds, it doesn't need to be as precise in timing, but kiai being 
                    notably unsnapped from any distinct sound is still probably something you'd want to fix. Taiko has stronger
                    kiai screen effects so this matters a bit more for that mode."
                    }
                }
            };

        public override Dictionary<string, IssueTemplate> GetTemplates() =>
            new()
            {
                {
                    "Warning",
                    new IssueTemplate(Issue.Level.Warning, "{0} Kiai is unsnapped by {1} ms.", "timestamp - ", "unsnap").WithCause("An inherited line with kiai enabled is unsnapped by 10 ms or more. For taiko this is 5 ms or more instead.")
                },

                {
                    "Minor",
                    new IssueTemplate(Issue.Level.Minor, "{0} Kiai is unsnapped by {1} ms.", "timestamp - ", "unsnap").WithCause("Same as the other check, but by 1 ms or more instead.")
                },

                {
                    "Minor End",
                    new IssueTemplate(Issue.Level.Minor, "{0} Kiai end is unsnapped by {1} ms.", "timestamp - ", "unsnap").WithCause("Same as the second check, except looks for where kiai ends.")
                }
            };

        public override IEnumerable<Issue> GetIssues(Beatmap beatmap)
        {
            foreach (var line in beatmap.TimingLines.Where(line => line.Kiai))
            {
                var previousLine = beatmap.GetTimingLine(line.Offset - 1);
                if (previousLine == null)
                {
                    break;
                }
                
                // If we're inside a kiai, a new line with kiai won't cause kiai to start again.
                if (previousLine.Kiai)
                    continue;

                var unsnap = beatmap.GetPracticalUnsnap(line.Offset);

                // In taiko the screen changes color more drastically, so timing is more noticable.
                var warningThreshold = beatmap.GeneralSettings.mode == Beatmap.Mode.Taiko ? 5 : 10;

                if (Math.Abs(unsnap) >= warningThreshold)
                    yield return new Issue(GetTemplate("Warning"), beatmap, Timestamp.Get(line.Offset), unsnap);

                else if (Math.Abs(unsnap) >= 1)
                    yield return new Issue(GetTemplate("Minor"), beatmap, Timestamp.Get(line.Offset), unsnap);

                // Prevents duplicate issues occuring from both red and green line on same tick picking next line.
                if (beatmap.TimingLines.Any(otherLine => otherLine.Offset.AlmostEqual(line.Offset) && !otherLine.Uninherited && line.Uninherited))
                    continue;

                var nextLine = line.Next(true);

                if (nextLine == null || nextLine.Kiai)
                    continue;

                unsnap = beatmap.GetPracticalUnsnap(nextLine.Offset);

                if (Math.Abs(unsnap) >= 1)
                    yield return new Issue(GetTemplate("Minor End"), beatmap, Timestamp.Get(nextLine.Offset), unsnap);
            }
        }
    }
}