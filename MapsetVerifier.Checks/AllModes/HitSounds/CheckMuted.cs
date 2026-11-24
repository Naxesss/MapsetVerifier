using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Framework.Objects.Attributes;
using MapsetVerifier.Framework.Objects.Metadata;
using MapsetVerifier.Parser.Objects;
using MapsetVerifier.Parser.Objects.HitObjects;
using MapsetVerifier.Parser.Statics;
using MapsetVerifier.RankingCriteria;
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
                    },
                },
            };

        public override Dictionary<string, IssueTemplate> GetTemplates() =>
            new()
            {
                {
                    "Warning Volume",
                    new IssueTemplate(
                        Issue.Level.Warning,
                        "{0} {1}% volume {2}, this may be hard to hear over the song.",
                        "timestamp -",
                        "percent",
                        "active hit object"
                    )
                        .WithCause("An active hit object is at 10% or lower volume.")
                        .WithRule(RC.General.Audio_HitsoundsAudible)
                        .WithRule(RC.Osu.ActivelyClickedPartsObjectsLeast)
                },
                {
                    "Minor Volume",
                    new IssueTemplate(
                        Issue.Level.Minor,
                        "{0} {1}% volume {2}, this may be hard to hear over the song.",
                        "timestamp -",
                        "percent",
                        "active hit object"
                    )
                        .WithCause("An active hit object is at 20% or lower volume.")
                        .WithRule(RC.General.Audio_HitsoundsAudible)
                        .WithRule(RC.Osu.ActivelyClickedPartsObjectsLeast)
                },
                {
                    "Passive Reverse",
                    new IssueTemplate(
                        Issue.Level.Warning,
                        "{0} {1}% volume {2}, ensure there is no distinct sound here in the song.",
                        "timestamp -",
                        "percent",
                        "reverse"
                    )
                        .WithCause("A slider reverse is at 10% or lower volume.")
                        .WithRule(RC.Osu.SpinnerEndsSliderEndsSlider)
                },
                {
                    "Passive",
                    new IssueTemplate(
                        Issue.Level.Minor,
                        "{0} {1}% volume {2}, ensure there is no distinct sound here in the song.",
                        "timestamp -",
                        "percent",
                        "tick/tail"
                    )
                        .WithCause("A passive hit object is at 10% or lower volume.")
                        .WithRule(RC.Osu.SpinnerEndsSliderEndsSlider)
                },
            };

        public override IEnumerable<Issue> GetIssues(Beatmap beatmap)
        {
            foreach (var hitObject in beatmap.HitObjects)
            {
                if (!(hitObject is Circle || hitObject is Slider || hitObject is HoldNote))
                    continue;

                // Object-specific volume overrides line-specific volume for circles and hold notes
                // (feature for Mania hit sounding) when it is > 0. However, this applies to other modes as well.
                var volume =
                    hitObject is not Slider && hitObject.volume > 0 && hitObject.volume != null
                        ? hitObject.volume.GetValueOrDefault()
                        : GetVolume(beatmap, hitObject.time);

                foreach (var issue in GetIssue(hitObject, hitObject.time, volume, true))
                    yield return issue;

                if (hitObject is not Slider slider)
                    continue;

                for (var edgeIndex = 1; edgeIndex <= slider.EdgeAmount; ++edgeIndex)
                {
                    double time = Timestamp.Round(
                        slider.time + slider.GetCurveDuration() * edgeIndex
                    );
                    var isReverse = edgeIndex < slider.EdgeAmount;

                    if (!isReverse)
                        // Necessary to get the exact slider end time, as opposed to a decimal value.
                        time = slider.EndTime;

                    volume = GetVolume(beatmap, time);

                    foreach (var issue in GetIssue(hitObject, time, volume, isReverse))
                        yield return issue;
                }

                foreach (var tickTime in slider.GetSliderTickTimes())
                {
                    volume = GetVolume(beatmap, tickTime, true);

                    foreach (var issue in GetIssue(hitObject, tickTime, volume))
                        yield return issue;
                }
            }
        }

        private IEnumerable<Issue> GetIssue(
            HitObject hitObject,
            double time,
            float volume,
            bool isActive = false
        )
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
                        yield return new Issue(
                            GetTemplate("Warning Volume"),
                            hitObject.beatmap,
                            timestamp,
                            volume,
                            partName
                        );
                    else
                        yield return new Issue(
                            GetTemplate("Minor Volume"),
                            hitObject.beatmap,
                            timestamp,
                            volume,
                            partName
                        );
                }
                else
                {
                    // Must be a slider reverse, mappers rarely map these to nothing.
                    if (volume <= 10)
                        yield return new Issue(
                            GetTemplate("Passive Reverse"),
                            hitObject.beatmap,
                            timestamp,
                            volume,
                            partName
                        );
                }
            }
            else if (volume <= 10)
            {
                // Must be a slider tail or similar, these are often silenced intentionally.
                yield return new Issue(
                    GetTemplate("Passive"),
                    hitObject.beatmap,
                    timestamp,
                    volume,
                    partName
                );
            }
        }

        /// <summary>
        ///     Returns the volume that can be heard in-game, given the timing line or object
        ///     code volume from the code. Volumes less than 5% are interpreted as 5%.
        /// </summary>
        /// <param name="volume"> The volume according to the object/timing line code, in percent (i.e. 20 is 20%). </param>
        private static float GetActualVolume(float volume) => volume < 5 ? 5 : volume;

        /// <summary> Returns the volume of the timing line in effect, accounting for hit sound leniency. </summary>
        private static float GetVolume(Beatmap beatmap, double time, bool isSliderTick = false) =>
            beatmap.GetTimingLine(time, true, isSliderTick)?.Volume ?? 100;
    }
}
