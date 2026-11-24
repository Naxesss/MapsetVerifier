using MapsetVerifier.Checks.Utils.Romanisation;
using Xunit;

namespace MapsetVerifier.Checks.Tests.Patterns;

public class RomaniserTests
{
    private const RomanisationVerdict Matches = RomanisationVerdict.Matches;
    private const RomanisationVerdict Unreadable = RomanisationVerdict.Unreadable;

    [Theory]
    // Small kana after one they go with, and on their own.
    [InlineData("しゃ", "sha", Matches)]
    [InlineData("ゃ", "ya", Matches)]
    [InlineData("あゃ", "aya", Matches)]
    [InlineData("あ ゃ", "aya", Matches)]
    [InlineData("うぃ", "wi", Matches)]
    [InlineData("いぇ", "ye", Matches)]
    [InlineData("ぁ", "a", Matches)]
    [InlineData("か ぁ", "kaa", Matches)]
    // Doubled consonants.
    [InlineData("まっちゃ", "matcha", Matches)]
    [InlineData("っあ", "a", Matches)]
    [InlineData("あっ", "at", Matches)]
    [InlineData("あっ!", "a", Matches)]
    // What can't be read.
    [InlineData("あゝ", "aa", Unreadable)]
    [InlineData("あж", "azh", Unreadable)]
    [InlineData("abc", "abc", Unreadable)]
    public void Japanese(string unicode, string romanised, RomanisationVerdict expected)
    {
        Assert.Equal(expected, new JapaneseRomaniser().Judge(unicode, romanised));
    }

    [Fact]
    public void JapaneseWords_KeepWhatIsBetweenThem()
    {
        Assert.Equal(
            "  水瀬 、 ",
            string.Concat(JapaneseReader.Read("  水瀬 、 ").Select(w => w.Text))
        );
        Assert.Empty(JapaneseReader.Read(""));
    }

    [Theory]
    [InlineData("你好", "ni hao", Matches)]
    [InlineData("你好A1", "ni hao a1", Matches)]
    [InlineData("你好ж", "ni hao zh", Unreadable)]
    [InlineData("你好あ", "ni hao a", Unreadable)]
    [InlineData("㐀𰀀", "qiu", Unreadable)]
    public void Chinese(string unicode, string romanised, RomanisationVerdict expected)
    {
        Assert.Equal(expected, new ChineseRomaniser().Judge(unicode, romanised));
    }

    [Theory]
    [InlineData("你好", true)]
    [InlineData("㐀", true)]
    [InlineData("𠀀", true)]
    [InlineData("ж", false)]
    [InlineData("a", false)]
    [InlineData("あ", false)]
    [InlineData("가", false)]
    public void ChineseScript(string text, bool expected)
    {
        Assert.Equal(expected, new ChineseRomaniser().AppliesTo(text));
    }

    [Fact]
    public void ChineseFormat_OfUnreadableText_IsNotJudged()
    {
        Assert.True(new ChineseRomaniser().FollowsFormat("你ж", "anything"));
    }

    [Theory]
    [InlineData("für", "fuer", Matches)]
    [InlineData("café für", "cafe fuer", Matches)]
    [InlineData("für ж", "fuer", Unreadable)]
    [InlineData("fürǝ", "fuere", Unreadable)]
    [InlineData("fürǂ", "fuer", Unreadable)]
    [InlineData("fur", "fur", Unreadable)] // Nothing to romanise.
    public void German(string unicode, string romanised, RomanisationVerdict expected)
    {
        Assert.Equal(expected, LatinRomaniser.German.Judge(unicode, romanised));
    }

    [Theory]
    [InlineData("ł", "l")]
    [InlineData("é", "e")]
    [InlineData("ı", "i")]
    [InlineData("đ", "d")]
    public void OtherLatinLetters_AreTheirNearest(string unicode, string romanised)
    {
        Assert.Equal(Matches, LatinRomaniser.German.Judge("ä" + unicode, "ae" + romanised));
    }
}
