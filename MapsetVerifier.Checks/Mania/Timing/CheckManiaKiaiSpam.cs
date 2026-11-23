using MapsetVerifier.Checks.Utils;
using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Framework.Objects.Attributes;
using MapsetVerifier.Framework.Objects.Metadata;
using MapsetVerifier.Parser.Objects;
using MapsetVerifier.Parser.Statics;
using MapsetVerifier.RankingCriteria;
using static MapsetVerifier.Parser.Objects.Beatmap;

namespace MapsetVerifier.Checks.Mania.Timing;

[Check]
public class CheckManiaKiaiSpam : BeatmapCheck
{
    public override CheckMetadata GetMetadata() =>
        new BeatmapCheckMetadata
        {
            Modes = [Mode.Mania],
            Category = "Timing",
            Message = "Many short kiai sections.",
            Author = "Greaper",

            Documentation = new Dictionary<string, string>
            {
                {
                    "Purpose",
                    @"
                    Pointing out difficulties with 4 or more kiai sections that last for one measure or less."
                },
                {
                    "Reasoning",
                    @"
                    Kiai turning on and off all the time can be distracting and get in the way of playing.

                    A few short kiai sections to emphasise a part of the song are fine."
                },
            },
        };

    /// <summary> A little over one measure, since kiai lines are placed on whole milliseconds. </summary>
    private const double ShortMeasures = 1.05;

    private const int MinSections = 4;

    public override Dictionary<string, IssueTemplate> GetTemplates() =>
        new()
        {
            {
                "Short",
                new IssueTemplate(
                    Issue.Level.Warning,
                    "{0} First of {1} kiai sections that last for one measure or less.",
                    "timestamp -",
                    "count"
                )
                    .WithCause("4 or more kiai sections last for one measure or less.")
                    .WithRule(RC.Mania.AvoidSpammingShortKiaiTime)
            },
        };

    public override IEnumerable<Issue> GetIssues(Beatmap beatmap)
    {
        var sections = ManiaPatterns
            .KiaiSections(beatmap)
            .Where(section => section.Measures <= ShortMeasures)
            .ToList();

        if (sections.Count >= MinSections)
            yield return new Issue(
                GetTemplate("Short"),
                beatmap,
                Timestamp.Get(sections[0].Start),
                sections.Count
            );
    }
}
