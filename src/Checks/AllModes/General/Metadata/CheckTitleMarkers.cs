using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Framework.Objects.Attributes;
using MapsetVerifier.Framework.Objects.Metadata;
using MapsetVerifier.Parser.Objects;

namespace MapsetVerifier.Checks.AllModes.General.Metadata
{
    [Check]
    public class CheckTitleMarkers : GeneralCheck
    {
        private static readonly List<MarkerFormat> MarkerFormats = new()
        {
            new MarkerFormat(Marker.TV_SIZE, new Regex(@"(?i)(tv (size|ver))")),
            new MarkerFormat(Marker.GAME_VER, new Regex(@"(?i)(game (size|ver))")),
            new MarkerFormat(Marker.SHORT_VER, new Regex(@"(?i)(short (size|ver))")),
            new MarkerFormat(Marker.CUT_VER, new Regex(@"(?i)(?<!& )(cut (size|ver))")),
            new MarkerFormat(Marker.SPED_UP_VER, new Regex(@"(?i)(?<!& )(sped|speed) ?up ver")),
            new MarkerFormat(Marker.NIGHTCORE_MIX, new Regex(@"(?i)(?<!& )(nightcore|night core) (ver|mix)")),
            new MarkerFormat(Marker.SPED_UP_CUT_VER, new Regex(@"(?i)(sped|speed) ?up (ver)? ?& cut (size|ver)")),
            new MarkerFormat(Marker.NIGHTCORE_CUT_VER, new Regex(@"(?i)(nightcore|night core) (ver|mix)? ?& cut (size|ver)"))
        };

        private static readonly IEnumerable<TitleType> TitleTypes = new[]
        {
            new TitleType("romanized", beatmap => beatmap.MetadataSettings.title),
            new TitleType("unicode", beatmap => beatmap.MetadataSettings.titleUnicode)
        };

        public override CheckMetadata GetMetadata() =>
            new()
            {
                Category = "Metadata",
                Message = "Incorrect format of (TV Size) / (Game Ver.) / (Short Ver.) / (Cut Ver.) / (Sped Up Ver.) / etc in title.",
                Author = "Naxess",

                Documentation = new Dictionary<string, string>
                {
                    {
                        "Purpose",
                        @"
                    Standardizing the way metadata is written for ranked content.
                    <image>
                        https://i.imgur.com/1ozV71n.png
                        A song using ""-TV version-"" as its official metadata, which becomes ""(TV Size)"" when standardized.
                    </image>"
                    },
                    {
                        "Reasoning",
                        @"
                    Small deviations in metadata or obvious mistakes in its formatting or capitalization are for the 
                    most part eliminated through standardization. Standardization also reduces confusion in case of 
                    multiple correct ways to write certain fields and contributes to making metadata more consistent 
                    across official content."
                    }
                }
            };

