using MapsetVerifier.Checks.AllModes.General.Metadata;
using MapsetVerifier.Framework.Objects;
using Xunit;

namespace MapsetVerifier.Checks.Tests.AllModes.Metadata;

public class CheckSymbolSpacingTests
{
    [Theory]
    [InlineData("Song*Title*", "*Title*")]
    [InlineData("*Song*Title", "*Song*")]
    [InlineData("Hatto~dandara shooting MIX~", "~dandara shooting MIX~")]
    [InlineData("Song-Sub Title-", "-Sub Title-")]
    [InlineData("Song(TV Size)", "(TV Size)")]
    [InlineData("Song[Remix]", "[Remix]")]
    public void FlagsGroupWithoutSpace(string title, string group)
    {
        var warning = Assert.Single(Run(title, title));

        Assert.Equal(Issue.Level.Warning, warning.level);
        Assert.Contains($"\"{group}\"", warning.message);
    }

    [Fact]
    public void FlagsGroupInUnicodeField()
    {
        var warning = Assert.Single(Run("Song *Title*", "Song★Title★"));

        Assert.Contains("\"★Title★\" in unicode title", warning.message);
    }

    [Theory]
    [InlineData("七森中☆ごらく部", "Nanamori-chu*Goraku-bu", "*")]
    [InlineData("愛♡スクリ～ム", "Ai<3Scream!", "<3")]
    [InlineData("君➔僕", "Kimi->Boku", "->")]
    public void FlagsSymbolBetweenWords(string unicode, string title, string symbol)
    {
        var warning = Assert.Single(Run(title, unicode));

        Assert.Contains($"\"{symbol}\"", warning.message);
    }

    [Theory]
    [InlineData("Song *Title*", "Song ★Title★")]
    [InlineData("Song ~Sub Title~", "Song ～Sub Title～")]
    [InlineData("Ai <3 Scream!", "愛♡スクリ～ム")]
    [InlineData("Nanamori-chu * Goraku-bu", "七森中☆ごらく部")]
    [InlineData("Song *Title*", "歌★題★")]
    [InlineData("Shinryaku no Susume* (TV Size)", "侵略ノススメ☆ (TV Size)")]
    [InlineData("Hea~rt Bea~t <3 O~ver~Hea~~~~t!!!!", "は～とび～と♥お～ば～ひ～～～～と！！！！")]
    [InlineData(".*302?ionwan2go*.", "。✰302？ionwan2go✰。")]
    [InlineData("DECO*27", "DECO*27")]
    [InlineData("Love*Love*", "Love★Love★")]
    [InlineData("Mada~mada~", "まだ～まだ～")]
    [InlineData("Bling-Bang-Bang-Born (TV Size)", "Bling-Bang-Bang-Born (TV Size)")]
    [InlineData("Sh*tty", "Sh★tty")]
    [InlineData("f(x)", "f(x)")]
    [InlineData("LOVE(LESS)", "LOVE(LESS)")]
    [InlineData("Eye(sight)", "Eye(sight)")]
    [InlineData("Kimi->Boku", "君→僕")]
    public void DoesNotFlagOtherSymbols(string title, string unicode)
    {
        Assert.Empty(Run(title, unicode));
    }

    [Fact]
    public void LeavesArtistMarkersToTheMarkerSpacingCheck()
    {
        using var context = CheckTestContext.CreateFromOsu(
            new OsuBuilder().Artist("Miku(CV: Fujita Saki)")
        );

        Assert.Empty(context.RunGeneralCheck<CheckSymbolSpacing>());
    }

    private static List<Issue> Run(string title, string titleUnicode)
    {
        using var context = CheckTestContext.CreateFromOsu(
            new OsuBuilder().Title(title).TitleUnicode(titleUnicode)
        );
        return context.RunGeneralCheck<CheckSymbolSpacing>();
    }
}
