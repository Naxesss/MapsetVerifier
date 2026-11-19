using System.Globalization;
using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Framework.Objects.Attributes;
using MapsetVerifier.Framework.Objects.Metadata;
using MapsetVerifier.Parser.Objects;
using MapsetVerifier.Parser.Objects.HitObjects;
using MapsetVerifier.Parser.Statics;
using MapsetVerifier.RankingCriteria;

namespace MapsetVerifier.Checks.Standard.Spread;

[Check]
public class CheckHighSliderVelocity : BeatmapCheck
{
    private const double MaxSliderVelocity = 1.3;

    public override CheckMetadata GetMetadata() =>
        new BeatmapCheckMetadata
        {
            Modes = [Beatmap.Mode.Standard],
            Difficulties = [Beatmap.Difficulty.Easy, Beatmap.Difficulty.Normal],
            Category = "Spread",
            Message = "Too high slider velocity.",
            Author = "Greaper",

            Documentation = new Dictionary<string, string>
            {
                {
                    "Purpose",
                    @"
                    Keeping the slider velocity of Easy and Normal difficulties at 1.3x or lower."
                },
                {
                    "Reasoning",
                    @"
                    Fast sliders are hard to follow for newer players, who are still learning to read and hold sliders.

                    The slider velocity here is the base slider velocity from the difficulty settings multiplied by the
                    green line the slider starts on, so it matches what the player sees."
                },
            },
        };

    public override Dictionary<string, IssueTemplate> GetTemplates() =>
        new()
        {
            {
                "TooHigh",
                new IssueTemplate(
                    Issue.Level.Warning,
                    "{0} Slider velocity is {1}x, higher than 1.3x.",
                    "timestamp -",
                    "slider velocity"
                )
                    .WithCause("A slider in an Easy or Normal difficulty moves faster than 1.3x.")
                    .WithRule(
                        RC.Osu.Easy_AvoidSliderVelocityAbove1,
                        RC.Osu.Normal_AvoidSliderVelocityAbove1
                    )
            },
        };

    public override IEnumerable<Issue> GetIssues(Beatmap beatmap)
    {
        var baseVelocity = beatmap.DifficultySettings.sliderMultiplier;

        // Sliders sharing a green line share a velocity, so only the first one of each line is reported.
        TimingLine? reportedLine = null;

        foreach (var slider in beatmap.HitObjects.OfType<Slider>())
        {
            var line = beatmap.GetSliderVelocityLine(slider.time);
            var lineMultiplier = Math.Clamp(line?.SvMult ?? 1, 0.1, 10);
            var velocity = Math.Round(baseVelocity * lineMultiplier, 2);

            if (velocity <= MaxSliderVelocity || (line != null && line == reportedLine))
                continue;

            reportedLine = line;

            yield return new Issue(
                GetTemplate("TooHigh"),
                beatmap,
                Timestamp.Get(slider),
                velocity.ToString("0.##", CultureInfo.InvariantCulture)
            );
        }
    }
}
