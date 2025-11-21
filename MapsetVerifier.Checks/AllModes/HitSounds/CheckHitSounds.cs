using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Framework.Objects.Attributes;
using MapsetVerifier.Framework.Objects.Metadata;
using MapsetVerifier.Parser.Objects;
using MapsetVerifier.Parser.Objects.HitObjects;
using MapsetVerifier.Parser.Objects.HitObjects.Catch;
using MapsetVerifier.Parser.Statics;

namespace MapsetVerifier.Checks.AllModes.HitSounds
{
    [Check]
    public class CheckHitSounds : BeatmapCheck
    {
        public override CheckMetadata GetMetadata() =>
            new BeatmapCheckMetadata
            {
                Modes =
                [
                    // This check would take on another meaning if applied to taiko, since there you basically map with hit sounds.
                    Beatmap.Mode.Standard,
                    Beatmap.Mode.Catch
                ],
                Category = "Hit Sounds",
                Message = "Long periods without hit sounding.",
                Author = "Naxess",

                Documentation = new Dictionary<string, string>
                {
                    {
                        "Purpose",
                        @"
                        Ensuring varied feedback is used frequently throughout the map. Not too frequently, but mixing things up at least 
                        once or twice every measure is preferable. This could be with hit sounds, sampleset changes or additions."
                    },
                    {
                        "Reasoning",
                        @"
                        Accenting and complementing the song with hit sounds, either by reflecting it or adding to it, generally yields 
                        better feedback than if the same sound would be used throughout. However, the option to use the same sounds for all 
                        hit sounds and samplesets is still possible through skinning on the players' end for those who prefer the monotone 
                        approach."
                    },
                    {
                        "Exceptions",
                        @"
                        Taiko is an exception due to relying on hit sounds for gameplay mechanics. Circles are by default don, and can be 
                        turned into kat by using a clap or whistle hit sound, for example. As such applying this check to taiko would
                        make it take on a different meaning.

                        Mania sets consisting only of insane and above difficulties can omit hit sounds due to very few in the community at that level enjoying them in general.
                        See [[Proposal - mania] Guidelines allowing higher difficulties to omit hitsound additions.](https://osu.ppy.sh/community/forums/topics/996091)
                        Since we're missing mania SR support we'll simply exclude mania from this check entirely."
                    }
                }
            };

        public override Dictionary<string, IssueTemplate> GetTemplates() =>
            new()
            {
                {
                    "No Hit Sounds",
                    new IssueTemplate(Issue.Level.Problem, "This beatmap contains no hit sounds or sampleset changes.")
                        .WithCause("There are no hit sounds or sampleset changes anywhere in a difficulty.")
                },

                {
                    "Problem",
                    new IssueTemplate(Issue.Level.Problem, "{0} No hit sounds or sampleset changes from here to {1}, ({2} s).", "timestamp - ", "timestamp - ", "duration")
                        .WithCause("The hit sound score value, based on the amount of hit objects and time between two points without hit sounds " + "or sampleset changes, is way too low.")
                },

                {
                    "Warning",
                    new IssueTemplate(Issue.Level.Warning, "{0} No hit sounds or sampleset changes from here to {1}, ({2} s).", "timestamp - ", "timestamp - ", "duration")
                        .WithCause("Same as the other check, but with a threshold which is higher, but still very low.")
                }
            };

        public override IEnumerable<Issue> GetIssues(Beatmap beatmap)
        {
            var prevTime = beatmap.HitObjects.Count > 0 ? beatmap.HitObjects.First().time : 0;
            var objectsPassed = 0;
            var totalHitSounds = 0;

            HitSample.SamplesetType? prevSample = null;

            var issues = new List<Issue>();

            void ApplyFeedbackUpdate(HitObject.HitSounds hitSound, HitSample.SamplesetType sampleset, HitObject hitObject, double time)
            {
                prevSample ??= sampleset;

                if (hitSound > 0 || sampleset != prevSample || (beatmap.GeneralSettings.mode == Beatmap.Mode.Mania && (hitObject.filename ?? "") != ""))
                {
                    prevSample = sampleset;

                    ++totalHitSounds;
                    var issue = GetIssueFromUpdate(time, ref objectsPassed, ref prevTime, beatmap);

                    if (issue != null)
                        issues.Add(issue);
                }
                else
                {
                    ++objectsPassed;
                }
            }

            foreach (var hitObject in beatmap.HitObjects)
            {
                while (true)
                {
                    // Breaks and spinners don't really need to be hit sounded so we take that into account
                    // by looking for any between the current object and excluding their drain time if present.
                    var @break = beatmap.Breaks.FirstOrDefault(otherBreak => otherBreak.endTime > prevTime && otherBreak.endTime < hitObject.time);

                    var spinner = beatmap.HitObjects.OfType<Spinner>().FirstOrDefault(otherSpinner => otherSpinner.endTime > prevTime && otherSpinner.endTime < hitObject.time);

                    var excludeStart = @break?.time ?? (spinner?.time ?? -1);
                    var excludeEnd = @break?.endTime ?? (spinner?.endTime ?? -1);

                    if (@break != null && spinner != null)
                    {
                        excludeStart = @break.time > spinner.time ? @break.time : spinner.time;
                        excludeEnd = @break.endTime > spinner.endTime ? @break.endTime : spinner.endTime;
                    }
                    else if (@break == null && spinner == null)
                    {
                        break;
                    }

                    var objectBeforeExcl = beatmap.GetHitObject(excludeStart - 1);

                    if (objectBeforeExcl == null)
                    {
                        break;
                    }

                    var endTimeBeforeExcl = objectBeforeExcl switch
                    {
                        Spinner spinnerObject => spinnerObject.endTime,
                        Slider sliderObject => sliderObject.EndTime,
                        _ => objectBeforeExcl.time
                    };

                    // Between the previous object's time and the end time before the exclusion,
                    // storyboarded hit sounds should be accounted for in mania, since they need to
                    // use them as substitutes to actual hit sounding.
                    foreach (var storyIssue in GetStoryHsIssuesFromUpdates(beatmap, prevTime, endTimeBeforeExcl, ref objectsPassed, ref prevTime))
                        issues.Add(storyIssue);

                    // Exclusion happens through updating prevTime manually rather than through the update function.
                    var issue = GetIssueFromUpdate(endTimeBeforeExcl, ref objectsPassed, ref prevTime, beatmap);

                    if (issue != null)
                        issues.Add(issue);

                    var objectAfterExcl = beatmap.GetNextHitObject(excludeEnd);
                    if (objectAfterExcl == null)
                    {
                        break;
                    }
                    
                    prevTime = objectAfterExcl.time;
                }

                // Regardless of there being a spinner or break, storyboarded hit sounds should still be taken into account.
                foreach (var storyIssue in GetStoryHsIssuesFromUpdates(beatmap, prevTime, hitObject.time, ref objectsPassed, ref prevTime))
                    issues.Add(storyIssue);

                switch (hitObject)
                {
                    case Circle _:
                        ApplyFeedbackUpdate(hitObject.hitSound, hitObject.GetSampleset(), hitObject, hitObject.time);

                        break;

                    case Slider slider:
                    {
                        ApplyFeedbackUpdate(slider.StartHitSound, slider.GetStartSampleset(), slider, slider.time);

                        if (slider.ReverseHitSounds.Any())
                            for (var reverseIndex = 0; reverseIndex < slider.EdgeAmount - 1; ++reverseIndex)
                                ApplyFeedbackUpdate(slider.ReverseHitSounds.ElementAt(reverseIndex), slider.GetReverseSampleset(reverseIndex), slider, Math.Floor(slider.time + slider.GetCurveDuration() * (reverseIndex + 1)));

                        ApplyFeedbackUpdate(slider.EndHitSound, slider.GetEndSampleset(), slider, slider.EndTime);

                        break;
                    }
                }
            }

            if (totalHitSounds == 0)
                yield return new Issue(GetTemplate("No Hit Sounds"), beatmap);
            else
                foreach (var issue in issues)
                    yield return issue;
        }

        /// <summary> Returns an issue when too much time and/or too many objects were passed before this method was called again. </summary>
        private Issue? GetIssueFromUpdate(double currentTime, ref int objectsPassed, ref double previousTime, Beatmap beatmap)
        {
            var prevTime = previousTime;

            var timeDifference = currentTime - prevTime;
            double objectDifference = objectsPassed;

            objectsPassed = 0;
            previousTime = currentTime;

            // ReSharper disable once InlineTemporaryVariable (More clear what we consider our scores this way.)
            var timeScore = timeDifference;
            var objectScore = objectDifference * 200;

            // Thresholds
            const int warningTotal = 10000;

            const int warningTime = 8 * 1500; // 12 seconds (8 measures of 160 BPM, usually makes up a whole section in the song)

            const int warningObject = 2 * 200; // 2 objects (difficulty invariant, so needs to work for easy diffs too)

            const int problemTotal = 30000;

            const int problemTime = 24 * 1500; // 36 seconds (24 measures of 160 BPM, usually makes up multiple sections in the song)

            const int problemObject = 8 * 200; // 8 objects

            if (timeScore + objectScore > problemTotal && // at least this much of the combined
                timeScore > problemTime && // at least this much of the individual
                objectScore > problemObject)
                return new Issue(GetTemplate("Problem"), beatmap, Timestamp.Get(currentTime - timeDifference), Timestamp.Get(currentTime), $"{timeDifference / 1000:0.##}");

            if (timeScore + objectScore > warningTotal && timeScore > warningTime && objectScore > warningObject)
                return new Issue(GetTemplate("Warning"), beatmap, Timestamp.Get(currentTime - timeDifference), Timestamp.Get(currentTime), $"{timeDifference / 1000:0.##}");

            return null;
        }

        /// <summary>
        ///     Returns issues for every storyboarded hit sound where too much time and/or too many objects were passed since
        ///     last update.
        /// </summary>
        private List<Issue> GetStoryHsIssuesFromUpdates(Beatmap beatmap, double startTime, double endTime, ref int objectsPassed, ref double prevTime)
        {
            var issues = new List<Issue>();

            if (beatmap.GeneralSettings.mode != Beatmap.Mode.Mania)
                return issues;

            while (true)
            {
                var storyHitSound = beatmap.Samples.FirstOrDefault(hitsound => hitsound.time > startTime && hitsound.time < endTime);

                if (storyHitSound == null)
                    break;

                var maniaIssue = GetIssueFromUpdate(storyHitSound.time, ref objectsPassed, ref prevTime, beatmap);

                if (maniaIssue != null)
                    issues.Add(maniaIssue);

                startTime = prevTime;
            }

            return issues;
        }
    }
}