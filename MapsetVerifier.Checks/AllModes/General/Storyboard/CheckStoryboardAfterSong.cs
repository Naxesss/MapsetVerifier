using MapsetVerifier.Checks.Utils;
using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Framework.Objects.Attributes;
using MapsetVerifier.Framework.Objects.Metadata;
using MapsetVerifier.Framework.Objects.Resources;
using MapsetVerifier.Parser.Objects;
using MapsetVerifier.Parser.Objects.Events;
using MapsetVerifier.RankingCriteria;
using Serilog;

namespace MapsetVerifier.Checks.AllModes.General.Storyboard;

[Check]
public class CheckStoryboardAfterSong : GeneralCheck
{
    /// <summary> How long after the song sprites can still be active, in ms. </summary>
    private const double Leniency = 5000;

    public override CheckMetadata GetMetadata() =>
        new()
        {
            Category = "Storyboard",
            Message = "Storyboard continuing after the song.",
            Author = "Greaper",

            Documentation = new Dictionary<string, string>
            {
                {
                    "Purpose",
                    @"
                    Preventing storyboards from continuing long after the song has ended."
                },
                {
                    "Reasoning",
                    @"
                    Sprites which are still active after the song has ended take up performance without anyone getting to see them,
                    and can be a sign that a command or the amount of times a loop repeats is wrong. A few extra seconds are fine, for
                    example to fade out.
                    
                    What happens in triggers is not included, since when that happens depends on the player."
                },
            },
        };

    public override Dictionary<string, IssueTemplate> GetTemplates() =>
        new()
        {
            {
                "After",
                new IssueTemplate(
                    Issue.Level.Warning,
                    "{0} active after the song ends, by up to {1} seconds, see {2}.",
                    "sprites",
                    "seconds",
                    "sprites"
                )
                    .WithCause(
                        "The last command of a sprite or animation ends more than 5 seconds after the audio file does."
                    )
                    .WithRule(RC.General.Storyboarding_NoActiveSpritesCommandsAfter)
            },
        };

    public override IEnumerable<Issue> GetIssues(BeatmapSet beatmapSet)
    {
        foreach (var (beatmap, elements) in StoryboardPatterns.GetStoryboards(beatmapSet))
        {
            if (GetSongEnd(beatmapSet, beatmap) is not { } songEnd)
                continue;

            var late = elements
                .Select(element => (Element: element, Time: element.GetActiveTime()?.EndTime))
                .Where(place => place.Time > songEnd + Leniency)
                .ToList();

            if (late.Count == 0)
                continue;

            var latest = late.Max(place => place.Time!.Value);

            yield return new Issue(
                GetTemplate("After"),
                beatmap,
                late.Count == 1 ? "1 sprite is" : $"{late.Count} sprites are",
                $"{(latest - songEnd) / 1000:0.#}",
                StoryboardPatterns.Describe(
                    late.Select(place => (place.Element, (double?)null)).ToList()
                )
            );
        }
    }

    /// <summary> Returns when the longest song using the storyboard ends, in ms, or null if that can't be determined. </summary>
    private static double? GetSongEnd(BeatmapSet beatmapSet, Beatmap? beatmap)
    {
        double? songEnd = null;

        var audioPaths = (beatmap == null ? beatmapSet.Beatmaps : [beatmap])
            .Select(other => other.GetAudioFilePath())
            .OfType<string>()
            .Distinct();

        foreach (var audioPath in audioPaths)
            try
            {
                songEnd = Math.Max(songEnd ?? 0, AudioFileCache.GetDuration(audioPath));
            }
            catch (Exception exception)
            {
                // Audio which can't be read is reported by other checks.
                Log.Debug(exception, "Couldn't read the duration of {Path}", audioPath);

                return null;
            }

        return songEnd > 0 ? songEnd : null;
    }
}
