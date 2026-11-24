using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Framework.Objects.Attributes;
using MapsetVerifier.Framework.Objects.Metadata;
using MapsetVerifier.Parser.Objects;
using MapsetVerifier.Parser.Objects.HitObjects.Mania;
using MapsetVerifier.RankingCriteria;

namespace MapsetVerifier.Checks.Mania.Settings;

[Check]
public class CheckSpecialStyle : BeatmapSetCheck
{
    /// <summary> From this key count on, a difficulty can't be N+1 and the special style must be off. </summary>
    private const int TooManyKeys = 10;

    public override CheckMetadata GetMetadata() =>
        new BeatmapCheckMetadata
        {
            Modes = [Beatmap.Mode.Mania],
            Category = "Settings",
            Message = "Special style and N+1 tag.",
            Author = "Greaper",

            Documentation = new Dictionary<string, string>
            {
                {
                    "Purpose",
                    @"
                    Making sure N+1 difficulties, like 7K+1, have the special style option enabled and the ""N+1"" tag.

                    A difficulty counts as N+1 when its name says so, as in ""7K+1"" or ""7+1K"". The key count can't
                    tell, since 8 keys can be played either way. If only the option or only the tag is there, the two
                    contradict each other, and one of them has to change.

                    The option must stay off with 10 keys or more."
                },
                {
                    "Reasoning",
                    @"
                    In N+1, the leftmost column is mapped separately from the others. The option marks which difficulties
                    are made that way, and the tag lets players search for them.

                    If the option is on without the tag, add the tag if the difficulty is N+1, or turn the option off otherwise."
                },
            },
        };

    public override Dictionary<string, IssueTemplate> GetTemplates() =>
        new()
        {
            {
                "Not Enabled",
                new IssueTemplate(
                    Issue.Level.Problem,
                    "Named {0}, but special style is off.",
                    "playstyle"
                )
                    .WithCause(
                        "A difficulty below 10 keys is named as N+1, but doesn't have the special style option enabled."
                    )
                    .WithRule(RC.Mania.DifficultiesUsingN1Playstyle)
            },
            {
                "Missing Tag",
                new IssueTemplate(
                    Issue.Level.Problem,
                    "Named {0}, but \"N+1\" isn't in the tags.",
                    "playstyle"
                )
                    .WithCause(
                        "A difficulty below 10 keys is named as N+1, but the tags don't include \"N+1\"."
                    )
                    .WithRule(
                        RC.Mania.DifficultiesUsingN1Playstyle,
                        RC.Mania.NonStandardPlaystylePresentBeatmap
                    )
            },
            {
                "Too Many Keys",
                new IssueTemplate(
                    Issue.Level.Problem,
                    "Special style is on with {0} keys, must be off from 10 keys.",
                    "keys"
                )
                    .WithCause(
                        "A difficulty with 10 keys or more has the special style option enabled."
                    )
                    .WithRule(RC.Mania.DifficultiesUsingN1Playstyle)
            },
            {
                "Enabled Without Tag",
                new IssueTemplate(
                    Issue.Level.Warning,
                    "Special style is on, but \"N+1\" isn't in the tags."
                )
                    .WithCause(
                        "A difficulty below 10 keys has the special style option enabled, but neither its name nor the tags say N+1."
                    )
                    .WithRule(RC.Mania.DifficultiesUsingN1Playstyle)
            },
            {
                "Tag Without Enabled",
                new IssueTemplate(
                    Issue.Level.Warning,
                    "\"N+1\" is in the tags, but special style is off everywhere."
                )
                    .WithCause(
                        "The tags include \"N+1\", but no difficulty is named as N+1 or has the special style option enabled."
                    )
                    .WithRule(RC.Mania.DifficultiesUsingN1Playstyle)
            },
        };

    public override IEnumerable<Issue> GetIssues(BeatmapSet beatmapSet)
    {
        var difficulties = beatmapSet
            .Beatmaps.Select(beatmap => (Beatmap: beatmap, Playstyle: ManiaPlaystyle.Of(beatmap)))
            .Where(pair => pair.Playstyle != null)
            .Select(pair => (pair.Beatmap, Playstyle: pair.Playstyle!))
            .ToList();

        foreach (var (beatmap, playstyle) in difficulties)
        {
            var enabled = beatmap.GeneralSettings.specialN1Style;
            var tagged = HasTag(beatmap);

            if (playstyle.Keys >= TooManyKeys)
            {
                if (enabled)
                    yield return new Issue(GetTemplate("Too Many Keys"), beatmap, playstyle.Keys);
            }
            else if (playstyle.IsN1())
            {
                if (!enabled)
                    yield return new Issue(GetTemplate("Not Enabled"), beatmap, playstyle.Label);

                if (!tagged)
                    yield return new Issue(GetTemplate("Missing Tag"), beatmap, playstyle.Label);
            }
            else if (enabled && !tagged)
                yield return new Issue(GetTemplate("Enabled Without Tag"), beatmap);
        }

        // Above 9 keys the option has to be off, so a set with only those can have the tag on its own.
        var tagOnly =
            difficulties.Any(pair => pair.Playstyle.Keys < TooManyKeys)
            && difficulties.Any(pair => HasTag(pair.Beatmap))
            && !difficulties.Any(pair =>
                pair.Playstyle.IsN1() || pair.Beatmap.GeneralSettings.specialN1Style
            );

        if (tagOnly)
            yield return new Issue(GetTemplate("Tag Without Enabled"), null);
    }

    private static bool HasTag(Beatmap beatmap) => ManiaTags.Has(beatmap, "N+1");
}
