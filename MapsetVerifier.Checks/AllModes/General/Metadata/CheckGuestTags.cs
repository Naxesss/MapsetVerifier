using System.Text.RegularExpressions;
using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Framework.Objects.Attributes;
using MapsetVerifier.Framework.Objects.Metadata;
using MapsetVerifier.Parser.Objects;

namespace MapsetVerifier.Checks.AllModes.General.Metadata
{
    [Check]
    public class CheckGuestTags : GeneralCheck
    {
        private readonly char[] collabChars = [',', '&', '|', '/'];
        private readonly Regex possessorRegex = new(@"(.+)(?:'s|(s)')", RegexOptions.IgnoreCase);
        private readonly Dictionary<char, char> wrappingBrackets = new()
        {
            ['('] = ')',
            ['['] = ']',
            ['{'] = '}',
        };

        public override CheckMetadata GetMetadata() =>
            new()
            {
                Category = "Metadata",
                Message = "Missing GDers in tags.",
                Author = "Naxess",

                Documentation = new Dictionary<string, string>
                {
                    {
                        "Purpose",
                        @"
                        Ensuring that the set can be found by searching for any of its guest difficulty creators."
                    },
                    {
                        "Reasoning",
                        @"
                        If you're looking for beatmaps of a specific user, it'd make sense if sets containing their
                        guest difficulties would appear, and not only sets they hosted."
                    },
                },
            };

        public override Dictionary<string, IssueTemplate> GetTemplates() =>
            new()
            {
                {
                    "Warning",
                    new IssueTemplate(
                        Issue.Level.Warning,
                        "\"{0}\" is possessive but \"{1}\" isn't in the tags, ignore if not a user.",
                        "guest's diff",
                        "guest"
                    ).WithCause(
                        "A difficulty name is prefixed by text containing an apostrophe (') before or after the character \"s\", which is not in the tags."
                    )
                },
            };

        public override IEnumerable<Issue> GetIssues(BeatmapSet beatmapSet)
        {
            foreach (var beatmap in beatmapSet.Beatmaps)
            foreach (var possessor in GetAllPossessors(beatmap.MetadataSettings.version))
            {
                var possessorTag = BuildPossessorTag(possessor);

                if (
                    beatmap.MetadataSettings.IsCoveredByTags(possessorTag)
                    || beatmap.MetadataSettings.creator.ToLower() == possessor.ToLower()
                )
                    continue;

                yield return new Issue(
                    GetTemplate("Warning"),
                    null,
                    beatmap.MetadataSettings.version,
                    possessorTag
                );
            }
        }

        private string BuildPossessorTag(string possessor)
        {
            var components = possessor.Split(" ");
            bool hasSingleCharacterComponents = components.Any(p => p.Length == 1);

            return hasSingleCharacterComponents
                ? string.Join("_", components).ToLower()
                : possessor.ToLower();
        }

        /// <summary>
        ///     Returns any text before the last apostrophe with a neighbouring "s".
        ///     E.g. "Naxess' & Greaper" when inputting "Naxess' & Greaper's Nor'mal".
        /// </summary>
        private string? GetPossessor(string text)
        {
            Match match;

            try
            {
                match = possessorRegex.Match(text);
            }
            catch (Exception)
            {
                return null;
            }

            // e.g. "Alphabet" in "Alphabet's Normal"
            var possessor = match.Groups[1].Value;

            if (match.Groups.Count > 2)
                // If e.g. "Naxess' Insane", group 1 is "Naxes" and group 2 is the remaining "s".
                possessor += match.Groups[2].Value;

            // Strip leading brackets that wrap the whole difficulty name (e.g. "(" in
            // "(Take's Hard)"), but keep them if unbalanced, since they're then likely part of
            // a stylized username instead (e.g. "[[[[[['s Easy").
            possessor = StripWrappingBrackets(possessor, text);

            return possessor.Trim();
        }

        /// <summary>
        ///     Strips leading bracket characters from <paramref name="possessor" /> as long as
        ///     they have a matching, balanced closing bracket somewhere in the full <paramref name="text" />.
        /// </summary>
        private string StripWrappingBrackets(string possessor, string text)
        {
            while (
                possessor.Length > 0
                && wrappingBrackets.TryGetValue(possessor[0], out var closeChar)
            )
            {
                var openCount = text.Count(c => c == possessor[0]);
                var closeCount = text.Count(c => c == closeChar);

                if (openCount != closeCount)
                    break;

                possessor = possessor[1..];
            }

            return possessor;
        }

        /// <summary>
        ///     Returns all possessors in the given text. E.g. "Naxess', Greaper's &
        ///     Someone else's Normal" returns "Naxess", "Greaper", and "Someone else".
        /// </summary>
        private IEnumerable<string> GetAllPossessors(string text)
        {
            return text.Split(collabChars)
                .Select(GetPossessor)
                .Where(p => !string.IsNullOrEmpty(p))
                .OfType<string>();
        }
    }
}
