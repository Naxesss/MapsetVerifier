using Lucene.Net.Analysis.Ja;
using Lucene.Net.Analysis.Ja.Dict;
using Lucene.Net.Analysis.Ja.TokenAttributes;
using Lucene.Net.Analysis.TokenAttributes;

namespace MapsetVerifier.Checks.Utils.Romanisation;

/// <summary> What a word is, as far as romanising it is concerned. </summary>
public enum JapaneseWordKind
{
    Other,

    /// <summary> Particles like の, は and を inside a phrase, which romanisations keep lowercase. </summary>
    Particle,
    Surname,
    GivenName,
}

/// <summary> One word of Japanese text: the text as written, and how it reads in kana. </summary>
/// <param name="Text"> The word as written. </param>
/// <param name="Kana"> The word in kana, or null when it contains kanji whose reading isn't known. </param>
/// <param name="Kind"> What sort of word this is. </param>
public sealed record JapaneseWord(string Text, string? Kana, JapaneseWordKind Kind)
{
    public bool HasKanji => JapaneseReader.ContainsKanji(Text);
}

/// <summary>
///     Splits Japanese text into words and reads kanji as kana, using Kuromoji with the IPADIC dictionary.
///     Kana and everything else is passed through as written, only kanji are replaced with their reading.
/// </summary>
public static class JapaneseReader
{
    // Names the dictionary reads wrong, mostly singing voice synthesizer voicebanks. Kuromoji user dictionary format:
    // text, its words separated by spaces, their readings, and a part of speech.
    private const string NameReadings = """
        鏡音リン,鏡音 リン,カガミネ リン,カスタム人名
        鏡音レン,鏡音 レン,カガミネ レン,カスタム人名
        巡音ルカ,巡音 ルカ,メグリネ ルカ,カスタム人名
        重音テト,重音 テト,カサネ テト,カスタム人名
        神威がくぽ,神威 がくぽ,カムイ ガクポ,カスタム人名
        歌愛ユキ,歌愛 ユキ,カアイ ユキ,カスタム人名
        氷山キヨテル,氷山 キヨテル,ヒヤマ キヨテル,カスタム人名
        音街ウナ,音街 ウナ,オトマチ ウナ,カスタム人名
        結月ゆかり,結月 ゆかり,ユヅキ ユカリ,カスタム人名
        紲星あかり,紲星 あかり,キズナ アカリ,カスタム人名
        東北きりたん,東北 きりたん,トウホク キリタン,カスタム人名
        東北ずん子,東北 ずん子,トウホク ズンコ,カスタム人名
        琴葉茜,琴葉 茜,コトノハ アカネ,カスタム人名
        琴葉葵,琴葉 葵,コトノハ アオイ,カスタム人名
        弦巻マキ,弦巻 マキ,ツルマキ マキ,カスタム人名
        小春六花,小春 六花,コハル リッカ,カスタム人名
        夏色花梨,夏色 花梨,ナツキ カリン,カスタム人名
        花隈千冬,花隈 千冬,ハナクマ チフユ,カスタム人名
        可不,可不,カフ,カスタム人名
        星界,星界,セカイ,カスタム人名
        """;

    private static readonly Lazy<UserDictionary> Names = new(() =>
        new UserDictionary(new StringReader(NameReadings))
    );

    private static bool IsKanji(char ch) =>
        ch is >= '一' and <= '鿿' or >= '㐀' and <= '䶿' or '々' or '〆' or 'ヶ';

    public static bool ContainsKanji(string text) => text.Any(IsKanji);

    /// <summary>
    ///     Splits the text into words. Whatever lies between words, like spaces and punctuation, comes out as its own
    ///     word, so joining all the words' text gives back the input.
    /// </summary>
    public static List<JapaneseWord> Read(string text)
    {
        var words = new List<JapaneseWord>();
        using var tokenizer = new JapaneseTokenizer(
            new StringReader(text),
            Names.Value,
            discardPunctuation: false,
            JapaneseTokenizerMode.NORMAL
        );
        var offset = tokenizer.AddAttribute<IOffsetAttribute>();
        var reading = tokenizer.AddAttribute<IReadingAttribute>();
        var partOfSpeech = tokenizer.AddAttribute<IPartOfSpeechAttribute>();

        tokenizer.Reset();
        var last = 0;
        while (tokenizer.IncrementToken())
        {
            var start = offset.StartOffset;
            var end = offset.EndOffset;
            if (start > last)
                words.Add(
                    new JapaneseWord(text[last..start], text[last..start], JapaneseWordKind.Other)
                );

            var surface = text[start..end];
            var kana = ContainsKanji(surface) ? reading.GetReading() : surface;
            words.Add(new JapaneseWord(surface, kana, KindOf(partOfSpeech.GetPartOfSpeech())));
            last = end;
        }
        tokenizer.End();

        if (last < text.Length)
            words.Add(new JapaneseWord(text[last..], text[last..], JapaneseWordKind.Other));

        return words;
    }

    private static JapaneseWordKind KindOf(string? partOfSpeech) =>
        partOfSpeech switch
        {
            null => JapaneseWordKind.Other,
            // Sentence-ending particles (ね, よ, かな) are left out: they're rare in titles, and かな is also a common name.
            _ when partOfSpeech.StartsWith("助詞") && !partOfSpeech.Contains("終助詞") =>
                JapaneseWordKind.Particle,
            _ when partOfSpeech.EndsWith("人名-姓") => JapaneseWordKind.Surname,
            _ when partOfSpeech.EndsWith("人名-名") => JapaneseWordKind.GivenName,
            _ => JapaneseWordKind.Other,
        };
}
