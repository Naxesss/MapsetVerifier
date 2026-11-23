using MapsetVerifier.Checks.Utils;
using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Framework.Objects.Attributes;
using MapsetVerifier.Framework.Objects.Metadata;
using MapsetVerifier.Parser.Objects;
using MapsetVerifier.Parser.Statics;
using MapsetVerifier.RankingCriteria;

namespace MapsetVerifier.Checks.Standard.Spread;

[Check]
public class CheckStreamLength : BeatmapCheck
{
    private const int MaxStreamNotes = 5;

    public override CheckMetadata GetMetadata() =>
        new BeatmapCheckMetadata
        {
            Modes = [Beatmap.Mode.Standard],
            Difficulties = [Beatmap.Difficulty.Hard],
            Category = "Spread",
            Message = "Too long streams for a Hard.",
            Author = "Greaper",

            Documentation = new Dictionary<string, string>
            {
                {
                    "Purpose",
                    @"
                    Pointing out streams of more than 5 notes in Hards."
                },
                {
                    "Reasoning",
                    @"
                    Players at this level are still getting used to 1/4, and longer streams take more stamina and accuracy than
                    they usually have.

                    A stream here is a run of objects whose starts are closer than 1/2 apart, measured as if the song were 180 BPM,
                    which is what the ranking criteria are written for. At lower tempos 1/4 is slower and doesn't count as a
                    stream, and at higher tempos 1/3 does."
                },
            },
        };

    public override Dictionary<string, IssueTemplate> GetTemplates() =>
        new()
        {
            {
                "Stream",
                new IssueTemplate(
                    Issue.Level.Warning,
                    "{0} Stream of {1} notes, more than 5.",
                    "timestamp -",
                    "count"
                )
                    .WithCause("More than 5 objects follow each other faster than 1/2 apart.")
                    .WithRule(RC.Osu.Hard_AvoidStreamsMadeMore5)
            },
        };

    public override IEnumerable<Issue> GetIssues(Beatmap beatmap) =>
        OsuPatterns
            .Streams(beatmap)
            .Where(run => run.Count > MaxStreamNotes)
            .Select(run => new Issue(
                GetTemplate("Stream"),
                beatmap,
                Timestamp.Get(run.First, run.Last),
                run.Count
            ));
}
