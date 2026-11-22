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
    [InlineData("Song feat. 初音ミク", "Song feat.", "初音ミク")]
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
    [InlineData("Song (TV Size)", "Song (TV Size)")]
    public void DoesNotFlagRepeatedOrUnreadableParts(string unicode, string romanised)
    {
        using var context = CreateTitleContext(romanised, unicode);

        var issues = context.RunGeneralCheck<CheckRomanisation>();

        Assert.Empty(issues);
    }

    [Theory]
    [InlineData("紅蓮の弓矢 (TV Size)", "Guren no Yumiya (TV Size)")]
    [InlineData("君の銀の庭", "Kimi no Gin no Niwa")]
    [InlineData("邪神の婚礼、儀は愛と知る。", "Jashin no Konrei, Gi wa Ai to Shiru.")]
    [InlineData("DIVELA feat. 初音ミク", "DIVELA feat. Hatsune Miku")]
    [InlineData("otetsu feat. 巡音ルカ", "otetsu feat. Megurine Luka")]
    [InlineData("想いの眠るゆりかご", "Cradle Where Feelings Rest")]
    [InlineData("未来へ翔ぶために (TV Size)", "Ashita e tobutameni (TV Size)")]
    [InlineData("黝堊の祈り", "Yuuaku no Inori")]
    public void AcceptsKanjiReadingsTranslationsAndUnknownWords(string unicode, string romanised)
    {
        using var context = CreateTitleContext(romanised, unicode);

        var issues = context.RunGeneralCheck<CheckRomanisation>();

        Assert.Empty(issues);
    }

    [Theory]
    [InlineData("月の歌", "Tuki no Uta", "Tsuki no Uta")]
    [InlineData("紅蓮の弓矢", "Guren no Yumiya Syousetsu", null)]
    public void FlagsKanjiInAnotherSystem(string unicode, string romanised, string? suggestion)
    {
        using var context = CreateTitleContext(romanised, unicode);

        var issues = context.RunGeneralCheck<CheckRomanisation>();

        if (suggestion == null)
        {
            Assert.Empty(issues);
            return;
        }

        var warning = Assert.Single(issues);
        Assert.Contains("Modified Hepburn", warning.message);
        Assert.Contains($"\"{suggestion}\"", warning.message);
    }

    [Fact]
    public void FlagsSwappedKanjiNameWithoutSpaces()
    {
        using var context = CreateArtistContext("Masayuki Suzuki", "鈴木雅之");

        var issues = context.RunGeneralCheck<CheckRomanisation>();

        var warning = Assert.Single(issues);
        Assert.Contains("name order", warning.message);
        Assert.Contains("\"Suzuki Masayuki\"", warning.message);
    }

    [Theory]
    [InlineData("君の銀の庭", "Kimi no Gin no Niwa")]
    [InlineData("巡音ルカ", "Megurine Ruka")]
    [InlineData("鏡音リン", "Kagamine Rin")]
    [InlineData("40mP & かぐや", "40mP & Kaguya")]
    [InlineData("有馬かな (CV:潘めぐみ)", "Arima Kana (CV:Han Megumi)")]
    [InlineData("坂道のメロディ", "Sakamichi no Merodi")]
    [InlineData("佐咲紗花", "Sasakishahana")]
    [InlineData("水面の桜、夢は逆夢", "Suimen no Sakura Yume wa Sakayume")]
    [InlineData("六三四プロジェクト＆増田俊郎", "Rokusanyon Purojekuto&Masuda Toshio")]
    public void SuggestsKanjiReadingsWithWordBreaks(string unicode, string expected)
    {
        Assert.Equal(expected, new JapaneseRomaniser().Suggest(unicode));
    }

    [Theory]
    [InlineData("Für Elise", "Fuer Elise", "")]
    [InlineData("Für Elise", "Fur Elise", "")]
    [InlineData("Für Elise", "For Elise", "german")]
    [InlineData("Beyoncé", "Beyonce", "")]
    [InlineData("Sigur Rós", "Sigur Ros", "")]
    [InlineData("Straße", "Strasse", "german")]
    [InlineData("Räksmörgås", "Raksmorgas", "swedish")]
    [InlineData("Blåbærgrød", "Blaabaergroed", "danish")]
    [InlineData("Москва", "Moskva", "")]
    [InlineData("Пётр", "Pyotr", "")]
    [InlineData("Елена", "Yelena", "")]
    [InlineData("Київ", "Kyiv", "")]
    [InlineData(
        "Странствiя Рукоблуда и брата его Малафѣя",
        "Stranstviya Rukobluda i brata yego Malafeya",
        ""
    )]
    public void AcceptsLetterRules(string unicode, string romanised, string tags)
    {
        using var context = CreateTitleContext(romanised, unicode, tags);

        var issues = context.RunGeneralCheck<CheckRomanisation>();

        Assert.Empty(issues);
    }

    [Theory]
    [InlineData("Für Elise", "Fur Elise", "german", "Fuer Elise")]
    [InlineData("Straße", "Strase", "german", "Strasse")]
    [InlineData("Räksmörgås", "Raeksmoergaas", "swedish", "Raksmorgas")]
    [InlineData("Blåbærgrød", "Blabargrod", "danish", "Blaabaergroed")]
    [InlineData("Mø", "Mo", "", "Moe")]
    [InlineData("Юрий", "Jurij", "", "Yuriy")]
    [InlineData("Щука", "Schuka", "", "Shchuka")]
    public void FlagsLettersAgainstTheRules(
        string unicode,
        string romanised,
        string tags,
        string suggestion
    )
    {
        using var context = CreateTitleContext(romanised, unicode, tags);

        var issues = context.RunGeneralCheck<CheckRomanisation>();

        var warning = Assert.Single(issues);
        Assert.Equal(Issue.Level.Warning, warning.level);
        Assert.Contains($"\"{suggestion}\"", warning.message);
    }

    [Theory]
    [InlineData("Москва", "Moskva")]
    [InlineData("МОСКВА", "MOSKVA")]
    [InlineData("Чайковский и Пётр", "Chaykovskiy i Petr")]
    public void SuggestsCyrillicKeepingCase(string unicode, string expected)
    {
        Assert.Equal(expected, new CyrillicRomaniser().Suggest(unicode));
    }

    [Theory]
    [InlineData("Für Elise", "Fuer Elise")]
    [InlineData("MØ", "MOE")]
    public void SuggestsLatinKeepingCase(string unicode, string expected)
    {
        Assert.Equal(expected, LatinRomaniser.AnyLanguage.Suggest(unicode));
    }

    [Theory]
    [InlineData("我爱你", "Wo Ai Ni", "chinese")]
    [InlineData("我爱你", "I Love You", "chinese")]
    [InlineData("绿", "Lv", "chinese")]
    [InlineData("绿", "Lu", "chinese")]
    [InlineData("银行", "Yin Hang", "chinese")]
    [InlineData("我爱你 (TV Size)", "Wo Ai Ni (TV Size)", "chinese")]
    [InlineData("我爱你", "Woaini", "")]
    public void AcceptsChinesePinyinTranslationsAndUntaggedSets(
        string unicode,
        string romanised,
        string tags
    )
    {
        using var context = CreateTitleContext(romanised, unicode, tags);

        var issues = context.RunGeneralCheck<CheckRomanisation>();

        Assert.Empty(issues);
    }

    [Theory]
    [InlineData("我爱你", "Woaini")]
    [InlineData("我爱你", "wo ai ni")]
    [InlineData("我爱你", "Wo Aini")]
    [InlineData("北京", "Beijing")]
    public void FlagsChineseNotWrittenPerCharacter(string unicode, string romanised)
    {
        using var context = CreateTitleContext(romanised, unicode, "chinese");

        var issues = context.RunGeneralCheck<CheckRomanisation>();

        var warning = Assert.Single(issues);
        Assert.Contains("its own capitalised word", warning.message);
        Assert.Contains("Ignore for proper nouns", warning.message);
    }

    [Fact]
    public void FlagsLeftOutChinese()
    {
        using var context = CreateTitleContext("Song feat.", "Song feat. 周杰伦", "chinese");

        var issues = context.RunGeneralCheck<CheckRomanisation>();

        var warning = Assert.Single(issues);
        Assert.Contains("leaves out \"周杰伦\"", warning.message);
    }

    [Theory]
    [InlineData("我爱你", "Wo Ai Ni")]
    [InlineData("北京 (TV Size)", "Bei Jing (TV Size)")]
    public void SuggestsChinesePerCharacter(string unicode, string expected)
    {
        Assert.Equal(expected, new ChineseRomaniser().Suggest(unicode));
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

    private static CheckTestContext CreateTitleContext(
        string title,
        string titleUnicode,
        string tags = ""
    ) =>
        CheckTestContext.CreateFromOsu(
            new OsuBuilder()
                .Title(title)
                .TitleUnicode(titleUnicode)
                .Artist("Tests")
                .Creator("Tests")
                .Tags(tags)
        );

    private static CheckTestContext CreateArtistContext(string artist, string artistUnicode) =>
        CheckTestContext.CreateFromOsu(
            new OsuBuilder().Artist(artist).ArtistUnicode(artistUnicode).Creator("Tests")
        );
}
