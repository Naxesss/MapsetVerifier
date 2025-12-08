using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Framework.Objects.Attributes;
using MapsetVerifier.Framework.Objects.Metadata;
using MapsetVerifier.Parser.Objects;
using MapsetVerifier.Parser.Objects.HitObjects;
using MapsetVerifier.Parser.Statics;

namespace MapsetVerifier.Checks.Standard.Spread
{
    [Check]
    public class CheckMultipleReverses : BeatmapCheck
    {
        public override CheckMetadata GetMetadata() =>
            new BeatmapCheckMetadata
            {
                Modes =
                [
                    Beatmap.Mode.Standard
                ],
                Difficulties =
                [
                    Beatmap.Difficulty.Easy,
                    Beatmap.Difficulty.Normal
                ],
                Category = "Spread",
                Message = "Multiple reverses on too short sliders.",
                Author = "Naxess",

                Documentation = new Dictionary<string, string>
                {
                    {
                        "Purpose",
                        @"
                        Preventing sliders from having multiple reverses in easy and normal difficulties."
                    },
                    {
                        "Reasoning",
                        @"
                        Assuming we do this on a short slider, the reverse would be visible for so short of a time that it 
                        would be difficult to react to. If we instead do this on a long slider where they can react, it's 
                        going to be really boring gameplay-wise due to how long the slider becomes. So no matter how it's 
                        done, it'll be worse than alternatives."
                    }
                }
            };

        public override Dictionary<string, IssueTemplate> GetTemplates() =>
            new()
            {
                {
                    "Problem",
                    new IssueTemplate(Issue.Level.Problem, "{0} This slider is way too short to have multiple reverses.", "timestamp -")
                        .WithCause("A slider has at least 2 reverses and is 250 ms or shorter (240 bpm 1/1) in an Easy, or 125 ms or shorter (240 bpm 1/2) in a Normal.")
                },

                {
                    "Warning",
                    new IssueTemplate(Issue.Level.Warning, "{0} This slider is very short to have multiple reverses.", "timestamp -")
                        .WithCause("A slider has at least 2 reverses and is 333 ms or shorter (180 bpm 1/1) in an Easy, or 167 ms or shorter (180 bpm 1/2) in a Normal.")
                }
            };

        public override IEnumerable<Issue> GetIssues(Beatmap beatmap)
        {
            const double problemThreshold = 60000 / 240d;
            const double warningThreshold = 60000 / 180d;

            foreach (var slider in beatmap.HitObjects.OfType<Slider>())
            {
                if (slider.EdgeAmount <= 2)
                    continue;

                // 1/1 for Easy
                var easyTemplate = slider.EndTime - slider.time < problemThreshold ? "Problem" :
                    slider.EndTime - slider.time < warningThreshold ? "Warning" : null;

                if (easyTemplate != null)
                    yield return new Issue(GetTemplate(easyTemplate), beatmap, Timestamp.Get(slider)).ForDifficulties(Beatmap.Difficulty.Easy);

                // 1/2 for Normal
                var normalTemplate = slider.EndTime - slider.time < problemThreshold * 0.5 ? "Problem" :
                    slider.EndTime - slider.time < warningThreshold * 0.5 ? "Warning" : null;

                if (normalTemplate != null)
                    yield return new Issue(GetTemplate(normalTemplate), beatmap, Timestamp.Get(slider)).ForDifficulties(Beatmap.Difficulty.Normal);
            }
        }
    }
}