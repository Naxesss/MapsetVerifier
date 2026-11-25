using MapsetVerifier.Checks.Utils.Romanisation;
using Xunit;

namespace MapsetVerifier.Checks.Tests.Patterns;

public class RomanisationCorrectionTests
{
    [Theory]
    // Ukrainian, where some letters differ at the start of a word.
    [InlineData("Їжак", "Yizhak")]
    [InlineData("Київ", "Kyiv")]
    [InlineData("Єва", "Yeva")]
    [InlineData("Моє", "Moie")]
    [InlineData("Буряї", "Buriai")]
    // Bulgarian and South Slavic.
    [InlineData("България", "Balgaria")]
    [InlineData("Гълъб", "Galab")]
    [InlineData("Његош", "Njegos")]
    // Neither, so as Russian.
    [InlineData("Земля", "Zemlya")]
    public void Cyrillic_IsSuggestedByItsLanguage(string unicode, string expected)
    {
        Assert.Equal(expected, new CyrillicRomaniser().Suggest(unicode));
        Assert.Equal(RomanisationVerdict.Matches, new CyrillicRomaniser().Judge(unicode, expected));
    }

    [Theory]
    // Ukrainian by one of its words.
    [InlineData("Єва і Юлія", "Yeva i Yuliia", true)]
    [InlineData("Яна і Йосип", "Yana i Yosyp", true)]
    [InlineData("Моє Люблю", "Moie Liubliu", true)]
    [InlineData("Гай і Чай", "Hai i Chai", true)]
    [InlineData("Згода є", "Zghoda ye", true)]
    [InlineData("Небо і Земля", "Nebo i Zemlia", true)]
    // The same, if nothing tells.
    [InlineData("Чай", "Chai", false)]
    [InlineData("Земля", "Zemlia", false)]
    [InlineData("Згода", "Zghoda", false)]
    // Or if something tells it's not.
    [InlineData("Земля і ы", "Zemlia i y", false)]
    [InlineData("Яна и Гълъб", "Yana i Galab", true)]
    public void Cyrillic_IsAcceptedByItsLanguage(string unicode, string romanised, bool expected)
    {
        Assert.Equal(
            expected,
            new CyrillicRomaniser().Judge(unicode, romanised) == RomanisationVerdict.Matches
        );

        // What's suggested is what's accepted.
        if (expected)
            Assert.Equal(romanised, new CyrillicRomaniser().Suggest(unicode));
    }

    [Theory]
    [InlineData("Мѯ", "Mks")]
    [InlineData("Мӿ", "Mkh")]
    public void CyrillicLettersWithoutARule_CannotBeRead(string unicode, string romanised)
    {
        var romaniser = new CyrillicRomaniser();

        Assert.Equal(RomanisationVerdict.Unreadable, romaniser.Judge(unicode, romanised));
        Assert.Null(romaniser.Correct(unicode, romanised));
        Assert.Equal("", romaniser.Suggest(unicode));
    }

    [Fact]
    public void UnreadableText_HasNoCorrection()
    {
        Assert.Null(new JapaneseRomaniser().Correct("あж", "Azh"));
        Assert.Null(new ChineseRomaniser().Correct("你ж", "Ni Zh"));
        Assert.Null(LatinRomaniser.German.Correct("für ж", "fuer zh"));
    }

    [Theory]
    [InlineData("Moksva", "Moskva", 1)] // Swapped.
    [InlineData("Moskvaa", "Moskva", 1)]
    [InlineData("Moska", "Moskva", 1)]
    [InlineData("Mosbva", "Moskva", 1)]
    [InlineData("oskva", "moskva", 1)]
    [InlineData("MOSKA", "MOSKVA", 1)]
    [InlineData("", "moskva", 6)]
    [InlineData("!", "moskva!", 6)]
    public void Correction_ChangesAsFewLettersAsPossible(
        string romanised,
        string expected,
        int edits
    )
    {
        var correction = new CyrillicRomaniser().Correct("Москва", romanised)!;

        Assert.Equal(expected, correction.Text);
        Assert.Equal(edits, correction.Edits);
    }

    [Theory]
    [InlineData("ab", "ba")]
    [InlineData("Ab", "Ba")]
    [InlineData("a b", "")]
    [InlineData("1a", "")]
    [InlineData("a", "")]
    [InlineData("", "")]
    public void Swaps_KeepCapitalsInPlace(string romanised, string expected)
    {
        Assert.Equal(
            expected,
            string.Concat(RomanisationCorrection.Swaps(romanised).Select(swap => swap.Text))
        );
    }

    private static List<RomanisationWord> Word(int end, params string[] alternatives) =>
        [new RomanisationWord(end, [new RomanisationUnit('x', [.. alternatives])])];

    [Fact]
    public void Search_PicksTheClosestOfAllWays()
    {
        Assert.Equal(
            ["love", "song"],
            RomanisationSearch.FindClosest(
                [Word(1, "ai", "love"), Word(2, "uta", "song")],
                "lovsong"
            )
        );
        // Of those equally close, the first.
        Assert.Equal(["ai"], RomanisationSearch.FindClosest([Word(1, "ai", "bi")], "ci"));
        // Words without letters are left out.
        Assert.Equal(["ai"], RomanisationSearch.FindClosest([Word(1, ""), Word(2, "ai")], "ai"));
        Assert.Equal(["ai"], RomanisationSearch.FindClosest([Word(1, "ai")], ""));
    }

    [Fact]
    public void Search_WithoutAWayThrough_FindsNothing()
    {
        Assert.Null(RomanisationSearch.FindClosest([Word(1, "ai"), []], "ai"));
        Assert.Null(
            RomanisationSearch.FindClosest(
                [
                    [],
                    Word(2, "ai"),
                ],
                "ai"
            )
        );
        Assert.Empty(RomanisationSearch.FindClosest([], "ai")!);
    }

    private static JapaneseWordGraph Graph(string text) =>
        new(
            text,
            kana =>
                kana == "あい"
                    ? [new RomanisationUnit('あ', ["a"]), new RomanisationUnit('い', ["i"])]
                    : null
        );

    [Theory]
    [InlineData("あい", "ai", true)]
    [InlineData("あい", "ia", false)]
    [InlineData("あい×あい", "aixai", true)]
    [InlineData("あい×あい", "aiai", true)]
    [InlineData("あい、あい", "aiai", true)]
    [InlineData("A あい", "aai", true)]
    [InlineData("1 あい", "ai", true)]
    [InlineData("ж あい", "ai", false)] // Can't be read.
    [InlineData("あいう", "aiu", false)]
    public void WordGraph_MatchesTheWholeText(string text, string letters, bool expected)
    {
        Assert.Equal(expected, Graph(text).Matches(letters));
    }

    [Fact]
    public void WordGraph_WithSomethingUnreadable_HasNoClosest()
    {
        Assert.Null(Graph("жあい").FindClosest("ai"));
        Assert.Equal(["ai"], Graph("あい").FindClosest("a"));
    }
}