        public override Dictionary<string, IssueTemplate> GetTemplates() =>
            new()
            {
                {
                    "Problem",
                    new IssueTemplate(Issue.Level.Problem, "{0} title field; \"{1}\" incorrect format of \"{2}\".", "Romanized/unicode", "field", "title marker").WithCause(@"The format of a title marker, in either the romanized or unicode title, is incorrect.
                        The following are detected formats:
                        <ul>
                            <li>(TV Size)</li>
                            <li>(Game Ver.)</li>
                            <li>(Short Ver.)</li>
                            <li>(Cut Ver.)</li>
                            <li>(Sped Up Ver.)</li>
                            <li>(Nightcore Mix)</li>
                            <li>(Sped Up & Cut Ver.)</li>
                            <li>(Nightcore & Cut Ver.)</li>
                        </ul>
                        ")
                },

                {
                    "Warning Nightcore",
                    new IssueTemplate(Issue.Level.Warning, "\"{0}\" in tags, consider \"{1}\" instead of \"{2}\" in {3} title.", "nightcore", "(Nightcore Mix)", "(Sped Up Ver.)", "romanized/unicode").WithCause("The romanized/unicode title contains \"(Sped Up Ver.)\" or equivalent, " + "when the tags contain \"nightcore\".")
                }
            };

        public override IEnumerable<Issue> GetIssues(BeatmapSet beatmapSet)
        {
            var beatmap = beatmapSet.Beatmaps[0];

            foreach (var issue in GetMarkerFormatIssues(beatmap))
                yield return issue;

            foreach (var issue in GetNightcoreIssues(beatmap))
                yield return issue;
        }

        private IEnumerable<Issue> GetMarkerFormatIssues(Beatmap beatmap)
        {
            foreach (var markerFormat in MarkerFormats)
                // Matches any string containing some form of the marker but not exactly it.
                foreach (var issue in GetIssuesFromRegex(beatmap, markerFormat))
                    yield return issue;
        }

        private static string Capitalize(string str) => str.First().ToString().ToUpper() + str[1..];

        /// <summary> Returns issues wherever the romanized or unicode title matches the regex but not the exact format. </summary>
        private IEnumerable<Issue> GetIssuesFromRegex(Beatmap beatmap, MarkerFormat markerFormat)
        {
            foreach (var titleType in TitleTypes)
            {
                var title = titleType.Get(beatmap);
                var correctFormat = markerFormat.marker.Value;
                var approxRegex = markerFormat.incorrectFormatRegex;
                var exactRegex = new Regex(Regex.Escape(correctFormat));

                // Unicode fields do not exist in file version 9, hence null check.
                if (title != null && approxRegex.IsMatch(title) && !exactRegex.IsMatch(title))
                    yield return new Issue(GetTemplate("Problem"), null, Capitalize(titleType.type), title, correctFormat);
            }
        }

        private IEnumerable<Issue> GetNightcoreIssues(Beatmap beatmap)
        {
            var nightcoreTag = beatmap.MetadataSettings.GetCoveringTag("nightcore");

            if (nightcoreTag == null)
                yield break;

            var substitutionPairs = new List<SubstitutionPair>
            {
                new(Marker.SPED_UP_VER, Marker.NIGHTCORE_MIX),
                new(Marker.SPED_UP_CUT_VER, Marker.NIGHTCORE_CUT_VER)
            };

            foreach (var pair in substitutionPairs)
                foreach (var titleType in TitleTypes)
                    if (titleType.Get(beatmap).Contains(pair.original.Value))
                        yield return new Issue(GetTemplate("Warning Nightcore"), null, nightcoreTag, pair.substitution.Value, pair.original.Value, titleType.type);
        }

        private class Marker
        {
            private Marker(string value) => Value = value;

            public string Value { get; }

            public static Marker TV_SIZE => new("(TV Size)");
            public static Marker GAME_VER => new("(Game Ver.)");
            public static Marker SHORT_VER => new("(Short Ver.)");
            public static Marker CUT_VER => new("(Cut Ver.)");
            public static Marker SPED_UP_VER => new("(Sped Up Ver.)");
            public static Marker NIGHTCORE_MIX => new("(Nightcore Mix)");
            public static Marker SPED_UP_CUT_VER => new("(Sped Up & Cut Ver.)");
            public static Marker NIGHTCORE_CUT_VER => new("(Nightcore & Cut Ver.)");
        }

        private readonly struct MarkerFormat
        {
            public readonly Marker marker;
            public readonly Regex incorrectFormatRegex;

            public MarkerFormat(Marker marker, Regex incorrectFormatRegex)
            {
                this.marker = marker;
                this.incorrectFormatRegex = incorrectFormatRegex;
            }
        }

        private readonly struct TitleType
        {
            public readonly string type;
            public readonly Func<Beatmap, string> Get;

            public TitleType(string type, Func<Beatmap, string> Get)
            {
                this.type = type;
                this.Get = Get;
            }
        }

        private readonly struct SubstitutionPair
        {
            public readonly Marker original;
            public readonly Marker substitution;

            public SubstitutionPair(Marker original, Marker substitution)
            {
                this.original = original;
                this.substitution = substitution;
            }
        }
    }
}