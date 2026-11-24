using System.Globalization;
using MapsetVerifier.Checks.Utils;
using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Framework.Objects.Attributes;
using MapsetVerifier.Framework.Objects.Metadata;
using MapsetVerifier.Parser.Objects;
using MapsetVerifier.Parser.Objects.HitObjects;
using MapsetVerifier.Parser.Statics;
using MapsetVerifier.RankingCriteria;

namespace MapsetVerifier.Checks.Standard.Compose;

[Check]
public class CheckReverseArrowDirection : BeatmapCheck
{
    public override CheckMetadata GetMetadata() =>
        new BeatmapCheckMetadata
        {
            Modes = [Beatmap.Mode.Standard],
            Category = "Compose",
            Message = "Reverse arrows not following the slider path.",
            Author = "Greaper",

            Documentation = new Dictionary<string, string>
            {
                {
                    "Purpose",
                    @"
                    Preventing reverse arrows from pointing somewhere else than where the slider goes."
                },
                {
                    "Reasoning",
                    @"
                    A reverse arrow shows the player which way the slider continues. The game points it along the very last piece
                    of the slider, so a tiny hook hidden beneath the arrow is enough to turn it the wrong way, while the slider
                    looks the same otherwise.

                    The direction of the arrow is compared with where the slider comes out from beneath it, both at three quarters
                    of the circle radius and at the edge of the circle. Arrows more than 45 degrees off at both are reported.
                    Sliders that stay beneath the circle are left out, since there is no visible path to follow.

                    Arrows at most 60 degrees off are only reported as minor, as are those at most 90 degrees off on sliders so
                    short or curled up that the circles at both ends overlap. Whether those follow the path is up to judgement.

                    This is a rule for Easy, Normal and Hard, and a guideline above that."
                },
            },
        };

    public override Dictionary<string, IssueTemplate> GetTemplates() =>
        new()
        {
            {
                "Rule",
                new IssueTemplate(
                    Issue.Level.Problem,
                    "{0} Reverse arrow is {1}° off the slider path.",
                    "timestamp -",
                    "angle"
                )
                    .WithCause(
                        "A reverse arrow points clearly away from the visible slider path, in an Easy, Normal or Hard."
                    )
                    .WithRule(
                        RC.Osu.Easy_NotUseSliderReverseArrows,
                        RC.Osu.Normal_NotUseSliderReverseArrows,
                        RC.Osu.Hard_NotUseSliderReverseArrows
                    )
            },
            {
                "Guideline",
                new IssueTemplate(
                    Issue.Level.Warning,
                    "{0} Reverse arrow is {1}° off the slider path.",
                    "timestamp -",
                    "angle"
                )
                    .WithCause(
                        "Same as the first check, but for difficulties above Hard or with an unknown level."
                    )
                    .WithRule(
                        RC.Osu.Insane_AvoidSliderReverseArrowsNot,
                        RC.Osu.Expert_AvoidSliderReverseArrowsNot
                    )
            },
            {
                "Doubtful",
                new IssueTemplate(
                    Issue.Level.Minor,
                    "{0} Reverse arrow is {1}° off the slider path, ensure it's clear.",
                    "timestamp -",
                    "angle"
                )
                    .WithCause(
                        "A reverse arrow points more than 45 degrees away from the visible slider path, but it's close to that limit or the slider barely comes out from beneath the arrow."
                    )
                    .WithRule(
                        RC.Osu.Easy_NotUseSliderReverseArrows,
                        RC.Osu.Normal_NotUseSliderReverseArrows,
                        RC.Osu.Hard_NotUseSliderReverseArrows,
                        RC.Osu.Insane_AvoidSliderReverseArrowsNot,
                        RC.Osu.Expert_AvoidSliderReverseArrowsNot
                    )
            },
        };

    public override IEnumerable<Issue> GetIssues(Beatmap beatmap)
    {
        // Whatever the level turns out to be, it's at least a guideline.
        var isGuessed = beatmap.IsDifficultyGuessed();

        foreach (var slider in beatmap.HitObjects.OfType<Slider>())
        {
            if (OsuReverseArrows.GetMisdirection(slider) is not { } misdirection)
                continue;

            var angle = misdirection.Angle;

            if (!misdirection.IsClear)
            {
                yield return Report("Doubtful", slider, angle);
                continue;
            }

            if (isGuessed)
            {
                yield return Report("Guideline", slider, angle);
                continue;
            }

            yield return Report("Rule", slider, angle)
                .ForDifficulties(
                    Beatmap.Difficulty.Easy,
                    Beatmap.Difficulty.Normal,
                    Beatmap.Difficulty.Hard
                );

            yield return Report("Guideline", slider, angle)
                .ForDifficulties(
                    Beatmap.Difficulty.Insane,
                    Beatmap.Difficulty.Expert,
                    Beatmap.Difficulty.Ultra
                );
        }
    }

    private Issue Report(string template, Slider slider, double angle) =>
        new(
            GetTemplate(template),
            slider.beatmap,
            Timestamp.Get(slider),
            angle.ToString("0", CultureInfo.InvariantCulture)
        );
}
