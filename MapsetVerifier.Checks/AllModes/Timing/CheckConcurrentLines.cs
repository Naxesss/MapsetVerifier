using System.Collections.Generic;
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
    public class CheckConcurrentLines : BeatmapCheck
    {
        public override CheckMetadata GetMetadata() =>
            new BeatmapCheckMetadata
            {
                Category = "Timing",
                Message = "Concurrent or conflicting timing lines.",
                Author = "Naxess",

                Documentation = new Dictionary<string, string>
                {
                    {
                        "Purpose",
                        @"
                    Preventing issues with concurrent lines of the same type, such as them switching order when loading the beatmap.
                    <image>
                        https://i.imgur.com/whTV4aV.png
                        Two inherited lines which were originally the other way around, but swapped places when opening the beatmap again.
                    </image>"
                    },
                    {
                        "Reasoning",
                        @"
                    Depending on how the game loads the lines, they may be loaded in the wrong order causing certain effects to disappear, 
                    like the editor to not see that kiai is enabled where it is in gameplay. This coupled with the fact that future versions 
                    of the game may change how these behave make them highly unreliable.
                    <note>
                        Two lines of different types, however, work properly as inherited and uninherited lines are handeled seperately, 
                        where the inherited will always apply its effects last.
                    </note>"
                    }
                }
            };

        public override Dictionary<string, IssueTemplate> GetTemplates() =>
            new()
            {
                {
                    "Concurrent",
                    new IssueTemplate(Issue.Level.Problem, "{0} Concurrent {1} lines.", "timestamp - ", "inherited/uninherited").WithCause("Two inherited or uninherited timing lines exist at the same point in time.")
                },

                {
                    "Conflicting",
                    new IssueTemplate(Issue.Level.Minor, "{0} Conflicting line settings. Green: {1}. Red: {2}. {3}.", "timestamp - ", "green setting(s)", "red setting(s)", "precedence").WithCause("An inherited and uninherited timing line exists at the same point in time and have different settings.")
                }
            };

        public override IEnumerable<Issue> GetIssues(Beatmap beatmap)
        {
            // Since the list of timing lines is sorted by time we can just check the previous line.
            for (var i = 1; i < beatmap.TimingLines.Count; ++i)
            {
                if (!beatmap.TimingLines[i - 1].Offset.AlmostEqual(beatmap.TimingLines[i].Offset))
                    continue;

                if (beatmap.TimingLines[i - 1].Uninherited == beatmap.TimingLines[i].Uninherited)
                {
                    var inheritance = beatmap.TimingLines[i].Uninherited ? "uninherited" : "inherited";

                    yield return new Issue(GetTemplate("Concurrent"), beatmap, Timestamp.Get(beatmap.TimingLines[i].Offset), inheritance);
                }
                else if (beatmap.TimingLines[i - 1].Kiai != beatmap.TimingLines[i].Kiai || !beatmap.TimingLines[i - 1].Volume.AlmostEqual(beatmap.TimingLines[i].Volume) || beatmap.TimingLines[i - 1].Sampleset != beatmap.TimingLines[i].Sampleset || beatmap.TimingLines[i - 1].CustomIndex != beatmap.TimingLines[i].CustomIndex)
                {
                    // We've guaranteed that one line is inherited and the other is
                    // uninherited, so we can figure out both by checking one.
                    InheritedLine greenLine;
                    UninheritedLine redLine;
                    string precedence;

                    if (beatmap.TimingLines[i - 1] is InheritedLine)
                    {
                        greenLine = beatmap.TimingLines[i - 1] as InheritedLine;
                        redLine = beatmap.TimingLines[i] as UninheritedLine;
                        precedence = "Red overrides green";
                    }
                    else
                    {
                        greenLine = beatmap.TimingLines[i] as InheritedLine;
                        redLine = beatmap.TimingLines[i - 1] as UninheritedLine;
                        precedence = "Green overrides red";
                    }

                    var conflictingGreenSettings = "";
                    var conflictingRedSettings = "";

                    // As mentioned above, we know neither are null, so we do not need to check that.
                    // ReSharper disable twice PossibleNullReferenceException
                    if (greenLine.Kiai != redLine.Kiai)
                    {
                        conflictingGreenSettings += (conflictingGreenSettings.Length > 0 ? ", " : "") + (greenLine.Kiai ? "kiai" : "no kiai");

                        conflictingRedSettings += (conflictingRedSettings.Length > 0 ? ", " : "") + (redLine.Kiai ? "kiai" : "no kiai");
                    }

                    if (!greenLine.Volume.AlmostEqual(redLine.Volume))
                    {
                        conflictingGreenSettings += (conflictingGreenSettings.Length > 0 ? ", " : "") + $"{greenLine.Volume}% volume";

                        conflictingRedSettings += (conflictingRedSettings.Length > 0 ? ", " : "") + $"{redLine.Volume}% volume";
                    }

                    if (greenLine.Sampleset != redLine.Sampleset)
                    {
                        conflictingGreenSettings += (conflictingGreenSettings.Length > 0 ? ", " : "") + $"{greenLine.Sampleset} sampleset";

                        conflictingRedSettings += (conflictingRedSettings.Length > 0 ? ", " : "") + $"{redLine.Sampleset} sampleset";
                    }

                    if (greenLine.CustomIndex != redLine.CustomIndex)
                    {
                        conflictingGreenSettings += (conflictingGreenSettings.Length > 0 ? ", " : "") + $"custom {greenLine.CustomIndex}";

                        conflictingRedSettings += (conflictingRedSettings.Length > 0 ? ", " : "") + $"custom {redLine.CustomIndex}";
                    }

                    yield return new Issue(GetTemplate("Conflicting"), beatmap, Timestamp.Get(beatmap.TimingLines[i].Offset), conflictingGreenSettings, conflictingRedSettings, precedence);
                }
            }
        }
    }
}