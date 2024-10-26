using System.Collections.Generic;
using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Framework.Objects.Attributes;
using MapsetVerifier.Framework.Objects.Metadata;
using MapsetVerifier.Parser.Objects;
using MapsetVerifier.Parser.Objects.Events;
using MapsetVerifier.Parser.Statics;

namespace MapsetVerifier.Checks.AllModes.Events
{
    [Check]
    public class CheckStoryHitSounds : BeatmapSetCheck
    {
        public override CheckMetadata GetMetadata() =>
            new BeatmapCheckMetadata
            {
                Modes = new[]
                {
                    // Mania uses storyboarded hit sounding due to hit sounds playing individually for each column otherwise.
                    Beatmap.Mode.Standard,
                    Beatmap.Mode.Taiko,
                    Beatmap.Mode.Catch
                },
                Category = "Events",
                Message = "Storyboarded hit sounds.",
                Author = "Naxess",

                Documentation = new Dictionary<string, string>
                {
                    {
                        "Purpose",
                        @"
                    Preventing storyboard sounds from replacing or becoming ambigious with any beatmap hit sounds."
                    },
                    {
                        "Reasoning",
                        @"
                    Storyboarded hit sounds always play at the same time regardless of however late or early the player clicks 
                    on an object, meaning they do not provide proper active hit object feedback, unlike regular hit sounds. This 
                    contradicts the purpose of hit sounds and is likely to be confusing for players if similar samples as the 
                    hit sounds are used.
                    <note>
                        Mania is exempt from this due to multiple objects at the same point in time being possible, leading 
                        to regular hit sounding working poorly, for example amplifying the volume if concurrent objects have 
                        the same hit sounds.
                    </note>"
                    }
                }
            };

        public override Dictionary<string, IssueTemplate> GetTemplates() =>
            new()
            {
                {
                    "Storyboarded Hit Sound",
                    new IssueTemplate(Issue.Level.Warning, "{0} Storyboarded hit sound ({1}, {2}%) from {3} file.", "timestamp - ", "path", "volume", ".osu/.osb").WithCause("The .osu file or .osb file contains storyboarded hit sounds.")
                }
            };

        public override IEnumerable<Issue> GetIssues(BeatmapSet beatmapSet)
        {
            foreach (var beatmap in beatmapSet.Beatmaps)
            {
                foreach (var storyHitSound in beatmap.Samples)
                    foreach (var issue in GetStoryHitSoundIssue(beatmap, storyHitSound, ".osu"))
                        yield return issue;

                if (beatmapSet.Osb == null)
                    continue;

                foreach (var storyHitSound in beatmapSet.Osb.samples)
                    foreach (var issue in GetStoryHitSoundIssue(beatmap, storyHitSound, ".osb"))
                        yield return issue;
            }
        }

        private IEnumerable<Issue> GetStoryHitSoundIssue(Beatmap beatmap, Sample sample, string origin)
        {
            yield return new Issue(GetTemplate("Storyboarded Hit Sound"), beatmap, Timestamp.Get(sample.time), sample.path, sample.volume, origin);
        }
    }
}