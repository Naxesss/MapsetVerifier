using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Framework.Objects.Attributes;
using MapsetVerifier.Framework.Objects.Metadata;
using MapsetVerifier.Parser.Objects;
using static MapsetVerifier.Checks.Utils.ManiaUtils;

namespace MapsetVerifier.Checks.Mania.Compose
{
    [Check]
    public class CheckColumnDistribution : BeatmapSetCheck
    {
        public override CheckMetadata GetMetadata() =>
            new BeatmapCheckMetadata
            {
                Modes = [Beatmap.Mode.Mania],
                Category = "Compose",
                Message = "Column usage.",
                Author = "Tailsdk",

                Documentation = new Dictionary<string, string>
                {
                    {
                        "Purpose",
                        @"
                    Maps should use all columns somewhat equally."
                    },
                    {
                        "Reasoning",
                        @"
                    Maps that do not use all columns somewhat evenly may have major handbalancing issues."
                    },
                },
            };

        public override Dictionary<string, IssueTemplate> GetTemplates()
        {
            return new Dictionary<string, IssueTemplate>
            {
                {
                    "Underused column warning",
                    new IssueTemplate(
                        Issue.Level.Warning,
                        "Column {0} is underused",
                        "column"
                    ).WithCause("A column is being underused.")
                },
                {
                    "Overused column warning",
                    new IssueTemplate(
                        Issue.Level.Warning,
                        "Column {0} is overused",
                        "column"
                    ).WithCause("A column is being overused.")
                },
                {
                    "Underused column problem",
                    new IssueTemplate(
                        Issue.Level.Problem,
                        "Column {0} is severely underused",
                        "column"
                    ).WithCause("A column is being severely underused.")
                },
                {
                    "Overused column problem",
                    new IssueTemplate(
                        Issue.Level.Problem,
                        "Column {0} is severely overused",
                        "column"
                    ).WithCause("A column is being severely overused.")
                },
                {
                    "Unused column",
                    new IssueTemplate(
                        Issue.Level.Problem,
                        "Column {0} is unused",
                        "column"
                    ).WithCause("A column is unused.")
                },
            };
        }

        public override IEnumerable<Issue> GetIssues(BeatmapSet beatmapSet)
        {
            foreach (Beatmap beatmap in beatmapSet.Beatmaps)
            {
                if (beatmap.GeneralSettings.mode != Beatmap.Mode.Mania)
                {
                    continue;
                }

                int keys = (int)beatmap.DifficultySettings.circleSize;
                int totalNotes = 0;
                int[] columnDistribution = new int[keys];
                foreach (var hitObject in beatmap.HitObjects)
                {
                    columnDistribution[getColumn(hitObject, keys)] += 1;
                    totalNotes += 1;
                }

                int averageNotes = totalNotes / keys;
                int belowAverageNotes = (int)(averageNotes * 0.8);
                int aboveAverageNotes = (int)(averageNotes * 1.2);
                int sigBelowAverageNotes = (int)(averageNotes * 0.65);
                int sigAboveAverageNotes = (int)(averageNotes * 1.35);

                for (int i = 0; i < columnDistribution.Length; i++)
                {
                    if (columnDistribution[i] == 0)
                    {
                        yield return new Issue(GetTemplate("Unused column"), beatmap, i + 1);
                    }
                    else if (columnDistribution[i] >= aboveAverageNotes)
                    {
                        yield return new Issue(
                            GetTemplate("Overused column warning"),
                            beatmap,
                            i + 1
                        );
                    }
                    else if (columnDistribution[i] <= belowAverageNotes)
                    {
                        yield return new Issue(
                            GetTemplate("Underused column warning"),
                            beatmap,
                            i + 1
                        );
                    }
                    else if (columnDistribution[i] >= sigAboveAverageNotes)
                    {
                        yield return new Issue(
                            GetTemplate("Overused column problem"),
                            beatmap,
                            i + 1
                        );
                    }
                    else if (columnDistribution[i] <= sigBelowAverageNotes)
                    {
                        yield return new Issue(
                            GetTemplate("Underused column problem"),
                            beatmap,
                            i + 1
                        );
                    }
                }
            }
        }
    }
}
