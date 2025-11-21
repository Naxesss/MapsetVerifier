using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Framework.Objects.Attributes;
using MapsetVerifier.Framework.Objects.Metadata;
using MapsetVerifier.Parser.Objects;
using MapsetVerifier.Parser.Objects.HitObjects;
using MapsetVerifier.Parser.Objects.TimingLines;
using MapsetVerifier.Parser.Statics;
using MathNet.Numerics;

namespace MapsetVerifier.Checks.AllModes.Timing
{
    [Check]
    public class CheckUnusedLines : BeatmapCheck
    {
        public override CheckMetadata GetMetadata() =>
            new BeatmapCheckMetadata
            {
                Category = "Timing",
                Message = "Unused timing lines.",
                Author = "Naxess",

                Documentation = new Dictionary<string, string>
                {
                    {
                        "Purpose",
                        @"
                        Ensuring there are no unused timing lines in the beatmap."
                    },
                    {
                        "Reasoning",
                        @"
                        When placing uninherited lines on-beat with the previous uninherited line, timing may shift 1 ms forwards 
                        due to rounding errors. This means after 20 uninherited lines placed in this way, timing may shift up to 
                        20 ms at the end. They may also affect the nightcore mod and main menu pulsing depending on placement.

                        Unused inherited lines don't cause any issues and are basically equivalent to bookmarks. Unless the mapper 
                        intended to do something with them (e.g. silencing slider ends/ticks), they can be safely removed, but 
                        removal is not necessary."
                    }
                }
            };

        public override Dictionary<string, IssueTemplate> GetTemplates() =>
            new()
            {
                {
                    "Problem",
                    new IssueTemplate(Issue.Level.Problem, "{0} Uninherited line changes nothing.", "timestamp - ")
                        .WithCause("An uninherited line is placed on a multiple of 4 downbeats away from the previous uninherited line, and changes no settings.")
                },
                {
                    "Problem Can Be Replaced By Inherited",
                    new IssueTemplate(Issue.Level.Problem, "{0} Uninherited line changes nothing that can't be changed with an inherited line.", "timestamp - ")
                        .WithCause("Same as the first check, but changes volume, sampleset, or another setting that an inherited line could change instead.")
                },
                {
                    "Warning",
                    new IssueTemplate(Issue.Level.Warning, "{0} Uninherited line only {1}, ensure this makes sense, otherwise remove the line.", "timestamp - ", "something not immediately obvious")
                        .WithCause("Same as the first check, but changes something that inherited lines cannot, yet isn't immediately obvious, i.e. omitting barline, correcting an omitted barline, or nightcore cymbals.")
                },
                {
                    "Warning Can Be Replaced By Inherited",
                    new IssueTemplate(Issue.Level.Warning, "{0} Uninherited line only {1}, ensure this makes sense, otherwise replace with an inherited line", "timestamp - ", "something not immediately obvious")
                        .WithCause("Same as the second check, but changes something that inherited lines cannot, yet isn't immediately obvious, i.e. omitting barline, correcting an omitted barline, or nightcore cymbals.")
                },
                {
                    "Minor Inherited",
                    new IssueTemplate(Issue.Level.Minor, "{0} Inherited line changes {1}.", "timestamp - ", "nothing(, other than SV/sample settings, but affects nothing)")
                        .WithCause("An inherited line changes no settings.")
                }
            };

        public override IEnumerable<Issue> GetIssues(Beatmap beatmap)
        {
            foreach (var issue in GetUninheritedLineIssues(beatmap))
                yield return issue;

            foreach (var issue in GetInheritedLineIssues(beatmap))
                yield return issue;
        }

