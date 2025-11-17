using System;
using System.Collections.Generic;
using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Framework.Objects.Attributes;
using MapsetVerifier.Framework.Objects.Metadata;
using MapsetVerifier.Parser.Objects;

namespace MapsetVerifier.Checks.AllModes.Settings
{
    [Check]
    public class CheckInconsistentSettings : BeatmapSetCheck
    {
        private static readonly Func<Beatmap, Beatmap, BeatmapSet, bool> CountdownSettingCondition = (beatmap, otherBeatmap, beatmapSet) => beatmap.GeneralSettings.mode == otherBeatmap.GeneralSettings.mode &&
                                                                                                                                            // Countdown has no effect in taiko or mania.
                                                                                                                                            beatmap.GeneralSettings.mode != Beatmap.Mode.Taiko && beatmap.GeneralSettings.mode != Beatmap.Mode.Mania;

        private static readonly Func<Beatmap, Beatmap, BeatmapSet, bool> StoryboardCondition = (beatmap, otherBeatmap, beatmapSet) => (beatmap.HasDifficultySpecificStoryboard() && otherBeatmap.HasDifficultySpecificStoryboard()) || beatmapSet.Osb != null;

        private static readonly List<InconsistencyTemplate> InconsistencyTemplates =
        [
            new("Problem", "beatmapset id", beatmap =>
                {
                    var beatmapSetId = beatmap.MetadataSettings.beatmapSetId;
                    // Beatmapset IDs are set to -1 for unsubmitted mapsets.
                    return beatmapSetId == null ? "-1" : ((ulong) beatmapSetId).ToString();
                }
            ),

            new("Warning", "countdown speed", beatmap => beatmap.GeneralSettings.countdown, (beatmap, otherBeatmap, beatmapSet) => CountdownSettingCondition(beatmap, otherBeatmap, beatmapSet) &&
                                                                                                                                   // CountdownStartBeat < 0 means no countdown.
                                                                                                                                   beatmap.GetCountdownStartBeat() >= 0 && otherBeatmap.GetCountdownStartBeat() >= 0),

            new("Warning", "countdown offset", beatmap => beatmap.GeneralSettings.countdownBeatOffset, (beatmap, otherBeatmap, beatmapSet) => CountdownSettingCondition(beatmap, otherBeatmap, beatmapSet) && beatmap.GetCountdownStartBeat() >= 0 && otherBeatmap.GetCountdownStartBeat() >= 0),
            new("Warning", "countdown", beatmap => beatmap.GeneralSettings.countdown, (beatmap, otherBeatmap, beatmapSet) => CountdownSettingCondition(beatmap, otherBeatmap, beatmapSet) &&
                                                                                                                             // One map has countdown, the other not.
                                                                                                                             beatmap.GetCountdownStartBeat() >= 0 != otherBeatmap.GetCountdownStartBeat() >= 0),

            new("Warning", "letterbox", beatmap => beatmap.GeneralSettings.letterbox, (beatmap, otherBeatmap, beatmapSet) => beatmap.GeneralSettings.mode == otherBeatmap.GeneralSettings.mode && beatmap.Breaks.Count > 0 && otherBeatmap.Breaks.Count > 0),
            new("Warning", "widescreen support", beatmap => beatmap.GeneralSettings.widescreenSupport, StoryboardCondition),
            new("Warning", "storyboard in front of combo fire", beatmap => beatmap.GeneralSettings.storyInFrontOfFire, StoryboardCondition),
            new("Warning", "usage of skin sprites in storyboard", beatmap => beatmap.GeneralSettings.useSkinSprites, StoryboardCondition),
            new("Warning", "difficulty-specific storyboard presence", beatmap => beatmap.HasDifficultySpecificStoryboard()),
            new("Warning", "epilepsy warning", beatmap => beatmap.GeneralSettings.epilepsyWarning, (beatmap, otherBeatmap, beatmapSet) => StoryboardCondition(beatmap, otherBeatmap, beatmapSet) || (beatmap.Videos.Count > 0 && otherBeatmap.Videos.Count > 0)),
            new("Warning", "audio lead-in", beatmap => beatmap.GeneralSettings.audioLeadIn),
            new("Warning", "skin preference", beatmap => beatmap.GeneralSettings.skinPreference),
            new("Minor", "slider tick rate", beatmap => beatmap.DifficultySettings.sliderTickRate, (beatmap, otherBeatmap, beatmapSet) => beatmap.GeneralSettings.mode == otherBeatmap.GeneralSettings.mode)
        ];

