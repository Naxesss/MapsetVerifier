using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Framework.Objects.Attributes;
using MapsetVerifier.Framework.Objects.Metadata;
using MapsetVerifier.Parser.Objects;
using MapsetVerifier.Parser.Objects.HitObjects;
using MapsetVerifier.Parser.Statics;

namespace MapsetVerifier.Checks.Standard.Compose
{
    [Check]
    public class CheckSliderOnlySections : BeatmapCheck
    {
        public override CheckMetadata GetMetadata() =>
            new BeatmapCheckMetadata
            {
                Modes = [Beatmap.Mode.Standard],
                Difficulties = [Beatmap.Difficulty.Easy, Beatmap.Difficulty.Normal],
                Category = "Compose",
                Message = "Slider only section.",
                Author = "Greaper",

                Documentation = new Dictionary<string, string>
                {
                    {
                        "Purpose",
                        @"
                        Avoiding slider-only sections in easy and normal difficulties."
                    },
                    {
                        "Reasoning",
                        @"
                        Having a slider-only section in lower difficulties can be tiring for new players, since
                        they have to juggle clicking and holding, as opposed to only clicking."
                    },
                },
            };

        public override Dictionary<string, IssueTemplate> GetTemplates() =>
            new()
            {
                {
                    "SliderSection",
                    new IssueTemplate(
                        Issue.Level.Minor,
                        "{0} Section is slider only ({1} objects, spanning {2} s). Ensure this includes plenty "
                            + "of time between objects.",
                        "timestamp -",
                        "num",
                        "duration"
                    ).WithCause(
                        "Slider-only sections should only exist given that there's enough time between objects "
                            + "to make sure the player has time to reset."
                    )
                },
            };

        public override IEnumerable<Issue> GetIssues(Beatmap beatmap)
        {
            var numSlidersInRow = 0;
            var lastSliderEnd = 0.0;
            var duration = 0.0;

            HitObject? firstObject = null;

            foreach (var hitObject in beatmap.HitObjects)
            {
                if (hitObject is Slider slider)
                {
                    firstObject ??= hitObject;

                    if (lastSliderEnd != 0)
                    {
                        duration += slider.time - lastSliderEnd;
                    }

                    lastSliderEnd = slider.GetEndTime();
                    duration += lastSliderEnd - slider.time;

                    ++numSlidersInRow;
                }
                else
                {
                    if (numSlidersInRow > 6 && duration > 5000 && firstObject != null)
                    {
                        yield return new Issue(
                            GetTemplate("SliderSection"),
                            beatmap,
                            Timestamp.Get(firstObject),
                            numSlidersInRow,
                            $"{(int)duration / 1000}"
                        );
                    }

                    lastSliderEnd = 0;
                    duration = 0;
                    firstObject = null;
                    numSlidersInRow = 0;
                }
            }

            if (numSlidersInRow > 6 && duration > 5000 && firstObject != null)
            {
                yield return new Issue(
                    GetTemplate("SliderSection"),
                    beatmap,
                    Timestamp.Get(firstObject),
                    numSlidersInRow,
                    $"{(int)duration / 1000}"
                );
            }
        }
    }
}