        private IEnumerable<Issue> GetUninheritedLineIssues(Beatmap beatmap)
        {
            var lines = beatmap.TimingLines.ToList();

            for (var i = 1; i < lines.Count; ++i)
            {
                if (lines[i] is not UninheritedLine currentLine)
                    continue;

                // Can't do lines[i - 1] since that could give a green line on the same offset, which we don't want.
                var previousLine = beatmap.GetTimingLine(currentLine.Offset - 1)!;
                var previousUninheritedLine = beatmap.GetTimingLine<UninheritedLine>(currentLine.Offset - 1)!;

                if (!DownbeatsAlign(currentLine, previousUninheritedLine))
                    continue;

                var omittingBarline = false;
                var correctingBarline = false;

                if (CanOmitBarLine(beatmap))
                {
                    omittingBarline = currentLine.OmitsBarLine;

                    correctingBarline = previousUninheritedLine.OmitsBarLine && !BarLinesAlign(currentLine, previousUninheritedLine);

                    // Omitting bar lines isn't commonly seen in standard, so it's likely that people will
                    // miss incorrect usages of it, hence warn if it's the only thing keeping it used.
                    if ((omittingBarline || correctingBarline) && beatmap.GeneralSettings.mode != Beatmap.Mode.Standard)
                        continue;
                }

                var changesNightCoreCymbals = !NightcoreCymbalsAlign(currentLine, previousUninheritedLine);

                var notImmediatelyObvious = new List<string>();
                if (omittingBarline) notImmediatelyObvious.Add("omits the first barline");

                if (correctingBarline)
                    notImmediatelyObvious.Add($"corrects the omitted barline at {Timestamp.Get(previousUninheritedLine.Offset)}");

                if (changesNightCoreCymbals) notImmediatelyObvious.Add("resets nightcore mod cymbals");
                var notImmediatelyObviousStr = string.Join(" and ", notImmediatelyObvious);

                if (!IsLineUsed(beatmap, currentLine, previousLine))
                {
                    if (notImmediatelyObvious.Count == 0)
                        yield return new Issue(GetTemplate("Problem"), beatmap, Timestamp.Get(currentLine.Offset));
                    else
                        yield return new Issue(GetTemplate("Warning"), beatmap, Timestamp.Get(currentLine.Offset), notImmediatelyObviousStr);
                }
                else
                {
                    if (notImmediatelyObvious.Count == 0)
                        yield return new Issue(GetTemplate("Problem Can Be Replaced By Inherited"), beatmap, Timestamp.Get(currentLine.Offset));
                    else
                        yield return new Issue(GetTemplate("Warning Can Be Replaced By Inherited"), beatmap, Timestamp.Get(currentLine.Offset), notImmediatelyObviousStr);
                }
            }
        }

        private IEnumerable<Issue> GetInheritedLineIssues(Beatmap beatmap)
        {
            var lines = beatmap.TimingLines.ToList();

            for (var i = 1; i < lines.Count; ++i)
            {
                if (lines[i] is not InheritedLine currentLine)
                    continue;

                var previousLine = lines[i - 1];

                // Since "used" only includes false positives, this will only result in false negatives,
                // hence the check will never say that a used line is unused.
                if (IsLineUsed(beatmap, currentLine, previousLine))
                    continue;

                // Avoids confusion in case the line actually does change something from the
                // previous, but just doesn't apply to anything.
                var changesDesc = "";

                if (!UsesSV(beatmap, currentLine, previousLine))
                    changesDesc += "SV";

                if (!UsesSamples(beatmap, currentLine, previousLine))
                    changesDesc += (changesDesc.Length > 0 ? " and " : "") + "sample settings";

                changesDesc += changesDesc.Length > 0 ? ", but affects nothing" : "nothing";

                yield return new Issue(GetTemplate("Minor Inherited"), beatmap, Timestamp.Get(currentLine.Offset), changesDesc);
            }
        }

        /// <summary>
        ///     Returns whether the offset aligns in such a way that one line is a multiple of 4 beats away
        ///     from the other, and the BPM and timing signature (meter) is the same.
        /// </summary>
        private static bool DownbeatsAlign(UninheritedLine line, UninheritedLine otherLine)
        {
            var negligibleDownbeatOffset = GetBeatOffset(otherLine, line, otherLine.Meter) <= 1;

            return otherLine.bpm.AlmostEqual(line.bpm) && otherLine.Meter == line.Meter && negligibleDownbeatOffset;
        }

        /// <summary>
        ///     Returns whether the bar lines from the first line align perfectly with those of the second.
        ///     Assumes the two lines have identical BPM and meter, use <see cref="DownbeatsAlign" /> for that.
        /// </summary>
        private static bool BarLinesAlign(UninheritedLine line, UninheritedLine otherLine) =>
            // Even differences in 1 ms would be visible since it'd make 2 barlines next to each other.
            GetBeatOffset(otherLine, line, otherLine.Meter) == 0;

