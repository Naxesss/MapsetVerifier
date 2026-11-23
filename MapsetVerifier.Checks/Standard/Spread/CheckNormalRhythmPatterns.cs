using System.Globalization;
using MapsetVerifier.Checks.Utils;
using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Framework.Objects.Attributes;
using MapsetVerifier.Framework.Objects.Metadata;
using MapsetVerifier.Parser.Objects;
using MapsetVerifier.Parser.Statics;
using MapsetVerifier.RankingCriteria;

namespace MapsetVerifier.Checks.Standard.Spread;

[Check]
public class CheckNormalRhythmPatterns : BeatmapCheck
{
    private const int MaxClickedHalfRhythms = 3;
    private const int MaxHalfSliders = 4;
    private const int MaxHalfGaps = 10;
    private const double MaxBeatsWithoutRest = 16;

    public override CheckMetadata GetMetadata() =>
        new BeatmapCheckMetadata
        {
            Modes = [Beatmap.Mode.Standard],
            Difficulties = [Beatmap.Difficulty.Normal],
            Category = "Spread",
            Message = "Too intense 1/2 rhythm for a Normal.",
            Author = "Greaper",

            Documentation = new Dictionary<string, string>
            {
                {
                    "Purpose",
                    @"
                    Pointing out 1/2 rhythm in Normals that goes on for longer than newer players can keep up with:

                    - more than three clicked 1/2 rhythms in a row,
                    - more than four 1/2 sliders in a row,
                    - long chains of objects with 1/2 gaps between them,
                    - long stretches without a 1/1 or longer gap to rest."
                },
                {
                    "Reasoning",
                    @"
                    Players at this level can handle some 1/2, but long runs of it quickly become tiring and hard to read.
                    Regular 1/1 gaps give them a moment to recover.

                    The rhythm is measured as if the song were 180 BPM, which is what the ranking criteria are written for:
                    1/2 at 120 BPM is slow enough to count as 1/1, and 1/1 at 240 BPM is fast enough to count as 1/2.

                    The ranking criteria don't say how long a chain of 1/2 gaps may be, so chains are reported from more than
                    10 gaps, a little above what ranked Normals use. Stretches without rest are reported from 16 beats, twice the
                    two measures the ranking criteria ask for, since ranked Normals often go a little over."
                },
            },
        };

    public override Dictionary<string, IssueTemplate> GetTemplates() =>
        new()
        {
            {
                "Clicked",
                new IssueTemplate(
                    Issue.Level.Warning,
                    "{0} {1} clicked 1/2 rhythms in a row, more than 3.",
                    "timestamp -",
                    "count"
                )
                    .WithCause("More than three clicks follow each other 1/2 apart.")
                    .WithRule(RC.Osu.Normal_AvoidMoreThreeActivelyClicked)
            },
            {
                "Sliders",
                new IssueTemplate(
                    Issue.Level.Warning,
                    "{0} {1} 1/2 sliders in a row, more than 4.",
                    "timestamp -",
                    "count"
                )
                    .WithCause("More than four 1/2 long sliders follow each other with 1/2 gaps.")
                    .WithRule(RC.Osu.Normal_AvoidMoreFourConsecutive1)
            },
            {
                "Chain",
                new IssueTemplate(
                    Issue.Level.Minor,
                    "{0} {1} 1/2 gaps in a row between objects.",
                    "timestamp -",
                    "count"
                )
                    .WithCause(
                        "More than 10 objects follow each other with only 1/2 gaps in between."
                    )
                    .WithRule(RC.Osu.Normal_AvoidLongChainsHitObjects)
            },
            {
                "Rest",
                new IssueTemplate(
                    Issue.Level.Minor,
                    "{0} {1} beats without a 1/1 or longer gap.",
                    "timestamp -",
                    "beats"
                )
                    .WithCause(
                        "Objects follow each other for more than 16 beats at 180 BPM without a 1/1 or longer gap."
                    )
                    .WithRule(RC.Osu.Normal_Include11LongerGap)
            },
        };

    public override IEnumerable<Issue> GetIssues(Beatmap beatmap)
    {
        foreach (
            var run in OsuPatterns
                .ClickedHalfRuns(beatmap)
                .Where(run => run.Count > MaxClickedHalfRhythms)
        )
            yield return Report("Clicked", beatmap, run, run.Count);

        foreach (
            var run in OsuPatterns.HalfSliderRuns(beatmap).Where(run => run.Count > MaxHalfSliders)
        )
            yield return Report("Sliders", beatmap, run, run.Count);

        foreach (
            var run in OsuPatterns.HalfGapChains(beatmap).Where(run => run.Count > MaxHalfGaps)
        )
            yield return Report("Chain", beatmap, run, run.Count);

        foreach (
            var run in OsuPatterns
                .SectionsWithoutRest(beatmap)
                .Where(run => run.ReferenceBeats > MaxBeatsWithoutRest)
        )
            yield return Report(
                "Rest",
                beatmap,
                run,
                run.ReferenceBeats.ToString("0", CultureInfo.InvariantCulture)
            );
    }

    private Issue Report(string template, Beatmap beatmap, OsuPatterns.Run run, object amount) =>
        new(GetTemplate(template), beatmap, Timestamp.Get(run.First, run.Last), amount);
}
