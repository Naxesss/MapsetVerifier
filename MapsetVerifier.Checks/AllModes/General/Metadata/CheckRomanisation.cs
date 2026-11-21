using System.Text;
using System.Text.RegularExpressions;
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
        private const string MissingTemplate = "Missing";

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
                        Criteria asks for, romanised artist names whose name order is swapped, and romanised fields that
                        leave out part of the unicode field entirely."
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
                        - **Japanese**, Modified Hepburn: fields written in kana and kanji, optionally mixed with plain latin
                          text. Kanji are read with a built-in dictionary. Japanese text is often used for loan words,
                          translations and official names, which keep their own spelling (*かめりあ* is *Camellia*), so a
                          field is only reported when its romanisation clearly follows another system like Kunrei-shiki
                          (e.g. *tyo* instead of *cho*, *tu* instead of *tsu*)."
                    },
                    {
                        "Limitations",
                        @"
                        Only the letters are compared, so spacing, punctuation and capitalisation aren't checked. Readings
                        that can't be told apart are all accepted, such as long vowels with or without the repeated vowel
                        (*ou* or *o*) and particles (*ha* or *wa*).

                        The dictionary picks one reading per word, and it's often wrong for names and creative readings
                        (*未来* sung as *ashita*). A wrong reading doesn't fit any romanisation system, so it leaves the
                        field unreported rather than causing a false warning. Words the dictionary doesn't know at all
                        leave the field unchecked.

                        Name order is checked for artists written with a space between the names, or as a single
                        surname and given name the dictionary recognises (e.g. *鈴木雅之*).

                        A left-out part is found by lining up the latin text both fields share, such as *feat.* or
                        *(TV Size)*. A part that repeats the latin text next to it may be left out, so a part is only
                        reported when it can be read and the text beside it isn't its romanisation."
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

            templates[MissingTemplate] = new IssueTemplate(
                Issue.Level.Warning,
                "Romanised {0} \"{1}\" leaves out \"{2}\" from the unicode {0} \"{3}\".",
                "artist/title",
                "romanised",
                "missing part",
                "unicode"
            ).WithCause(
                "Part of the unicode artist or title is written in a non-latin script, and the romanised field has nothing in its place while the latin text around it matches."
            );

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

            var missing = FindMissingPart(romanised, unicode);
            if (missing != null)
                return new Issue(
                    GetTemplate(MissingTemplate),
                    null,
                    field,
                    romanised,
                    missing,
                    unicode
                );

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

        /// <summary>
        ///     Finds a run of non-latin script in the unicode field that has nothing in its place in the romanised
        ///     field, using the latin text around it as anchors. This doesn't need to read the script, so it works for
        ///     Returns null when nothing is missing, or when the latin text itself differs, since the fields can't be
        ///     lined up then.
        ///
        ///     A word may be left out when it repeats latin text next to it, like "夜に駆ける (Yoru ni Kakeru)"
        ///     romanised as "Yoru ni Kakeru". So a missing run is only reported when a romaniser can read it and the
        ///     latin text beside it isn't its romanisation. Runs nothing can read, such as kanji, aren't reported.
        /// </summary>
        private static string? FindMissingPart(string romanised, string unicode)
        {
            var segments = new List<(bool IsScript, StringBuilder Text)>();
            foreach (var ch in unicode.Normalize(NormalizationForm.FormKC))
            {
                var isLatin = ch < 128 && char.IsLetterOrDigit(ch);
                var isScript = ch >= 128 && char.IsLetter(ch);
                if (isLatin || isScript)
                {
                    if (segments.Count == 0 || segments[^1].IsScript != isScript)
                        segments.Add((isScript, new StringBuilder()));
                    segments[^1].Text.Append(isLatin ? char.ToLowerInvariant(ch) : ch);
                }
                else if (segments.Count > 0 && segments[^1].IsScript)
                    // Keep spacing inside a script run, so the missing part reads as written.
                    segments[^1].Text.Append(ch);
            }

            if (!segments.Any(segment => segment.IsScript))
                return null;

            var target = new string(
                romanised
                    .ToLowerInvariant()
                    .Where(ch => ch is >= 'a' and <= 'z' or >= '0' and <= '9')
                    .ToArray()
            );

            var lenient = Regex.Match(target, BuildPattern(segments, "(.*)"));
            if (!lenient.Success || Regex.IsMatch(target, BuildPattern(segments, "(.+)")))
                return null;

            var group = 1;
            for (var i = 0; i < segments.Count; i++)
            {
                if (!segments[i].IsScript || lenient.Groups[group++].Length > 0)
                    continue;

                // Separators after the run were kept for spacing inside it; drop those around its ends.
                var text = segments[i].Text.ToString();
                var start = 0;
                var end = text.Length;
                while (start < end && !char.IsLetter(text[start]))
                    start++;
                while (end > start && !char.IsLetter(text[end - 1]))
                    end--;
                var run = text[start..end];
                var romaniser = Romanisers.FirstOrDefault(romaniser => romaniser.AppliesTo(run));
                if (romaniser == null)
                    continue;

                var neighbours = new[] { i - 1, i + 1 }
                    .Where(index => index >= 0 && index < segments.Count)
                    .Select(index => segments[index].Text.ToString());
                var repeatsNeighbour = neighbours.Any(neighbour =>
                    romaniser.Judge(run, neighbour)
                        is not (RomanisationVerdict.Inconclusive or RomanisationVerdict.Unreadable)
                );
                if (romaniser.Judge(run, "") != RomanisationVerdict.Unreadable && !repeatsNeighbour)
                    return run;
            }

            return null;
        }

        private static string BuildPattern(
            List<(bool IsScript, StringBuilder Text)> segments,
            string scriptPattern
        ) =>
            "^"
            + string.Concat(
                segments.Select(segment =>
                    segment.IsScript ? scriptPattern : Regex.Escape(segment.Text.ToString())
                )
            )
            + "$";

        private static bool HasSwappedNameOrder(
            Romaniser romaniser,
            string unicode,
            string romanised
        )
        {
            var names = romaniser.SplitNames(unicode);
            if (names.Length < 2)
                return false;

            var reversed = string.Join(" ", names.Reverse());
            return romaniser.Judge(reversed, romanised) == RomanisationVerdict.Matches;
        }
    }
}
