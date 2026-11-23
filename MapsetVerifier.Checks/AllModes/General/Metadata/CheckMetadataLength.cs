using System.Globalization;
using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Framework.Objects.Attributes;
using MapsetVerifier.Framework.Objects.Metadata;
using MapsetVerifier.Parser.Objects;
using MapsetVerifier.RankingCriteria;

namespace MapsetVerifier.Checks.AllModes.General.Metadata;

[Check]
public class CheckMetadataLength : GeneralCheck
{
    private const int MaxArtistTitleLength = 81;
    private const int MaxTagsLength = 1000;

    public override CheckMetadata GetMetadata() =>
        new()
        {
            Category = "Metadata",
            Message = "Too long metadata.",
            Author = "Greaper",

            Documentation = new Dictionary<string, string>
            {
                {
                    "Purpose",
                    @"
                    Keeping the artist and title fields at 81 characters or less, and the tags at 1000 characters or less."
                },
                {
                    "Reasoning",
                    @"
                    Longer fields get cut off in-game and on the website, and can fail to upload. Very long artists or titles
                    should be shortened, for example by leaving out parts that are already clear from the rest of the metadata."
                },
            },
        };

    public override Dictionary<string, IssueTemplate> GetTemplates() =>
        new()
        {
            {
                "ArtistTitle",
                new IssueTemplate(
                    Issue.Level.Problem,
                    "{0} is {1} characters long, which is more than 81; \"{2}\".",
                    "field",
                    "length",
                    "field content"
                )
                    .WithCause(
                        "The artist or title field, romanised or unicode, is longer than 81 characters."
                    )
                    .WithRule(RC.Metadata.ArtistTitleFieldsOver81)
            },
            {
                "Tags",
                new IssueTemplate(
                    Issue.Level.Problem,
                    "Tags are {0} characters long, which is more than 1000.",
                    "length"
                )
                    .WithCause("The tags field is longer than 1000 characters.")
                    .WithRule(RC.Metadata.TagsFieldShortenedLonger1000)
            },
        };

    public override IEnumerable<Issue> GetIssues(BeatmapSet beatmapSet)
    {
        // Inconsistent metadata between difficulties is reported elsewhere, so each distinct value is reported once.
        var metadata = beatmapSet.Beatmaps.Select(beatmap => beatmap.MetadataSettings).ToList();

        var fields = new (string Name, Func<Parser.Settings.MetadataSettings, string?> Get)[]
        {
            ("Romanised artist", settings => settings.artist),
            ("Unicode artist", settings => settings.artistUnicode),
            ("Romanised title", settings => settings.title),
            ("Unicode title", settings => settings.titleUnicode),
        };

        foreach (var (name, get) in fields)
        foreach (var value in metadata.Select(get).OfType<string>().Distinct())
        {
            var length = GetLength(value);

            if (length > MaxArtistTitleLength)
                yield return new Issue(GetTemplate("ArtistTitle"), null, name, length, value);
        }

        foreach (var tags in metadata.Select(settings => settings.tags).OfType<string>().Distinct())
        {
            var length = GetLength(tags);

            if (length > MaxTagsLength)
                yield return new Issue(GetTemplate("Tags"), null, length);
        }
    }

    /// <summary> Counts characters the way a person would, so emoji and other surrogate pairs count once. </summary>
    private static int GetLength(string value) => new StringInfo(value).LengthInTextElements;
}
