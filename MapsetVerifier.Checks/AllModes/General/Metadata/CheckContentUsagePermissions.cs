using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Framework.Objects.Attributes;
using MapsetVerifier.Framework.Objects.Metadata;
using MapsetVerifier.Parser.Objects;
using MapsetVerifier.RankingCriteria;

namespace MapsetVerifier.Checks.AllModes.General.Metadata
{
    [Check]
    public class CheckContentUsagePermissions : GeneralCheck
    {
        public override CheckMetadata GetMetadata() =>
            new()
            {
                Category = "Metadata",
                Message = "Artist restricts the use of their songs.",
                Author = "Greaper",

                Documentation = new Dictionary<string, string>
                {
                    {
                        "Purpose",
                        @"
                        Pointing out songs from artists who do not allow, or only partly allow, their songs to be used
                        in osu!, according to the [content usage permissions](https://osu.ppy.sh/wiki/en/Rules/Content_usage_permissions#artist-permissions)."
                    },
                    {
                        "Reasoning",
                        @"
                        Beatmaps using songs without permission can be removed. Many mappers do not know which artists
                        restrict their songs, so it is easy to map one by accident.

                        The artist and source fields are compared against the artists listed as *allowed, with
                        exceptions* and *disallowed*. Featured Artist tracks are always allowed, even from these
                        artists, which this check cannot tell apart; ignore it for those.

                        Names found only in the tags are pointed out as minor issues. Music labels and the composers
                        of covers often only appear there, but so do the names of mappers and others."
                    },
                },
            };

        public override Dictionary<string, IssueTemplate> GetTemplates() =>
            new()
            {
                {
                    "Disallowed",
                    new IssueTemplate(
                        Issue.Level.Warning,
                        "\"{0}\" does not allow their songs to be used. This can only be ranked with explicit "
                            + "permission for the song, shown in the description or discussion.",
                        "artist"
                    )
                        .WithCause(
                            "The artist or source names an artist listed as disallowed in the content usage permissions."
                        )
                        .WithRule(RC.General.BeatmapContentClearedRegardingContent)
                },
                {
                    "Exceptions",
                    new IssueTemplate(
                        Issue.Level.Warning,
                        "\"{0}\" only allows some of their songs to be used: {1}",
                        "artist",
                        "conditions"
                    )
                        .WithCause(
                            "The artist or source names an artist listed as allowed, with exceptions in the content usage permissions."
                        )
                        .WithRule(RC.General.BeatmapContentClearedRegardingContent)
                },
                {
                    "Tags",
                    new IssueTemplate(
                        Issue.Level.Minor,
                        "Tags mention \"{0}\", who restricts the use of their songs. Make sure this song is allowed, see the content usage permissions.",
                        "artist"
                    )
                        .WithCause(
                            "A restricted artist or label appears in the tags but not in the artist or source, as with "
                                + "music labels and covers. Tags also hold other names, such as mappers, so this may not "
                                + "be about the song at all."
                        )
                        .WithRule(RC.General.BeatmapContentClearedRegardingContent)
                },
            };

        public override IEnumerable<Issue> GetIssues(BeatmapSet beatmapSet)
        {
            var metadata = beatmapSet.Beatmaps.FirstOrDefault()?.MetadataSettings;

            if (metadata == null)
                yield break;

            var artists = ContentUsagePermissions
                .FindIn(
                    ContentUsagePermissions.Restricted,
                    metadata.artist,
                    metadata.artistUnicode,
                    metadata.source
                )
                .ToList();

            foreach (var artist in artists)
                yield return artist.Status == ContentUsageStatus.Disallowed
                    ? new Issue(GetTemplate("Disallowed"), null, artist.Name)
                    : new Issue(GetTemplate("Exceptions"), null, artist.Name, artist.Notes);

            // Labels such as MEGAREX and the composers of covers often only show up in the tags.
            var inTags = ContentUsagePermissions
                .FindIn(ContentUsagePermissions.Restricted, metadata.tags)
                .Except(artists);

            foreach (var artist in inTags)
                yield return new Issue(GetTemplate("Tags"), null, artist.Name);
        }
    }
}
