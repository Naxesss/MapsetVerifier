using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Framework.Objects.Attributes;
using MapsetVerifier.Framework.Objects.Metadata;
using MapsetVerifier.Parser.Objects;
using MapsetVerifier.Parser.Objects.HitObjects;
using MapsetVerifier.Parser.Statics;

namespace MapsetVerifier.Checks.Standard.Compose
{
    [Check]
    public class CheckNinjaSpinner : BeatmapCheck
    {
        public override CheckMetadata GetMetadata() =>
            new BeatmapCheckMetadata
            {
                Modes =
                [
                    Beatmap.Mode.Standard
                ],
                Category = "Compose",
                Message = "Too short spinner.",
                Author = "Naxess",

                Documentation = new Dictionary<string, string>
                {
                    {
                        "Purpose",
                        @"
                        Preventing spinners from being so short that you almost need to memorize them in order to react 
                        to them before they end."
                    },
                    {
                        "Reasoning",
                        @"
                        Players generally react much slower than auto, so if auto can't even achieve 1000 points on the 
                        spinner, players will likely not get any points at all, much less pass it without losing accuracy. 
                        In general, these are just not fun to play due to needing to memorize them for a fair experience."
                    }
                }
            };

        public override Dictionary<string, IssueTemplate> GetTemplates() =>
            new()
            {
                {
                    "Problem",
                    new IssueTemplate(Issue.Level.Problem, "{0} Spinner is too short, auto cannot achieve 1000 points on this.", "timestamp -")
                        .WithCause("A spinner is predicted to, based on the OD and BPM, not be able to achieve 1000 points on this, and by a margin to account for any inconsistencies.")
                },

                {
                    "Warning",
                    new IssueTemplate(Issue.Level.Warning, "{0} Spinner may be too short, ensure auto can achieve 1000 points on this.", "timestamp -")
                        .WithCause("Same as the other check, but without the margin, meaning the threshold is lower.")
                }
            };

        public override IEnumerable<Issue> GetIssues(Beatmap beatmap)
        {
            foreach (var hitObject in beatmap.HitObjects)
            {
                if (hitObject is not Spinner spinner)
                    continue;

                double od = beatmap.DifficultySettings.overallDifficulty;

                var warningThreshold = 500 + (od < 5 ? (5 - od) * -21.8 : (od - 5) * 20); // anything above this works fine

                var problemThreshold = 450 + (od < 5 ? (5 - od) * -17 : (od - 5) * 17); // anything above this only works sometimes

                if (problemThreshold > spinner.endTime - spinner.time)
                    yield return new Issue(GetTemplate("Problem"), beatmap, Timestamp.Get(spinner));

                else if (warningThreshold > spinner.endTime - spinner.time)
                    yield return new Issue(GetTemplate("Warning"), beatmap, Timestamp.Get(spinner));
            }
        }
    }
}