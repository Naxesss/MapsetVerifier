using MapsetVerifier.Checks.Utils;
using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Framework.Objects.Attributes;
using MapsetVerifier.Framework.Objects.Metadata;
using MapsetVerifier.Parser.Objects;
using MapsetVerifier.Parser.Objects.HitObjects;
using MapsetVerifier.Parser.Statics;

namespace MapsetVerifier.Checks.Taiko.Skinning;

[Check]
public class CheckSkinning : BeatmapSetCheck
{
    private static bool HasDrumrolls(BeatmapSet beatmapSet) =>
        beatmapSet.Beatmaps.Any(beatmap =>
            beatmap.GeneralSettings.mode == Beatmap.Mode.Taiko
            && beatmap.HitObjects.Any(hitObject => hitObject is Slider)
        );

    private static bool HasSpinners(BeatmapSet beatmapSet) =>
        beatmapSet.Beatmaps.Any(beatmap =>
            beatmap.GeneralSettings.mode == Beatmap.Mode.Taiko
            && beatmap.HitObjects.Any(hitObject => hitObject is Spinner)
        );

    // sliderscorepoint.png is a shared image with osu!: per the wiki, it "should only be used on
    // beatmaps without osu! difficulties" — when an osu! difficulty exists, the file is governed by
    // osu!'s own slider requirements, so its presence shouldn't be attributed to taiko's own skin.
    private static bool HasStandardDifficulty(BeatmapSet beatmapSet) =>
        beatmapSet.Beatmaps.Any(beatmap => beatmap.GeneralSettings.mode == Beatmap.Mode.Standard);

    private static readonly SkinSet[] Sets =
    [
        new SkinSet(
            "Hit object",
            new SkinElement("taikobigcircle.png", true),
            new SkinElement("taikobigcircleoverlay-{n}.png", true),
            new SkinElement("taikohitcircle.png", true),
            new SkinElement("taikohitcircleoverlay-{n}.png", true),
            new SkinElement(
                "sliderscorepoint.png",
                true,
                beatmapSet => HasDrumrolls(beatmapSet) && !HasStandardDifficulty(beatmapSet)
            ),
            new SkinElement("taiko-roll-middle.png", true, HasDrumrolls),
            new SkinElement("taiko-roll-end.png", true, HasDrumrolls),
            new SkinElement("spinner-warning.png", true, HasSpinners)
        ),
        new SkinSet(
            "Hitburst",
            new SkinElement("taiko-hit0-{n}.png", true),
            new SkinElement("taiko-hit100-{n}.png", true),
            new SkinElement("taiko-hit100k-{n}.png", true),
            new SkinElement("taiko-hit300-{n}.png", true),
            new SkinElement("taiko-hit300k-{n}.png", true)
        ),
        new SkinSet(
            "Pippidon",
            new SkinElement("pippidonclear{n}.png", true),
            new SkinElement("pippidonfail{n}.png", true),
            new SkinElement("pippidonidle{n}.png", true),
            new SkinElement("pippidonkiai{n}.png", true),
            new SkinElement("taiko-flower-group-{n}.png", false)
        ),
    ];

    public override CheckMetadata GetMetadata() =>
        new BeatmapCheckMetadata
        {
            Modes = [Beatmap.Mode.Taiko],
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
