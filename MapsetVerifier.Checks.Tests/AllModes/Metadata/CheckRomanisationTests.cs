using MapsetVerifier.Checks.AllModes.General.Metadata;
using MapsetVerifier.Checks.Utils.Romanisation;
using MapsetVerifier.Framework.Objects;
using Xunit;

namespace MapsetVerifier.Checks.Tests.AllModes.Metadata;

public class CheckRomanisationTests
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

        var issues = context.RunGeneralCheck<CheckRomanisation>();

        Assert.Empty(issues);
    }

    [Theory]
    [InlineData("いっしょに", "Issyoni", "Isshoni")]
    [InlineData("しんぶん", "Sinbun", "Shinbun")]
    [InlineData("ちゃんと", "Tyanto", "Chanto")]
    [InlineData("まっちゃ", "Maccha", "Matcha")]
    [InlineData("つき", "Tuki", "Tsuki")]
    [InlineData("ふじ", "Huji", "Fuji")]
    [InlineData("もうそう♥つき", "Mousou Tuki", "Mousou Tsuki")]
    [InlineData("Song (つき Remix)", "Song (Tuki Remix)", "Song (Tsuki Remix)")]
    [InlineData("つ～き！", "Tu~ki!", "Tsu~ki!")]
    [InlineData("HoYoつき", "HoYo Tuki", "HoYo Tsuki")]
    public void FlagsNonHepburnRomanisation(string unicode, string romanised, string suggestion)
    {
        using var context = CreateTitleContext(romanised, unicode);

        var issues = context.RunGeneralCheck<CheckRomanisation>();

        var warning = Assert.Single(issues);
        Assert.Equal(Issue.Level.Warning, warning.level);
        Assert.Contains($"\"{suggestion}\"", warning.message);
    }

    [Theory]
    [InlineData("米津玄師", "Yonezu Kenshi")]
    [InlineData("きみの名は", "Kimi no Na wa")]
    [InlineData("ゆよゆっぺ", "Yuyoyuppe")]
    [InlineData("Song", "Song")]
    [InlineData("☆☆☆", "***")]
    public void SkipsFieldsItCannotRead(string unicode, string romanised)
    {
        using var context = CreateTitleContext(romanised, unicode);

        var issues = context.RunGeneralCheck<CheckRomanisation>();

        Assert.Empty(issues);
    }

    [Theory]
    [InlineData("かめりあ", "Camellia")]
    [InlineData("かめりあ feat. ななひら", "Camellia feat. Nanahira")]
    [InlineData("THE WORLD REVOLVING (かめりあ Remix)", "THE WORLD REVOLVING (Camellia Remix)")]
    [InlineData("もうそう♥えくすぷれす", "Mousou Express")]
    [InlineData("は～とび～と♥お～ば～ひ～～～～と！！！！", "Hea~rt Bea~t <3 O~ver~Hea~~~~t!!!!")]
    [InlineData("すくりぃむ！ (TV Size)", "Scream! (TV Size)")]
    [InlineData("ふぃすとはっかーず", "Fist Hackers")]
    [InlineData("じっぷす", "Zips")]
    [InlineData("ありがとう", "Arigatoo")]
    [InlineData("かわいい", "Kawai")]
    public void LeavesHiraganaLoanWordsAndOfficialNamesQuiet(string unicode, string romanised)
    {
        using var context = CreateTitleContext(romanised, unicode);

        var issues = context.RunGeneralCheck<CheckRomanisation>();

        Assert.Empty(issues);
    }

    [Theory]
    [InlineData("もうそう♥えくすぷれす", "Mousou Ekusupuresu")]
    [InlineData("は～とび～と♥お～ば～ひ～～～～と！！！！", "Ha~tobi~to O~ba~hi~~~~to!!!!")]
    [InlineData("THE WORLD REVOLVING (かめりあ Remix)", "THE WORLD REVOLVING (Kameria Remix)")]
    [InlineData("HoYoHoYoにしてあげる♪", "HoYoHoYo Nishiteageru")]
    [InlineData("スピラ・スピカ", "Supira Supika")]
    [InlineData("キネマ106", "Kinema106")]
    [InlineData("やなぎ　なぎ", "Yanagi Nagi")]
    public void SuggestsRomanisationKeepingPunctuation(string unicode, string expected)
    {
        Assert.Equal(expected, new JapaneseRomaniser().Suggest(unicode));
    }

    [Theory]
    [InlineData(
        "Taishi ft. みとせのりこ - The Blue Sanctuary",
        "Taishi ft.  - The Blue Sanctuary",
        "みとせのりこ"
    )]
    [InlineData("かめりあ feat. ななひら", "Camellia feat.", "ななひら")]
    [InlineData("つき (TV Size)", "(TV Size)", "つき")]
    public void FlagsMissingPart(string unicode, string romanised, string missing)
    {
        using var context = CreateTitleContext(romanised, unicode);

        var issues = context.RunGeneralCheck<CheckRomanisation>();

        var warning = Assert.Single(issues);
        Assert.Equal(Issue.Level.Warning, warning.level);
        Assert.Contains($"leaves out \"{missing}\"", warning.message);
    }

    [Theory]
    [InlineData("よるにかける (Yoru ni Kakeru)", "Yoru ni Kakeru")]
    [InlineData("つき Tsuki", "Tsuki")]
    [InlineData("夜に駆ける (Yoru ni Kakeru)", "Yoru ni Kakeru")]
    [InlineData("Song feat. 初音ミク", "Song feat.")]
    [InlineData("Song (TV Size)", "Song (TV Size)")]
    public void DoesNotFlagRepeatedOrUnreadableParts(string unicode, string romanised)
    {
        using var context = CreateTitleContext(romanised, unicode);

        var issues = context.RunGeneralCheck<CheckRomanisation>();

        Assert.Empty(issues);
    }

    [Fact]
    public void SkipsRomanisedFieldsWithUnicode()
    {
        using var context = CreateTitleContext("Arigatō", "ありがとう");

        var issues = context.RunGeneralCheck<CheckRomanisation>();

        Assert.Empty(issues);
    }

    [Fact]
    public void AcceptsArtistInPrintedOrder()
    {
        using var context = CreateArtistContext("Yanagi Nagi", "やなぎ なぎ");

        var issues = context.RunGeneralCheck<CheckRomanisation>();

        Assert.Empty(issues);
    }

    [Fact]
    public void FlagsSwappedArtistNameOrder()
    {
        using var context = CreateArtistContext("Nagi Yanagi", "やなぎ なぎ");

        var issues = context.RunGeneralCheck<CheckRomanisation>();

        var warning = Assert.Single(issues);
        Assert.Contains("name order", warning.message);
        Assert.Contains("\"Yanagi Nagi\"", warning.message);
    }

    [Fact]
    public void FlagsArtistSpelling()
    {
        using var context = CreateArtistContext("Kudou Sizuka", "くどう しずか");

        var issues = context.RunGeneralCheck<CheckRomanisation>();

        var warning = Assert.Single(issues);
        Assert.Contains("Modified Hepburn", warning.message);
        Assert.Contains("\"Kudou Shizuka\"", warning.message);
    }

    [Theory]
    [InlineData("ラブソング", "Love Song")]
    [InlineData("チョコレート", "Chocolate")]
    [InlineData("チョコレート", "Chokoreeto")]
    [InlineData("チョコレート", "Chokoreto")]
    [InlineData("ティータイム", "Tea Time")]
    [InlineData("ティータイム", "Tiitaimu")]
    [InlineData("ファンタジー", "Fantajii")]
    [InlineData("ヴァンパイア", "Vanpaia")]
    [InlineData("ウィッチ", "Witchi")]
    public void AcceptsKatakanaLoanWordsAndHepburn(string unicode, string romanised)
    {
        using var context = CreateTitleContext(romanised, unicode);

        var issues = context.RunGeneralCheck<CheckRomanisation>();

        Assert.Empty(issues);
    }

    [Theory]
    [InlineData("チョコレート", "Tyokoreeto", "Chokoreeto")]
    [InlineData("ツナミ", "Tunami", "Tsunami")]
    [InlineData("シンデレラ", "Sinderera", "Shinderera")]
    [InlineData("マッチ", "Macchi", "Matchi")]
    [InlineData("ﾂﾅﾐ", "Tunami", "Tsunami")]
    public void FlagsKatakanaInAnotherSystem(string unicode, string romanised, string suggestion)
    {
        using var context = CreateTitleContext(romanised, unicode);

        var issues = context.RunGeneralCheck<CheckRomanisation>();

        var warning = Assert.Single(issues);
        Assert.Equal(Issue.Level.Warning, warning.level);
        Assert.Contains($"\"{suggestion}\"", warning.message);
    }

    [Fact]
    public void FlagsTraditionalHepburnInHiragana()
    {
        using var context = CreateTitleContext("Shimbun", "しんぶん");

        var issues = context.RunGeneralCheck<CheckRomanisation>();

        var warning = Assert.Single(issues);
        Assert.Contains("\"Shinbun\"", warning.message);
    }

    [Fact]
    public void FlagsSwappedKatakanaArtistNameOrder()
    {
        using var context = CreateArtistContext("Tatsuya Kitani", "キタニ タツヤ");

        var issues = context.RunGeneralCheck<CheckRomanisation>();

        var warning = Assert.Single(issues);
        Assert.Contains("name order", warning.message);
        Assert.Contains("\"Kitani Tatsuya\"", warning.message);
    }

    [Fact]
    public void AcceptsKatakanaArtistInPrintedOrder()
    {
        using var context = CreateArtistContext("Kitani Tatsuya", "キタニ　タツヤ");

        var issues = context.RunGeneralCheck<CheckRomanisation>();

        Assert.Empty(issues);
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
