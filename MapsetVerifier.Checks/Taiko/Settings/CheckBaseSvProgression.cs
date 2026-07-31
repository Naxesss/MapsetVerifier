using System.Globalization;
using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Framework.Objects.Attributes;
using MapsetVerifier.Framework.Objects.Metadata;
using MapsetVerifier.Parser.Objects;

namespace MapsetVerifier.Checks.Taiko.Settings;

[Check]
public class CheckBaseSvProgression : BeatmapSetCheck
{
    private const string Warning = nameof(Issue.Level.Warning);

    /// <summary>
    ///     Base SV values rounded to this many decimals are treated as equal,
    ///     matching how SliderMultiplier is typically displayed.
    /// </summary>
    private const int SvDecimals = 2;

    public override CheckMetadata GetMetadata() =>
        new BeatmapCheckMetadata
        {
            Modes = [Beatmap.Mode.Taiko],
            Category = "Settings",
            Message = "Illogical base SV progression.",
            Author = "Hivie",
            Documentation = new Dictionary<string, string>
            {
                {
                    "Purpose",
                    @"
                    Ensuring base slider velocity follows a one-directional progression across osu!taiko difficulties."
                },
                {
                    "Reasoning",
                    @"
                    High-BPM sets often lower base SV on easier difficulties, while double-BPM sets often raise it on
                    harder ones. Low-SV gimmick sets may decrease base SV as difficulty increases. Equal values are fine.
                    A zig-zag (decreasing then increasing, or the reverse) is usually unintentional and should be checked."
                },
            },
        };

    public override Dictionary<string, IssueTemplate> GetTemplates() =>
        new()
        {
            {
                Warning,
                new IssueTemplate(
                    Issue.Level.Warning,
                    "{0} Base SV {1}x breaks the set's progression from {2}x on {3}. Ensure this is intentional.",
                    "version",
                    "currentSV",
                    "previousSV",
                    "previousVersion"
                ).WithCause(
                    "SliderMultiplier does not follow a one-directional progression when difficulties are ordered by interpreted difficulty and star rating."
                )
            },
        };

    public override IEnumerable<Issue> GetIssues(BeatmapSet beatmapSet)
    {
        var taikoBeatmaps = beatmapSet
            .Beatmaps.Where(beatmap => beatmap.GeneralSettings.mode == Beatmap.Mode.Taiko)
            .OrderBy(beatmap => beatmap.GetDifficulty())
            .ThenBy(beatmap => beatmap.StarRating)
            .ToList();

        // Fewer than 3 diffs cannot zig-zag under min-violation direction selection.
        if (taikoBeatmaps.Count < 3)
            yield break;

        var values = taikoBeatmaps
            .Select(beatmap => RoundSv(beatmap.DifficultySettings.sliderMultiplier))
            .ToList();

        var ascendingViolations = 0;
        var descendingViolations = 0;

        for (var i = 1; i < values.Count; i++)
        {
            if (values[i] < values[i - 1])
                ascendingViolations++;
            else if (values[i] > values[i - 1])
                descendingViolations++;
        }

        // Prefer ascending on ties (more common than low-SV gimmick sets).
        var preferAscending = ascendingViolations <= descendingViolations;

        for (var i = 1; i < taikoBeatmaps.Count; i++)
        {
            var prevSv = values[i - 1];
            var currSv = values[i];

            var breaksAscending = currSv < prevSv;
            var breaksDescending = currSv > prevSv;

            if (preferAscending ? !breaksAscending : !breaksDescending)
                continue;

            var curr = taikoBeatmaps[i];
            var prev = taikoBeatmaps[i - 1];

            yield return new Issue(
                GetTemplate(Warning),
                curr,
                curr.MetadataSettings.version,
                FormatSv(currSv),
                FormatSv(prevSv),
                prev.MetadataSettings.version
            );
        }
    }

    private static double RoundSv(float sliderMultiplier) =>
        Math.Round(sliderMultiplier, SvDecimals, MidpointRounding.AwayFromZero);

    private static string FormatSv(double sv) =>
        sv.ToString($"F{SvDecimals}", CultureInfo.InvariantCulture);
}