        public override CheckMetadata GetMetadata() =>
            new BeatmapCheckMetadata
            {
                Category = "Settings",
                Message = "Inconsistent mapset id, countdown, epilepsy warning, etc.",
                Author = "Naxess",

                Documentation = new Dictionary<string, string>
                {
                    {
                        "Purpose",
                        @"
                    Ensuring settings across difficulties in a beatmapset are consistent within game modes and where it makes sense."
                    },
                    {
                        "Reasoning",
                        @"
                    Difficulties in a beatmapset using a similar video or storyboard, would likely want to have the same 
                    epilepsy settings since they would share the same reason to have it. Same goes for countdown, letterboxing, 
                    widescreen support, audio lead-in, etc. Obviously excluding settings that don't apply.
                    <br \><br \>
                    Having difficulties all be different in terms of noticeable settings would make the set seem less coherent, 
                    but it can be acceptable if it differs thematically in a way that makes it seem intentional, without needing to 
                    specify that it is, for example one beatmap being old-style with a special skin and countdown while others are 
                    more modern and exclude this."
                    }
                }
            };

        public override Dictionary<string, IssueTemplate> GetTemplates() =>
            new()
            {
                {
                    "Problem",
                    new IssueTemplate(Issue.Level.Problem, "Inconsistent {0} \"{1}\", see {2} \"{3}\".", "setting", "value", "difficulty", "value").WithCause("The beatmapset id is inconsistent between any two difficulties in the set, regardless of mode.")
                },

                {
                    "Warning",
                    new IssueTemplate(Issue.Level.Warning, "Inconsistent {0} \"{1}\", see {2} \"{3}\".", "setting", "value", "difficulty", "value").WithCause(@"Compares settings and presence of elements within the same mode. Includes the following:
                        <ul>
                            <li>countdown speed (if there's enough time to show it, excluded for taiko/mania)</li>
                            <li>countdown offset (if there's enough time to show it, excluded for taiko/mania)</li>
                            <li>countdown presence (if there's enough time to show it, excluded for taiko/mania)</li>
                            <li>letterbox (if there are breaks)</li>
                            <li>widescreen support (if there's a storyboard)</li>
                            <li>difficulty-specific storyboard presence</li>
                            <li>epilepsy warning (if there's a storyboard or video)</li>
                            <li>audio lead-in</li>
                            <li>skin preference</li>
                            <li>storyboard in front of combo fire (if there's a storyboard)</li>
                            <li>usage of skin sprites in storyboard (if there's a storyboard)</li>
                        </ul>
                        <note>
                            Inconsistent video is already covered by another check.
                        </note>")
                },

                {
                    "Minor",
                    new IssueTemplate(Issue.Level.Minor, "Inconsistent {0} \"{1}\", see {2} \"{3}\".", "setting", "value", "difficulty", "value").WithCause("Same as the warning, but only checks for slider ticks.")
                }
            };

        public override IEnumerable<Issue> GetIssues(BeatmapSet beatmapSet)
        {
            foreach (var beatmap in beatmapSet.Beatmaps)
                foreach (var otherBeatmap in beatmapSet.Beatmaps)
                    foreach (var inconsistency in InconsistencyTemplates)
                        // `GetInconsistency` returns either 1 or 0 issues, so this becomes O(n^2*m),
                        // where n is amount of beatmaps and m is amount of inconsistencies checked.
                        foreach (var issue in GetInconsistency(beatmap, otherBeatmap, beatmapSet, inconsistency))
                            yield return issue;
        }

        private IEnumerable<Issue> GetInconsistency(Beatmap beatmap, Beatmap otherBeatmap, BeatmapSet beatmapSet, InconsistencyTemplate inconsistency)
        {
            if (inconsistency.Condition != null && !inconsistency.Condition(beatmap, otherBeatmap, beatmapSet))
                yield break;

            var value = inconsistency.Value(beatmap).ToString()!;
            var otherValue = inconsistency.Value(otherBeatmap).ToString()!;

            if (value != otherValue)
                yield return new Issue(GetTemplate(inconsistency.Template), beatmap, inconsistency.Name, value, otherBeatmap, otherValue);
        }

        private readonly struct InconsistencyTemplate(
            string template,
            string name,
            Func<Beatmap, object> value,
            Func<Beatmap, Beatmap, BeatmapSet, bool>? condition = null)
        {
            public readonly string Template = template;
            public readonly string Name = name;
            public readonly Func<Beatmap, object> Value = value;
            public readonly Func<Beatmap, Beatmap, BeatmapSet, bool>? Condition = condition;
        }
    }
}