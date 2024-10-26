using System.Collections.Generic;
using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Framework.Objects.Attributes;
using MapsetVerifier.Framework.Objects.Metadata;
using MapsetVerifier.Parser.Objects;
using MapsetVerifier.Parser.Objects.HitObjects;
using MapsetVerifier.Parser.Statics;
using MathNet.Numerics;

namespace MapsetVerifier.Checks.AllModes.HitSounds
{
    [Check]
    public class CheckMuted : BeatmapCheck
    {
        public override CheckMetadata GetMetadata() =>
            new BeatmapCheckMetadata
            {
                Category = "Hit Sounds",
                Message = "Low volume hit sounding.",
                Author = "Naxess",

                Documentation = new Dictionary<string, string>
                {
                    {
                        "Purpose",
                        @"
                    Ensuring that active hit object feedback is audible."
                    },
                    {
                        "Reasoning",
                        @"
                    All active hit objects (i.e. circles, slider heads, and starts of hold notes) should provide some feedback 
                    so that players can hear if they're clicking too early or late. By reducing the volume to the point where 
                    it is difficult to hear over the song, hit sounds cease to function as proper feedback.

                    Reverses are generally always done on sound cues, and assuming that's the case, it wouldn't make much sense 
                    being silent."
                    }
                }
            };

        public override Dictionary<string, IssueTemplate> GetTemplates() =>
            new()
            {
                {
                    "Warning Volume",
                    new IssueTemplate(Issue.Level.Warning, "{0} {1}% volume {2}, this may be hard to hear over the song.", "timestamp - ", "percent", "active hit object").WithCause("An active hit object is at 10% or lower volume.")
                },

                {
                    "Minor Volume",
                    new IssueTemplate(Issue.Level.Minor, "{0} {1}% volume {2}, this may be hard to hear over the song.", "timestamp - ", "percent", "active hit object").WithCause("An active hit object is at 20% or lower volume.")
                },

                {
                    "Passive Reverse",
                    new IssueTemplate(Issue.Level.Warning, "{0} {1}% volume {2}, ensure there is no distinct sound here in the song.", "timestamp - ", "percent", "reverse").WithCause("A slider reverse is at 10% or lower volume.")
                },

                {
                    "Passive",
                    new IssueTemplate(Issue.Level.Minor, "{0} {1}% volume {2}, ensure there is no distinct sound here in the song.", "timestamp - ", "percent", "tick/tail").WithCause("A passive hit object is at 10% or lower volume.")
                }
            };

        public override IEnumerable<Issue> GetIssues(Beatmap beatmap)
        {
            var lineIndex = 0;

            foreach (var hitObject in beatmap.HitObjects)
            {
                if (!(hitObject is Circle || hitObject is Slider || hitObject is HoldNote))
                    continue;

                // Object-specific volume overrides line-specific volume for circles and hold notes
                // (feature for Mania hit sounding) when it is > 0. However, this applies to other modes as well.
                var volume = hitObject is not Slider && hitObject.volume > 0 && hitObject.volume != null ? hitObject.volume.GetValueOrDefault() : GetTimingLine(beatmap, ref lineIndex, hitObject.time).Volume;

                foreach (var issue in GetIssue(hitObject, hitObject.time, volume, true))
                    yield return issue;

                if (hitObject is not Slider slider)
                    continue;

                for (var edgeIndex = 1; edgeIndex <= slider.EdgeAmount; ++edgeIndex)
                {
                    double time = Timestamp.Round(slider.time + slider.GetCurveDuration() * edgeIndex);
                    var isReverse = edgeIndex < slider.EdgeAmount;

                    if (!isReverse)
                        // Necessary to get the exact slider end time, as opposed to a decimal value.
                        time = slider.EndTime;

                    volume = GetTimingLine(beatmap, ref lineIndex, time).Volume;

                    foreach (var issue in GetIssue(hitObject, time, volume, isReverse))
                        yield return issue;
                }

                foreach (var tickTime in slider.GetSliderTickTimes())
                {
                    volume = GetTimingLine(beatmap, ref lineIndex, tickTime).Volume;

                    foreach (var issue in GetIssue(hitObject, tickTime, volume))
                        yield return issue;
                }
            }
        }

        private IEnumerable<Issue> GetIssue(HitObject hitObject, double time, float volume, bool isActive = false)
        {
            volume = GetActualVolume(volume);

            if (volume > 20)
                // Volumes greater than 20% are usually audible.
                yield break;

            var isHead = time.AlmostEqual(hitObject.time);
            var timestamp = isHead ? Timestamp.Get(hitObject) : Timestamp.Get(time);
            var partName = hitObject.GetPartName(time).ToLower().Replace("body", "tick");

            if (isActive)
            {
                if (isHead)
                {
                    if (volume <= 10)
                        yield return new Issue(GetTemplate("Warning Volume"), hitObject.beatmap, timestamp, volume, partName);
                    else
                        yield return new Issue(GetTemplate("Minor Volume"), hitObject.beatmap, timestamp, volume, partName);
                }
                else
                {
                    // Must be a slider reverse, mappers rarely map these to nothing.
                    if (volume <= 10)
                        yield return new Issue(GetTemplate("Passive Reverse"), hitObject.beatmap, timestamp, volume, partName);
                }
            }
            else if (volume <= 10)
            {
                // Must be a slider tail or similar, these are often silenced intentionally.
                yield return new Issue(GetTemplate("Passive"), hitObject.beatmap, timestamp, volume, partName);
            }
        }

        /// <summary>
        ///     Returns the volume that can be heard in-game, given the timing line or object
        ///     code volume from the code. Volumes less than 5% are interpreted as 5%.
        /// </summary>
        /// <param name="volume"> The volume according to the object/timing line code, in percent (i.e. 20 is 20%). </param>
        private static float GetActualVolume(float volume) => volume < 5 ? 5 : volume;

        /// <summary>
        ///     Gets the timing line in effect at the given time continuing at the index given.
        ///     This is more performant than <see cref="Beatmap.GetTimingLine(double,bool,bool)" /> due to not
        ///     iterating from the beginning for each hit object.
        /// </summary>
        private static TimingLine GetTimingLine(Beatmap beatmap, ref int index, double time)
        {
            var length = beatmap.TimingLines.Count;

            for (; index < length; ++index)
                // Uses the 5 ms hit sound leniency.
                if (index > 0 && beatmap.TimingLines[index].Offset >= time + 5)
                    return beatmap.TimingLines[index - 1];

            return beatmap.TimingLines[length - 1];
        }
    }
}