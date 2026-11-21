using MapsetVerifier.Checks.Utils.Romanisation;
using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Framework.Objects.Attributes;
using MapsetVerifier.Framework.Objects.Metadata;
using MapsetVerifier.Parser.Objects;
using MapsetVerifier.RankingCriteria;

namespace MapsetVerifier.Checks.AllModes.General.Metadata
{
    [Check]
    public class CheckRomanisation : GeneralCheck
    {
        // Each language the Ranking Criteria gives romanisation rules for gets a romaniser here.
        private static readonly Romaniser[] Romanisers = [new JapaneseRomaniser()];

        private const string NameOrderTemplate = "Name Order";

        public override CheckMetadata GetMetadata() =>
            new()
            {
                Category = "Metadata",
                Message = "Romanisation.",
                Author = "Greaper",

                Documentation = new Dictionary<string, string>
                {
                    {
                        "Purpose",
                        @"
                        Pointing out romanised artists and titles that don't follow the romanisation system the Ranking
                        Criteria asks for, and romanised artist names whose name order is swapped."
                    },
                    {
                        "Reasoning",
                        @"
                        Metadata in other scripts must be romanised with a set system per language, and artist names must
                        keep the order they are printed in the unicode field. Consistent romanisation keeps the same song
                        or artist searchable and recognisable across beatmaps.

                        Official romanisations are used as-is instead, which is why these are warnings rather than problems."
                    },
                    {
                        "Covered languages",
                        @"
                        - **Japanese**, Modified Hepburn: fields written in hiragana or katakana, optionally mixed with plain
                          latin text. Kanji can't be read without a dictionary, so fields with kanji are skipped. Katakana is
                          mostly loan words, which use the source language's spelling, so it is only reported when the
                          romanised field clearly follows another system like Kunrei-shiki (e.g. *tyo* instead of *cho*)."
                    },
                    {
                        "Limitations",
                        @"
                        Only the letters are compared, so spacing, punctuation and capitalisation aren't checked. Readings
                        that can't be told apart are all accepted, such as long vowels with or without the repeated vowel
                        (*ou* or *o*) and particles (*ha* or *wa*)."
                    },
                },
            };

        public override Dictionary<string, IssueTemplate> GetTemplates()
        {
            var templates = Romanisers.ToDictionary(
                romaniser => romaniser.Name,
                romaniser =>
                    new IssueTemplate(
                        Issue.Level.Warning,
                        "Romanised {0} \"{1}\" doesn't match \"{2}\" in "
                            + romaniser.SystemName
                            + ", expected something like \"{3}\". Ignore if this is an official romanisation.",
                        "artist/title",
                        "romanised",
                        "unicode",
                        "suggestion"
                    )
                        .WithCause(romaniser.Cause)
                        .WithRule(romaniser.Rule)
            );

            templates[NameOrderTemplate] = new IssueTemplate(
                Issue.Level.Warning,
                "Romanised artist \"{0}\" swaps the name order of \"{1}\", expected something like \"{2}\". Ignore if this is an official romanisation.",
                "romanised",
                "unicode",
                "suggestion"
            )
                .WithCause(
                    "The romanised artist matches the unicode artist only when its names are put in the opposite order."
                )
                .WithRule(RC.Metadata.Romanisation_ArtistNamesRomanisedOrderPrinted);

            return templates;
        }

        public override IEnumerable<Issue> GetIssues(BeatmapSet beatmapSet)
        {
            var fields = beatmapSet
                .Beatmaps.SelectMany(beatmap =>
                    new[]
                    {
                        (
                            "artist",
                            beatmap.MetadataSettings.artist,
                            beatmap.MetadataSettings.artistUnicode
                        ),
                        (
                            "title",
                            beatmap.MetadataSettings.title,
                            beatmap.MetadataSettings.titleUnicode
                        ),
                    }
                )
                .Distinct();

            foreach (var (field, romanised, unicode) in fields)
            {
                var issue = GetIssue(field, romanised, unicode);
                if (issue != null)
                    yield return issue;
            }
        }

        private Issue? GetIssue(string field, string? romanised, string? unicode)
        {
            if (string.IsNullOrWhiteSpace(romanised) || string.IsNullOrWhiteSpace(unicode))
                return null;

            // Non-ASCII in the romanised field is reported by the unicode check instead.
            if (romanised.Any(ch => ch > 127))
                return null;

            var romaniser = Romanisers.FirstOrDefault(romaniser => romaniser.AppliesTo(unicode));
            if (romaniser == null)
                return null;

            var verdict = romaniser.Judge(unicode, romanised);
            if (verdict is RomanisationVerdict.Unreadable or RomanisationVerdict.Matches)
                return null;

            if (field == "artist" && HasSwappedNameOrder(romaniser, unicode, romanised))
                return new Issue(
                    GetTemplate(NameOrderTemplate),
                    null,
                    romanised,
                    unicode,
                    romaniser.Suggest(unicode)
                );

            if (verdict == RomanisationVerdict.Inconclusive)
                return null;

            return new Issue(
                GetTemplate(romaniser.Name),
                null,
                field,
                romanised,
                unicode,
                romaniser.Suggest(unicode)
            );
        }

        private static bool HasSwappedNameOrder(
            Romaniser romaniser,
            string unicode,
            string romanised
        )
        {
            var names = unicode.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
            if (names.Length < 2)
                return false;

            var reversed = string.Join(" ", names.Reverse());
            return romaniser.Judge(reversed, romanised) == RomanisationVerdict.Matches;
        }
    }
}
