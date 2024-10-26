using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Framework.Objects.Attributes;
using MapsetVerifier.Framework.Objects.Metadata;
using MapsetVerifier.Parser.Objects;
using MapsetVerifier.Parser.Settings;

namespace MapsetVerifier.Checks.AllModes.General.Metadata
{
    [Check]
    public class CheckMarkerFormat : GeneralCheck
    {
        private static readonly IEnumerable<Marker> Markers =
        [
            new("vs.", new Regex(@"(?i)( vs\.?[^A-Za-z0-9])"), new Regex(@"vs\.")),
            new("CV:", new Regex(@"(?i)((\(| |（)cv(:|：)?[^A-Za-z0-9])"), new Regex(@"CV(:|：)")),
            new("feat.", new Regex(@"(?i)((\(| |（)(ft|feat)\.?[^A-Za-z0-9])"), new Regex(@"feat\."))
        ];

        public override CheckMetadata GetMetadata() =>
            new()
            {
                Category = "Metadata",
                Message = "Incorrect marker format.",
                Author = "Naxess",

                Documentation = new Dictionary<string, string>
                {
                    {
                        "Purpose",
                        @"
                    Standardizing the way metadata is written for ranked content.
                    <image>
                        https://i.imgur.com/e5mHEan.png
                        An example of ""featured by"", which should be replaced by ""feat."".
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
                    "Wrong Format",
                    new IssueTemplate(Issue.Level.Problem, "Incorrect formatting of \"{0}\" marker in {1} {2} field, \"{3}\".", "marker", "Romanized/unicode", "artist/title", "field").WithCause("The artist or title field of a difficulty includes an incorrect format of \"CV:\", \"vs.\" or \"feat.\".")
                }
            };

        public override IEnumerable<Issue> GetIssues(BeatmapSet beatmapSet)
        {
            if (!beatmapSet.Beatmaps.Any())
                yield break;

            var refBeatmap = beatmapSet.Beatmaps[0];

            foreach (var marker in Markers)
                foreach (var issue in GetFormattingIssues(refBeatmap.MetadataSettings, marker))
                    yield return issue;
        }

        /// <summary> Applies a predicate to all artist and title metadata fields. Yields an issue wherever the predicate is true. </summary>
        private IEnumerable<Issue> GetFormattingIssues(MetadataSettings settings, Marker marker)
        {
            if (marker.IsSimilarButNotExact(settings.artist))
                yield return new Issue(GetTemplate("Wrong Format"), null, marker.name, "Romanized", "artist", settings.artist);

            // Unicode fields do not exist in file version 9.
            if (settings.artistUnicode != null && marker.IsSimilarButNotExact(settings.artistUnicode))
                yield return new Issue(GetTemplate("Wrong Format"), null, marker.name, "Unicode", "artist", settings.artistUnicode);

            if (marker.IsSimilarButNotExact(settings.title))
                yield return new Issue(GetTemplate("Wrong Format"), null, marker.name, "Romanized", "title", settings.title);

            if (settings.titleUnicode != null && marker.IsSimilarButNotExact(settings.titleUnicode))
                yield return new Issue(GetTemplate("Wrong Format"), null, marker.name, "Unicode", "title", settings.titleUnicode);
        }

        private readonly struct Marker
        {
            public readonly string name;
            private readonly Regex approxRegex;
            private readonly Regex exactRegex;

            public Marker(string name, Regex approxRegex, Regex exactRegex)
            {
                this.name = name;
                this.approxRegex = approxRegex;
                this.exactRegex = exactRegex;
            }

            public bool IsSimilarButNotExact(string input) => approxRegex.IsMatch(input) && !exactRegex.IsMatch(input);
        }
    }
}