        /// <summary>
        ///     Returns whether the offset aligns in such a way that one line is a multiple of 4 measures away
        ///     from the other (1 measure = 4 beats in 4/4 meter). This first checks that the downbeat structure is the same.
        ///     <br></br><br></br>
        ///     In the Nightcore mod, cymbals can be heard every 4 measures.
        /// </summary>
        private static bool NightcoreCymbalsAlign(UninheritedLine line, UninheritedLine otherLine) => DownbeatsAlign(line, otherLine) && GetBeatOffset(otherLine, line, 4 * otherLine.Meter) <= 1;

        /// <summary>
        ///     Returns the ms difference between two timing lines, where the timing lines reset offset every
        ///     given number of beats.
        /// </summary>
        // ReSharper disable once SuggestBaseTypeForParameter (This method only makes sense for uninherited lines.)
        private static double GetBeatOffset(UninheritedLine line, UninheritedLine nextLine, double beatModulo)
        {
            var beatsIn = (nextLine.Offset - line.Offset) / line.msPerBeat;
            var offset = beatsIn % beatModulo;

            return Math.Min(Math.Abs(offset), Math.Abs(offset - beatModulo)) * line.msPerBeat;
        }

        /// <summary> Returns whether the beatmap supports omitting bar lines. This is currently limited to taiko and mania. </summary>
        private static bool CanOmitBarLine(Beatmap beatmap) =>
            beatmap.GeneralSettings.mode == Beatmap.Mode.Standard || // Standard includes converts to taiko and
            // mania, so it would technically be used.
            beatmap.GeneralSettings.mode == Beatmap.Mode.Taiko || beatmap.GeneralSettings.mode == Beatmap.Mode.Mania;

        /// <summary>
        ///     Returns whether a line is considered used. Only partially covers uninherited lines.
        ///     <br></br><br></br>
        ///     Conditions for an inherited line being used (simplified, only false positives, e.g. hold notes/spinners) <br></br>
        ///     - Changes sampleset, custom index, or volume and there is an edge/body within the time frame <br></br>
        ///     - Changes SV and there are sliders starting within the time frame or changes SV and the mode is mania <br></br>
        ///     - Changes kiai (causes effects on screen during duration)
        /// </summary>
        private static bool IsLineUsed(Beatmap beatmap, TimingLine currentLine, TimingLine previousLine) =>
            UsesSamples(beatmap, currentLine, previousLine) || UsesSV(beatmap, currentLine, previousLine) || currentLine.Kiai != previousLine.Kiai;

        /// <summary> Returns whether this section makes use of sample changes (i.e. volume, sampleset, or custom index). </summary>
        private static bool UsesSamples(Beatmap beatmap, TimingLine currentLine, TimingLine previousLine) => SectionContainsObject<HitObject>(beatmap, currentLine) && SamplesDiffer(currentLine, previousLine);

        /// <summary> Returns whether this section changes sample settings (i.e. volume, sampleset, or custom index). </summary>
        private static bool SamplesDiffer(TimingLine currentLine, TimingLine previousLine) =>
            currentLine.Sampleset != previousLine.Sampleset || currentLine.CustomIndex != previousLine.CustomIndex || !currentLine.Volume.AlmostEqual(previousLine.Volume);

        /// <summary> Returns whether this section is affected by SV changes. </summary>
        private static bool UsesSV(Beatmap beatmap, TimingLine currentLine, TimingLine previousLine) => CanUseSV(beatmap, currentLine) && !currentLine.SvMult.AlmostEqual(previousLine.SvMult);

        /// <summary> Returns whether changes to SV for the line will be used. </summary>
        private static bool CanUseSV(Beatmap beatmap, TimingLine line) =>
            SectionContainsObject<Slider>(beatmap, line) ||
            // Taiko and mania affect approach rate through SV.
            beatmap.GeneralSettings.mode == Beatmap.Mode.Taiko || beatmap.GeneralSettings.mode == Beatmap.Mode.Mania;

        /// <summary>
        ///     Returns whether this section contains the respective hit object type.
        ///     Only counts the start of objects.
        /// </summary>
        private static bool SectionContainsObject<T>(Beatmap beatmap, TimingLine line) where T : HitObject
        {
            var nextLine = line.Next(true);

            // If this is the final timing section
            if (nextLine == null)
            {
                return beatmap.GetNextHitObject<T>(line.Offset) != null;
            }
            
            var nextSectionEnd = nextLine.Offset;
            var objectTimeBeforeEnd = beatmap.GetPrevHitObject<T>(nextSectionEnd)?.time ?? 0;

            return objectTimeBeforeEnd >= line.Offset;
        }
    }
}