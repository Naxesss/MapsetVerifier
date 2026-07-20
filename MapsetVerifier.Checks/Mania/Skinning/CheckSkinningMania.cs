using MapsetVerifier.Checks.Utils;
using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Framework.Objects.Attributes;
using MapsetVerifier.Framework.Objects.Metadata;
using MapsetVerifier.Parser.Objects;
using MapsetVerifier.Parser.Statics;

namespace MapsetVerifier.Checks.Mania.Skinning;

[Check]
public class CheckSkinningMania : BeatmapSetCheck
{
    // Per https://osu.ppy.sh/wiki/en/Skinning/osu!mania, only the hit burst and comboburst sets are
    // beatmap-skinnable; notes, keys, stage, and lighting elements are user-skin only.
    private static readonly SkinSet[] Sets =
    [
        new SkinSet(
            "Hitburst",
            new SkinElement("mania-hit0-{n}.png", true),
            new SkinElement("mania-hit50-{n}.png", true),
            new SkinElement("mania-hit100-{n}.png", true),
            new SkinElement("mania-hit200-{n}.png", true),
            new SkinElement("mania-hit300-{n}.png", true),
            new SkinElement("mania-hit300g-{n}.png", true)
        ),
        new SkinSet("Combo burst", new SkinElement("comboburst-mania-{n}.png", false)),
    ];

    public override CheckMetadata GetMetadata() =>
        new BeatmapCheckMetadata
        {
            Modes = [Beatmap.Mode.Mania],
            Category = "Skinning",
            Message = "Incomplete or incorrect skin element sets.",
            Author = "Greaper",

            Documentation = new Dictionary<string, string>
            {
                {
                    "Purpose",
                    @"
                    Ensuring that beatmap-specific skin elements are complete sets and use appropriate file formats."
                },
                {
                    "Reasoning",
                    @"
                    When skinning a gameplay element, complete sets of elements need to be skinned in order to
                    avoid conflicts between user-specific and beatmap-specific skins. If a required element of a
                    partially-skinned set is missing, the user-specific skin's version of that element may be
                    used instead, which can look inconsistent or even unreadable together with the rest of the
                    beatmap skin.
                    "
                },
            },
        };

    public override Dictionary<string, IssueTemplate> GetTemplates() =>
        new()
        {
            {
                "Incomplete Set",
                new IssueTemplate(
                    Issue.Level.Problem,
                    "The \"{0}\" skin set is only partially skinned, missing: {1}.",
                    "set",
                    "missing elements"
                ).WithCause(
                    "One or more elements of a skin set are present, but a required element of the same set is missing."
                )
            },
            {
                "Non Png",
                new IssueTemplate(
                    Issue.Level.Warning,
                    "\"{0}\" should be a .png file if it uses transparency.",
                    "path"
                ).WithCause(
                    "A skinned gameplay element does not use the .png format, which is required for elements that utilise transparency."
                )
            },
        };

    public override IEnumerable<Issue> GetIssues(BeatmapSet beatmapSet)
    {
        foreach (var set in Sets)
        {
            if (!SkinSetUtils.IsSetSkinnedAndRelevant(set, beatmapSet))
                continue;

            var missing = SkinSetUtils.GetMissingRequiredElements(set, beatmapSet).ToList();

            if (missing.Count > 0)
                yield return new Issue(
                    GetTemplate("Incomplete Set"),
                    null,
                    set.Name,
                    string.Join(", ", missing.Select(element => element.Pattern))
                );

            foreach (var element in set.Elements)
            {
                var path = SkinSetUtils.GetPresentPath(element, beatmapSet);

                if (path != null && !path.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                    yield return new Issue(GetTemplate("Non Png"), null, PathStatic.CutPath(path));
            }
        }
    }
}
