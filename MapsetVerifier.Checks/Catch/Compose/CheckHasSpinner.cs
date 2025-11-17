using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Framework.Objects.Attributes;
using MapsetVerifier.Framework.Objects.Metadata;
using MapsetVerifier.Parser.Objects;
using MapsetVerifier.Parser.Objects.HitObjects.Catch;

namespace MapsetVerifier.Checks.Catch.Compose;

[Check]
public class CheckHasSpinner : BeatmapCheck
{
    public override CheckMetadata GetMetadata() => new BeatmapCheckMetadata
    {
        Category = "Compose",
        Message = "Missing spinner.",
        Modes = [Beatmap.Mode.Catch],
        Author = "Greaper",

        Documentation = new Dictionary<string, string>
        {
            {
                "Purpose",
                @"
                    Every difficulty should have a spinner if possible to create fluctuation among scores.
                    However, if a spinner just doesn't fit anywhere in the song, then there's no need to force one."
            },
            {
                "Reasoning",
                @"
                    Scoring in catch is based on the amount of fruits, droplets. Because of this getting 100% accuracy is fairly easy to get.
                    </br>
                    Bananas are an exception since they are the bonus fruit in catch, this means that you won't be punished if you miss any of them, although they get only generated with a spinner."
            }
        }
    };

    public override Dictionary<string, IssueTemplate> GetTemplates()
    {
        return new Dictionary<string, IssueTemplate>
        {
            {"NoSpinner",
                new IssueTemplate(Issue.Level.Minor, "When possible add a spinner to create fluctuation among scores.")
                    .WithCause("No spinner has been added.")
            }
        };
    }

    public override IEnumerable<Issue> GetIssues(Beatmap beatmap)
    {
        if (beatmap.HitObjects.All(x => x is not Bananas))
        {
            yield return new Issue(GetTemplate("NoSpinner"), beatmap);
        }
    }
}