using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Framework.Objects.Attributes;
using MapsetVerifier.Framework.Objects.Metadata;
using MapsetVerifier.Parser.Objects;
using MapsetVerifier.Parser.Objects.HitObjects;
using MapsetVerifier.Parser.Statics;

using static MapsetVerifier.Checks.Utils.TaikoUtils;

namespace MapsetVerifier.Checks.Taiko.Compose
{
    [Check]
    public class CheckLastNoteHidingBarline : BeatmapCheck
    {
        private const string Minor = nameof(Issue.Level.Minor);
        private const string Problem = nameof(Issue.Level.Problem);
        private const string RoundingErrorWarning = "roundingErrorWarning";

        public override CheckMetadata GetMetadata() =>
            new BeatmapCheckMetadata()
            {
                Author = "Nostril",
                Category = "Compose",
                Message = "Unsnapped last note hiding barline",
                Modes = [Beatmap.Mode.Taiko],
                Documentation = new Dictionary<string, string>()
                {
                    {
                        "Purpose",
                        @"
                    Check if the last object in the beatmap is not 1ms earlier than a barline."
                    },
                    {
                        "Reasoning",
                        @"
                    This causes the last barline in the map to not be rendered."
                    }
                }
            };

        public override Dictionary<string, IssueTemplate> GetTemplates() =>
            new Dictionary<string, IssueTemplate>()
            {
                {
                    Minor,

                    new IssueTemplate(
                        Issue.Level.Minor,
                        "{0} Last spinner/slider end in the map is hiding its barline, due to being unsnapped 1ms early",
                        "timestamp - "
                    ).WithCause("The spinner/slider end is unsnapped 1ms early.")
                },
                {
                    Problem,

                    new IssueTemplate(
                        Issue.Level.Problem,
                        "{0} Last note in the map is hiding its barline, due to being unsnapped 1ms early",
                        "timestamp - "
                    ).WithCause("The note is unsnapped 1ms early.")
                },
                {
                    RoundingErrorWarning,

                    new IssueTemplate(
                        Issue.Level.Warning,
                        "{0} Last note in the map may have its barline hidden, due to rounding error. Doublecheck manually.",
                        "timestamp - "
                    ).WithCause("Rounding error.")
                }
            };

        public override IEnumerable<Issue> GetIssues(Beatmap beatmap)
        {
            if (beatmap.HitObjects.Count == 0)
            {
                yield break;
            }

            var lastObject = beatmap.HitObjects.Last();

            var unsnapFromLastBarline = lastObject.GetTailOffsetFromNextBarlineMs();

            if (unsnapFromLastBarline > -1.0 && unsnapFromLastBarline <= -1.0 + Common.ROUNDING_ERROR_MARGIN)
            {
                yield return new Issue(
                    GetTemplate(RoundingErrorWarning),
                    beatmap,
                    Timestamp.Get(lastObject.GetEndTime())
                );
            }
            else if (unsnapFromLastBarline > -2.0 && unsnapFromLastBarline <= -1.0)
            {
                if (lastObject is Circle)
                {
                    yield return new Issue(
                        GetTemplate(Problem),
                        beatmap,
                        Timestamp.Get(lastObject.GetEndTime())
                    );
                }
                else
                {
                    yield return new Issue(
                        GetTemplate(Minor),
                        beatmap,
                        Timestamp.Get(lastObject.GetEndTime())
                    );
                }
            }
        }
    }
}
