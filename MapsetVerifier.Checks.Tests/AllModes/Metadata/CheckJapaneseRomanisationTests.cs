using MapsetVerifier.Checks.AllModes.General.Metadata;
using MapsetVerifier.Framework.Objects;
using Xunit;

namespace MapsetVerifier.Checks.Tests.AllModes.Metadata;

public class CheckJapaneseRomanisationTests
{
    [Theory]
    [InlineData("ありがとう", "Arigatou")]
    [InlineData("ありがとう", "Arigato")]
    [InlineData("ありがとう", "Arigatoh")]
    [InlineData("さよなら", "Sayonara")]
    [InlineData("ちいさな こい", "Chiisana Koi")]
    [InlineData("ちゃんと", "Chanto")]
    [InlineData("いっしょに", "Isshoni")]
    [InlineData("まっちゃ", "Matcha")]
    [InlineData("こんにちは", "Konnichiwa")]
    [InlineData("こんにちは", "Konnichiha")]
    [InlineData("しんぶん", "Shinbun")]
    [InlineData("きょうも", "Kyou mo")]
    [InlineData("あいして (TV Size)", "Aishite (TV Size)")]
    [InlineData("あっ", "A")]
    [InlineData("すりぃ", "Surii")]
    [InlineData("ねえ、", "Ne,")]
    public void AcceptsModifiedHepburn(string unicode, string romanised)
    {
        using var context = CreateTitleContext(romanised, unicode);

        var issues = context.RunGeneralCheck<CheckJapaneseRomanisation>();

        Assert.Empty(issues);
    }

    [Theory]
    [InlineData("ありがとう", "Arigatoo", "Arigatou")]
    [InlineData("いっしょに", "Issyoni", "Isshoni")]
    [InlineData("しんぶん", "Sinbun", "Shinbun")]
    [InlineData("ちゃんと", "Tyanto", "Chanto")]
    [InlineData("まっちゃ", "Maccha", "Matcha")]
    [InlineData("つき", "Tuki", "Tsuki")]
    [InlineData("ふじ", "Huji", "Fuji")]
    [InlineData("かわいい", "Kawai", "Kawaii")]
    public void FlagsNonHepburnRomanisation(string unicode, string romanised, string suggestion)
    {
        using var context = CreateTitleContext(romanised, unicode);

        var issues = context.RunGeneralCheck<CheckJapaneseRomanisation>();

        var warning = Assert.Single(issues);
        Assert.Equal(Issue.Level.Warning, warning.level);
        Assert.Contains($"\"{suggestion}\"", warning.message);
    }

    [Theory]
    [InlineData("米津玄師", "Yonezu Kenshi")]
    [InlineData("ラブ", "Love")]
    [InlineData("ラブソング", "Rabusongu")]
    [InlineData("きみの名は", "Kimi no Na wa")]
    [InlineData("ゆよゆっぺ", "Yuyoyuppe")]
    [InlineData("Song", "Song")]
    [InlineData("☆☆☆", "***")]
    public void SkipsFieldsItCannotRead(string unicode, string romanised)
    {
        using var context = CreateTitleContext(romanised, unicode);

        var issues = context.RunGeneralCheck<CheckJapaneseRomanisation>();

        Assert.Empty(issues);
    }

    [Fact]
    public void SkipsRomanisedFieldsWithUnicode()
    {
        using var context = CreateTitleContext("Arigatō", "ありがとう");

        var issues = context.RunGeneralCheck<CheckJapaneseRomanisation>();

        Assert.Empty(issues);
    }

    [Fact]
    public void AcceptsArtistInPrintedOrder()
    {
        using var context = CreateArtistContext("Yanagi Nagi", "やなぎ なぎ");

        var issues = context.RunGeneralCheck<CheckJapaneseRomanisation>();

        Assert.Empty(issues);
    }

    [Fact]
    public void FlagsSwappedArtistNameOrder()
    {
        using var context = CreateArtistContext("Nagi Yanagi", "やなぎ なぎ");

        var issues = context.RunGeneralCheck<CheckJapaneseRomanisation>();

        var warning = Assert.Single(issues);
        Assert.Contains("name order", warning.message);
        Assert.Contains("\"Yanagi Nagi\"", warning.message);
    }

    [Fact]
    public void FlagsArtistSpelling()
    {
        using var context = CreateArtistContext("Yanagi Nazi", "やなぎ なぎ");

        var issues = context.RunGeneralCheck<CheckJapaneseRomanisation>();

        var warning = Assert.Single(issues);
        Assert.Contains("Modified Hepburn", warning.message);
    }

    private static CheckTestContext CreateTitleContext(string title, string titleUnicode) =>
        CheckTestContext.CreateFromOsu(
            new OsuBuilder()
                .Title(title)
                .TitleUnicode(titleUnicode)
                .Artist("Tests")
                .Creator("Tests")
        );

    private static CheckTestContext CreateArtistContext(string artist, string artistUnicode) =>
        CheckTestContext.CreateFromOsu(
            new OsuBuilder().Artist(artist).ArtistUnicode(artistUnicode).Creator("Tests")
        );
}
