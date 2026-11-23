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
    [InlineData("すくりぃむ！ (TV Size)", "Scream! (TV Size)")]
    [InlineData("じっぷす", "Zips")]
    public void PointsOutHiraganaLoanWordsAndOfficialNamesAsUnconfirmed(
        string unicode,
        string romanised
    )
    {
        using var context = CreateTitleContext(romanised, unicode);

        var issues = context.RunGeneralCheck<CheckRomanisation>();

        AssertUnconfirmed(issues);
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
    [InlineData("フローズン (TV Size)", "(TV Size)", "フローズン")]
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
    [InlineData("黝堊の祈り", "Yuuaku no Inori")]
    public void AcceptsKanjiReadingsAndUnknownWords(string unicode, string romanised)
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
            AssertUnconfirmed(issues);
            return;
        }

        var warning = Assert.Single(issues);
        Assert.Contains($"\"{suggestion}\"", warning.message);
    }

    [Fact]
    public void FlagsSwappedKanjiNameWithoutSpaces()
    {
        using var context = CreateArtistContext("Masayuki Suzuki", "鈴木雅之");

        var issues = context.RunGeneralCheck<CheckRomanisation>();

        var warning = Assert.Single(issues);
        Assert.Contains("Artist \"", warning.message);
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
        Assert.Contains("unless a proper noun", warning.message);
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

    [Theory]
    [InlineData("余白のほんね", "something wrong here", "Yohaku no Honne")]
    [InlineData("想いの眠るゆりかご", "Cradle Where Feelings Rest", "Omoi no Nemuru Yurikago")]
    [InlineData("ありがとう", "123", "Arigatou")]
    [InlineData(
        "は～とび～と♥お～ば～ひ～～～～と！！！！",
        "Hea~rt Bea~t <3 O~ver~Hea~~~~t!!!!",
        "Ha~tobi~to O~ba~hi~~~~to!!!!"
    )]
    public void SuggestsReadingForUnconfirmedTitles(
        string unicode,
        string romanised,
        string suggestion
    )
    {
        using var context = CreateTitleContext(romanised, unicode);

        var issues = context.RunGeneralCheck<CheckRomanisation>();

        var warning = Assert.Single(issues);
        Assert.Equal(Issue.Level.Warning, warning.level);
        Assert.Contains($"\"{suggestion}\"", warning.message);
    }

    [Theory]
    [InlineData("つき Tsuki", "Tsuki")]
    [InlineData("未来へ翔ぶために (TV Size)", "Ashita e tobutameni (TV Size)")]
    public void PointsOutSpellingsItCannotConfirm(string unicode, string romanised)
    {
        using var context = CreateTitleContext(romanised, unicode);

        var issues = context.RunGeneralCheck<CheckRomanisation>();

        AssertUnconfirmed(issues);
    }

    [Theory]
    [InlineData("ハートノイロ", "Heart noo Iro", "Heart no Iro")]
    [InlineData("ハートノイロ", "Heart no Iroo", "Heart no Iro")]
    [InlineData("ハートノイロ", "Heart no Rio", "Heart no Iro")]
    [InlineData("ハートノイロ", "Heart n Iro", "Heart no Iro")]
    [InlineData("ハートノイロ", "Heart o Iro", "Heart no Iro")]
    [InlineData("ハートノイロ", "Heart no ro", "Heart no iro")]
    [InlineData("ハートノイロ", "Haert no Iro", "Heart no Iro")]
    [InlineData("ハートノイロ", "HEART NOO IRO", "HEART NO IRO")]
    [InlineData("ハートノイロ (TV Size)", "Heartt no Iro (TV Size)", "Heart no Iro (TV Size)")]
    [InlineData("坂道のメロディ", "Sakamichi noo Melody", "Sakamichi no Melody")]
    [InlineData("余白のほんね", "Yohaku no Honnne", "Yohaku no Honne")]
    [InlineData("余白のほんね", "Yohaku, no ~Hone~", "Yohaku, no ~Honne~")]
    [InlineData("余白のほんね", "ohaku no Honne", "Yohaku no Honne")]
    [InlineData("ありがとう", "Arigatoo", "Arigatou")]
    [InlineData("かわいい", "Kawai", "Kawaii")]
    public void SuggestsCorrectedSpelling(string unicode, string romanised, string correction)
    {
        using var context = CreateTitleContext(romanised, unicode);

        var warning = Assert.Single(context.RunGeneralCheck<CheckRomanisation>());

        Assert.Equal(Issue.Level.Warning, warning.level);
        Assert.Contains(
            $"Title \"{romanised}\" should be \"{correction}\", unless official",
            warning.message
        );
    }

    [Theory]
    [InlineData("余白のほんね", "Yohaku no Honne")]
    [InlineData("つき × ほし", "Tsuki x Hoshi")]
    [InlineData("つき × ほし", "Tsuki Hoshi")]
    [InlineData("つき×ほし", "TsukixHoshi")]
    public void AcceptsReadingsAndCrosses(string unicode, string romanised)
    {
        using var context = CreateTitleContext(romanised, unicode);

        Assert.Empty(context.RunGeneralCheck<CheckRomanisation>());
    }

    [Theory]
    [InlineData("かめりあ", "Camellia")]
    [InlineData("かめりあ feat. ななひら", "Camellia feat. Nanahira")]
    [InlineData("もうそう♥えくすぷれす", "Mousou Express")]
    [InlineData("ふぃすとはっかーず", "Fist Hackers")]
    [InlineData("その未来へ", "Sono Saki e")]
    [InlineData("インフェルノ", "Inferno")]
    [InlineData("チョコレート", "Chocolate")]
    [InlineData("ティータイム", "Tea Time")]
    [InlineData("バイバイ (Bye)", "Bye Bye (Bye)")]
    [InlineData("バイバイ (Bye)", "Bye Bye")]
    [InlineData("ラブソング", "Love Song")]
    [InlineData("ｷﾗｰ", "KILLER")]
    [InlineData("THE WORLD REVOLVING (かめりあ Remix)", "THE WORLD REVOLVING (Camellia Remix)")]
    [InlineData("ハートノイロ", "Heart no Iro")]
    [InlineData("坂道のメロディ", "Sakamichi no Melody")]
    [InlineData("花びらメモリーズ", "Hanabira Memories")]
    [InlineData("燦々デイズ", "Sansan Days")]
    [InlineData("潮", "Ushio")]
    [InlineData("永久より永遠に", "Towa yori Towa ni")]
    [InlineData("水面の桜、夢は逆夢", "Minamo no Sakura, Yume wa Sakayume")]
    [InlineData("進化のメロディ", "Shinka no Melody")]
    public void AcceptsOtherReadingsAndLoanWordsItKnows(string unicode, string romanised)
    {
        using var context = CreateTitleContext(romanised, unicode);

        Assert.Empty(context.RunGeneralCheck<CheckRomanisation>());
    }

    [Theory]
    [InlineData("水瀬いのり", "Inori Minase", "Minase Inori")]
    [InlineData("下村陽子", "Yoko Shimomura", "Shimomura Yoko")]
    [InlineData(
        "環いろは(CV:麻倉もも)",
        "Iroha Tamaki (CV: Asakura Momo)",
        "Tamaki Iroha (CV: Asakura Momo)"
    )]
    [InlineData(
        "ヨリ (CV: 原 紗友里), アカリ (CV: 浅倉杏美)",
        "Yori (CV: Sayuri Hara), Akari (CV: Azumi Asakura)",
        "Yori (CV: Hara Sayuri), Akari (CV: Asakura Azumi)"
    )]
    public void SuggestsPrintedNameOrder(string unicode, string romanised, string expected)
    {
        using var context = CreateArtistContext(romanised, unicode);

        var warning = Assert.Single(context.RunGeneralCheck<CheckRomanisation>());

        Assert.Contains("Artist \"", warning.message);
        Assert.Contains($"\"{expected}\"", warning.message);
    }

    [Theory]
    [InlineData("水瀬いのり", "Minase Inori")]
    [InlineData("坂本真綾", "Sakamoto Maaya")]
    public void AcceptsPrintedNameOrder(string unicode, string romanised)
    {
        using var context = CreateArtistContext(romanised, unicode);

        Assert.Empty(context.RunGeneralCheck<CheckRomanisation>());
    }

    [Theory]
    [InlineData("水瀬", "みなせ")]
    [InlineData("ハト", "heart")]
    [InlineData("メロディ", "melody")]
    public void FindsWordsInDictionary(string word, string expected) =>
        Assert.Contains(expected, JapaneseDictionary.Find(word));

    [Theory]
    [InlineData("")]
    [InlineData("a")]
    [InlineData("水瀬水瀬水瀬")]
    [InlineData("龥龥龥龥")]
    public void FindsNothingForUnknownWords(string word) =>
        Assert.Empty(JapaneseDictionary.Find(word));

    [Fact]
    public void PointsOutArtistsItCannotConfirm()
    {
        using var context = CreateArtistContext("Iroha Tamakii", "環いろは");

        var issues = context.RunGeneralCheck<CheckRomanisation>();

        AssertUnconfirmed(issues);
    }

    [Theory]
    [InlineData("", false)]
    [InlineData("japanese", true)]
    [InlineData("cantonese", false)]
    public void NeedsJapaneseTagForKanjiAlone(string tags, bool isReported)
    {
        using var context = CreateTitleContext("Zips", "潮", tags);

        Assert.Equal(isReported, context.RunGeneralCheck<CheckRomanisation>().Count > 0);
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
        Assert.Contains("Artist \"", warning.message);
        Assert.Contains("\"Yanagi Nagi\"", warning.message);
    }

    [Fact]
    public void FlagsArtistSpelling()
    {
        using var context = CreateArtistContext("Kudou Sizuka", "くどう しずか");

        var issues = context.RunGeneralCheck<CheckRomanisation>();

        var warning = Assert.Single(issues);
        Assert.Contains("\"Kudou Shizuka\"", warning.message);
    }

    [Theory]
    [InlineData("チョコレート", "Chokoreeto")]
    [InlineData("チョコレート", "Chokoreto")]
    [InlineData("ティータイム", "Tiitaimu")]
    [InlineData("ファンタジー", "Fantajii")]
    [InlineData("ヴァンパイア", "Vanpaia")]
    [InlineData("ウィッチ", "Witchi")]
    public void AcceptsKatakanaHepburn(string unicode, string romanised)
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
        Assert.Contains("Artist \"", warning.message);
        Assert.Contains("\"Kitani Tatsuya\"", warning.message);
    }

    [Fact]
    public void AcceptsKatakanaArtistInPrintedOrder()
    {
        using var context = CreateArtistContext("Kitani Tatsuya", "キタニ　タツヤ");

        var issues = context.RunGeneralCheck<CheckRomanisation>();

        Assert.Empty(issues);
    }

    [Theory]
    [InlineData("夜に駆ける (Yoru ni Kakeru)", "Yoru ni Kakeru (Yoru ni Kakeru)", "Yoru ni Kakeru")]
    [InlineData("ありがとう (Arigato)", "Arigatou (Arigato)", "Arigatou")]
    [InlineData("ありがとう（Arigatou） (TV Size)", "Arigatou (Arigatou) (TV Size)", "Arigatou")]
    [InlineData("Song feat. つき (Tsuki)", "Song feat. Tsuki (Tsuki)", "Tsuki")]
    [InlineData("Tsuki (つき)", "Tsuki (Tsuki)", "Tsuki")]
    [InlineData("つき / Tsuki", "Tsuki / Tsuki", "Tsuki")]
    [InlineData("つき ～Tsuki～", "Tsuki ~Tsuki~", "Tsuki")]
    [InlineData("ソレイユ -Soleil-", "Soleil -Soleil-", "Soleil")]
    [InlineData("Soleil (ソレイユ) (TV Size)", "Soleil (Soleil) (TV Size)", "Soleil")]
    [InlineData("Москва (Moskva)", "Moskva (Moskva)", "Moskva")]
    [InlineData("Für (Fuer)", "Fuer (Fuer)", "Fuer")]
    public void FlagsRepeatedRomanisation(string unicode, string romanised, string repeat)
    {
        using var context = CreateTitleContext(romanised, unicode);

        var warning = Assert.Single(context.RunGeneralCheck<CheckRomanisation>());

        Assert.Equal(Issue.Level.Warning, warning.level);
        Assert.Contains($"has \"{repeat}\" twice", warning.message);
    }

    [Fact]
    public void FlagsRepeatedChineseRomanisation()
    {
        using var context = CreateTitleContext(
            "Wo Ai Ni (Wo Ai Ni)",
            "我爱你 (Wo Ai Ni)",
            "chinese"
        );

        var warning = Assert.Single(context.RunGeneralCheck<CheckRomanisation>());

        Assert.Contains("has \"Wo Ai Ni\" twice", warning.message);
    }

    [Fact]
    public void FlagsRepeatedRomanisationInArtist()
    {
        using var context = CreateArtistContext("Tsuki (Tsuki)", "つき (Tsuki)");

        var warning = Assert.Single(context.RunGeneralCheck<CheckRomanisation>());

        Assert.Contains("has \"Tsuki\" twice", warning.message);
    }

    [Theory]
    [InlineData("ソレイユ -Soleil-", "Soleil")]
    [InlineData("Москва (Moskva)", "Moskva")]
    [InlineData("つき つき", "Tsuki Tsuki")]
    [InlineData("つき (つき)", "Tsuki (Tsuki)")]
    [InlineData("つき つき (Tsuki)", "Tsuki Tsuki")]
    [InlineData("Soleil (ソレイユ)", "Soleil")]
    [InlineData("東京 (Tokyo Mix)", "Tokyo (Tokyo Mix)")]
    [InlineData("つき (Moon)", "Tsuki (Moon)")]
    [InlineData("Bye Bye", "Bye Bye")]
    [InlineData("初音ミク (Hatsune Miku Remix)", "Hatsune Miku (Hatsune Miku Remix)")]
    public void DoesNotFlagGenuineRepeats(string unicode, string romanised)
    {
        using var context = CreateTitleContext(romanised, unicode);

        Assert.Empty(context.RunGeneralCheck<CheckRomanisation>());
    }

    private static void AssertUnconfirmed(IEnumerable<Issue> issues)
    {
        var warning = Assert.Single(issues);
        Assert.Equal(Issue.Level.Warning, warning.level);
        Assert.Contains("ensure it's official", warning.message);
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
