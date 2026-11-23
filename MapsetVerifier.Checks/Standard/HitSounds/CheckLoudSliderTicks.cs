using System.Globalization;
using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Framework.Objects.Attributes;
using MapsetVerifier.Framework.Objects.Metadata;
using MapsetVerifier.Framework.Objects.Resources;
using MapsetVerifier.Parser.Objects;
using MapsetVerifier.Parser.Objects.HitObjects;
using MapsetVerifier.Parser.Statics;
using MapsetVerifier.RankingCriteria;
using Serilog;

namespace MapsetVerifier.Checks.Standard.HitSounds;

[Check]
public class CheckLoudSliderTicks : BeatmapCheck
{
    /// <summary> Ticks at least this loud compared to the hitsounds of their slider aren't notably quieter. </summary>
    private const double MinRelativeLoudness = 0.75;

    private const int MaxTimestamps = 5;

    /// <summary> Loudness is measured over the loudest part of a sound, as very short sounds seem quieter. </summary>
    private const int LoudnessWindowMs = 50;

    /// <summary> In the order the game looks for them. </summary>
    private static readonly string[] Extensions = [".wav", ".mp3", ".ogg"];

    public override CheckMetadata GetMetadata() =>
        new BeatmapCheckMetadata
        {
            Modes = [Beatmap.Mode.Standard],
            Difficulties =
            [
                Beatmap.Difficulty.Hard,
                Beatmap.Difficulty.Insane,
                Beatmap.Difficulty.Expert,
                Beatmap.Difficulty.Ultra,
            ],
            Category = "Hit Sounds",
            Message = "Loud slider tick hitsounds.",
            Author = "Greaper",

            Documentation = new Dictionary<string, string>
            {
                {
                    "Purpose",
                    @"
                    Pointing out custom slider tick sounds that are as loud as the regular hitsounds."
                },
                {
                    "Reasoning",
                    @"
                    Players don't do anything for slider ticks, so a sound as loud as a hitsound feels out of place there. This is
                    especially jarring when it only happens once or twice in a difficulty.

                    The loudness of a tick, with its volume taken into account, is compared to the loudest hitsound on the head,
                    reverses and tail of its slider. Ticks at 75% or more are reported. Only custom sounds can be compared, so
                    sliders using the hitsounds of the player's own skin are left out."
                },
            },
        };

    public override Dictionary<string, IssueTemplate> GetTemplates() =>
        new()
        {
            {
                "Loud",
                new IssueTemplate(
                    Issue.Level.Warning,
                    "{0} Slider tick sound \"{1}\" is up to {2}% as loud as the hitsounds of its slider, used {3} time(s).",
                    "timestamp -",
                    "file name",
                    "percent",
                    "amount"
                )
                    .WithCause(
                        "A custom slider tick sound is at least 75% as loud as the hitsounds of the slider it's on."
                    )
                    .WithRule(
                        RC.Osu.Hard_SliderTickHitsoundsDiscouraged,
                        RC.Osu.Insane_SliderTickHitsoundsDiscouraged,
                        RC.Osu.Expert_SliderTickHitsoundsDiscouraged
                    )
            },
        };

    public override IEnumerable<Issue> GetIssues(Beatmap beatmap)
    {
        if (beatmap.IsDifficultyGuessed())
            yield break;

        var files = GetSoundFiles(beatmap.SongPath);

        // Most sets don't have any, which saves decoding the audio.
        if (
            !files.Keys.Any(name => name.Contains("slidertick", StringComparison.OrdinalIgnoreCase))
        )
            yield break;

        var fileLoudness = new Dictionary<string, double>();
        var ticks = new List<(string File, double Time, double Relative)>();

        foreach (var slider in beatmap.HitObjects.OfType<Slider>())
        {
            double? edgeLoudness = null;

            foreach (
                var tick in slider.usedHitSamples.Where(sample =>
                    sample.HitSource == HitSample.HitSourceType.Tick
                )
            )
            {
                var tickLoudness = GetPlayedLoudness(tick);

                if (tickLoudness <= 0)
                    continue;

                edgeLoudness ??= slider
                    .usedHitSamples.Where(sample =>
                        sample is { HitSource: HitSample.HitSourceType.Edge, Taiko: false }
                    )
                    .Select(GetPlayedLoudness)
                    .DefaultIfEmpty(0)
                    .Max();

                var relative = edgeLoudness > 0 ? tickLoudness / edgeLoudness.Value : 0;

                if (relative >= MinRelativeLoudness)
                    ticks.Add((Path.GetFileName(files[tick.GetFileName()!]), tick.Time, relative));
            }
        }

        foreach (var group in ticks.GroupBy(tick => tick.File))
        {
            var timestamps = group
                .Take(MaxTimestamps)
                .Select(tick => Timestamp.Get(tick.Time).Trim());
            var percent = group.Max(tick => tick.Relative) * 100;

            yield return new Issue(
                GetTemplate("Loud"),
                beatmap,
                string.Join(" ", timestamps) + (group.Count() > MaxTimestamps ? " ..." : ""),
                group.Key,
                percent.ToString("0", CultureInfo.InvariantCulture),
                group.Count()
            );
        }

        yield break;

        // 0 if it's silent or not part of the set.
        double GetPlayedLoudness(HitSample sample)
        {
            // Without a custom index, the sounds of the player's skin are used.
            if (
                sample.CustomIndex < 1
                || sample.GetFileName() is not { } name
                || !files.TryGetValue(name, out var path)
            )
                return 0;

            if (!fileLoudness.TryGetValue(path, out var loudness))
                fileLoudness[path] = loudness = GetFileLoudness(path);

            var line = beatmap.GetTimingLine(
                sample.Time,
                hitSoundLeniency: true,
                isLeniencyForSliderTick: sample.HitSource == HitSample.HitSourceType.Tick
            );

            // Volumes below 5% play at 5%.
            return loudness * Math.Max(line?.Volume ?? 100, 5) / 100;
        }
    }

    private static Dictionary<string, string> GetSoundFiles(string songPath)
    {
        var files = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var extension in Extensions)
        foreach (var path in Directory.EnumerateFiles(songPath, "*" + extension))
            files.TryAdd(Path.GetFileNameWithoutExtension(path), path);

        return files;
    }

    private static double GetFileLoudness(string path)
    {
        try
        {
            // Files with no more than a header have no sound, as used for silencing.
            return new FileInfo(path).Length <= 44 ? 0 : GetLoudness(AudioFileCache.GetPeaks(path));
        }
        catch (Exception exception)
        {
            // Files that can't be read are reported by the hitsound file checks.
            Log.Error(exception, "Couldn't check hit sound file");

            return 0;
        }
    }

    /// <summary> The level of the loudest part of a sound, given its peaks in every millisecond. </summary>
    internal static double GetLoudness(IReadOnlyList<float[]> peaks)
    {
        double sum = 0;
        double loudest = 0;

        for (var i = 0; i < peaks.Count; i++)
        {
            sum += Power(i);

            if (i >= LoudnessWindowMs)
                sum -= Power(i - LoudnessWindowMs);

            loudest = Math.Max(loudest, sum);
        }

        return Math.Sqrt(loudest / LoudnessWindowMs);

        double Power(int index)
        {
            var level = peaks[index].Select(Math.Abs).DefaultIfEmpty(0).Max();

            return (double)level * level;
        }
    }
}
