using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Framework.Objects.Attributes;
using MapsetVerifier.Framework.Objects.Metadata;
using MapsetVerifier.Parser.Objects;
using MapsetVerifier.Parser.Objects.HitObjects.Mania;
using MapsetVerifier.RankingCriteria;

namespace MapsetVerifier.Checks.Mania.Settings;

[Check]
public class CheckPlaystyleTags : BeatmapCheck
{
    public override CheckMetadata GetMetadata() =>
        new BeatmapCheckMetadata
        {
            Modes = [Beatmap.Mode.Mania],
            Category = "Settings",
            Message = "Playstyle missing from the tags.",
            Author = "Greaper",

            Documentation = new Dictionary<string, string>
            {
                {
                    "Purpose",
                    @"
                    Making sure the tags name every playstyle that isn't standard, such as DP or 10K2S.

                    A difficulty with more than 10 keys always has such a playstyle. If its name doesn't say which one,
                    a tag for any playstyle with that key count is enough.

                    With 10 keys or less, only a name like ""7K+1"" shows that the playstyle isn't standard. For those,
                    see the check for the special style and N+1 tag."
                },
                {
                    "Reasoning",
                    @"
                    Players search for a playstyle by its tag. The key count alone doesn't say how the keys are meant to
                    be played."
                },
            },
        };

    public override Dictionary<string, IssueTemplate> GetTemplates() =>
        new()
        {
            {
                "Missing",
                new IssueTemplate(
                    Issue.Level.Problem,
                    "The tags don't include the playstyle of this difficulty, {0}.",
                    "playstyle"
                )
                    .WithCause(
                        "A difficulty has a playstyle that isn't standard, and the tags don't include that playstyle."
                    )
                    .WithRule(RC.Mania.NonStandardPlaystylePresentBeatmap)
            },
        };

    public override IEnumerable<Issue> GetIssues(Beatmap beatmap)
    {
        var playstyle = ManiaPlaystyle.Of(beatmap);

        if (playstyle == null || playstyle.Keys <= 10)
            yield break;

        var playstyles = ManiaPlaystyle.PlaystylesFor(playstyle.Keys);

        // Key counts without any playstyle aren't allowed to begin with.
        if (playstyles.Count == 0)
            yield break;

        // A difficulty whose name doesn't say which one it is can be any of them.
        var named = playstyles.Where(names => names[0] == playstyle.Label).ToList();
        var candidates = named.Count > 0 ? named : playstyles;

        if (candidates.SelectMany(names => names).Any(name => ManiaTags.Has(beatmap, name)))
            yield break;

        yield return new Issue(
            GetTemplate("Missing"),
            beatmap,
            string.Join(" or ", candidates.Select(names => names[0]))
        );
    }
}
