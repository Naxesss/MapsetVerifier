using Xunit;

namespace MapsetVerifier.RankingCriteria.Tests;

public class ContentUsagePermissionsTests
{
    private const string Markdown = """
        #### Allowed

        | Artist |  | Status |
        | :-- | :-- | :-- |
        | [![][FA]](https://osu.ppy.sh/beatmaps/artists/18) | [\*namirin](https://osu.ppy.sh/beatmaps/artists/18) | ![][true] |

        #### Allowed, with exceptions

        | Artist |  | Status | Notes |
        | :-- | :-- | :-- | :-- |
        | [![][FA]](https://osu.ppy.sh/beatmaps/artists/76) | [Zekk](https://osu.ppy.sh/beatmaps/artists/76) | ![][partial] | Do not use or upload tracks that are not available on the creator's Featured Artist listing. |
        |  | Mlumìn // SoundWarper | ![][partial] | Permission has been granted to use the track titled "Spinner (DnB Remix)". |
        | [![][FA]](https://osu.ppy.sh/beatmaps/artists/132) | [Yuyoyuppe / DJ'TEKINA//SOMETHING](https://osu.ppy.sh/beatmaps/artists/132) | ![][partial] | Tracks from or themed around *Touhou* should not be uploaded or used. |

        #### Disallowed

        | Artist | Status |
        | :-- | :-- |
        | a\_hisa | ![][false] |
        | DJMax (Neowiz) | ![][false] |
        | NOMA | ![][false] |
        """;

    private static readonly IReadOnlyList<ContentUsageArtist> Artists =
        ContentUsagePermissions.Parse(Markdown);

    [Fact]
    public void Parse_KeepsOnlyRestrictedArtists_WithPlainNotes()
    {
        Assert.Equal(
            [
                "Zekk",
                "Mlumìn // SoundWarper",
                "Yuyoyuppe / DJ'TEKINA//SOMETHING",
                "a_hisa",
                "DJMax (Neowiz)",
                "NOMA",
            ],
            Artists.Select(artist => artist.Name)
        );

        var yuyoyuppe = Artists[2];
        Assert.Equal(ContentUsageStatus.AllowedWithExceptions, yuyoyuppe.Status);
        Assert.Equal(["Yuyoyuppe", "DJ'TEKINA//SOMETHING"], yuyoyuppe.Aliases);
        Assert.Equal(
            "Tracks from or themed around Touhou should not be uploaded or used.",
            yuyoyuppe.Notes
        );

        Assert.Equal(ContentUsageStatus.Disallowed, Artists[3].Status);
        Assert.Null(Artists[3].Notes);
        Assert.Equal(["DJMax"], Artists[4].Aliases);
    }

    [Theory]
    [InlineData("a_hisa", "a_hisa")]
    [InlineData("Zekk feat. Somebody", "Zekk")]
    [InlineData("Mlumin & SoundWarper", "Mlumìn // SoundWarper")]
    [InlineData("SOUNDWARPER", "Mlumìn // SoundWarper")]
    [InlineData("Somebody vs. NOMA", "NOMA")]
    public void FindIn_MatchesNamesInTheArtist(string artist, string expected)
    {
        var found = Assert.Single(ContentUsagePermissions.FindIn(Artists, artist));
        Assert.Equal(expected, found.Name);
    }

    [Theory]
    [InlineData("NOMAD")]
    [InlineData("Zekkyou")]
    [InlineData("")]
    public void FindIn_IgnoresNamesInsideOtherWords(string artist)
    {
        Assert.Empty(ContentUsagePermissions.FindIn(Artists, artist));
    }

    [Fact]
    public void FindIn_ChecksTheSourceToo()
    {
        var found = Assert.Single(
            ContentUsagePermissions.FindIn(Artists, "Some Composer", "DJMAX RESPECT")
        );
        Assert.Equal("DJMax (Neowiz)", found.Name);
    }

    [Fact]
    public void Restricted_IsReadFromTheEmbeddedSnapshot()
    {
        Assert.Contains(
            ContentUsagePermissions.Restricted,
            artist => artist.Name == "kamome sano" && artist.Status == ContentUsageStatus.Disallowed
        );
        Assert.Contains(
            ContentUsagePermissions.Restricted,
            artist =>
                artist.Name == "Frums" && artist.Status == ContentUsageStatus.AllowedWithExceptions
        );
    }
}
