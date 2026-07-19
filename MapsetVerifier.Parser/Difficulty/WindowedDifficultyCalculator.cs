using System.Text;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.Formats;
using osu.Game.IO;
using osu.Game.Rulesets.Catch.Difficulty;
using osu.Game.Rulesets.Difficulty;
using osu.Game.Rulesets.Mania.Difficulty;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Osu.Difficulty;
using osu.Game.Rulesets.Taiko.Difficulty;
using Beatmap = MapsetVerifier.Parser.Objects.Beatmap;

namespace MapsetVerifier.Parser.Difficulty;

/// <summary>
///     Recomputes star rating for short, trailing time windows instead of cumulative-from-start.
///     For each window, a sliced-down `.osu` is decoded in-memory (same header/timing/difficulty
///     sections, only hit objects within the window) and run through the real ruleset difficulty
///     calculator, so the numbers are exact rather than an approximation of osu!'s internal formula.
///     Strain history resets at every window boundary, so values dip near a window's start -
///     this is only meant to surface relative difficulty spikes, not to replace the cumulative curve.
/// </summary>
public class WindowedDifficultyCalculator
{
    private static readonly CancellationToken UncancelledToken =
        new CancellationTokenSource().Token;

    public List<(double TimeMs, double StarRating)> CalculateWindowedStarRatings(
        Beatmap mvBeatmap,
        int windowMs,
        int strideMs,
        int maxPoints
    )
    {
        var results = new List<(double, double)>();

        if (mvBeatmap.HitObjects.Count == 0)
            return results;

        var header = GetHeaderUpToHitObjects(mvBeatmap.Code);
        if (header == null)
            return results;

        var lastObjectTime = mvBeatmap.HitObjects[^1].time;
        var pointCount = Math.Min(maxPoints, (int)Math.Ceiling(lastObjectTime / strideMs) + 1);

        for (var index = 0; index < pointCount; index++)
        {
            var windowEnd = index * strideMs;
            var windowStart = windowEnd - windowMs;

            var objectsInWindow = mvBeatmap
                .HitObjects.Where(hitObject =>
                    hitObject.time > windowStart && hitObject.time <= windowEnd
                )
                .ToList();

            var starRating =
                objectsInWindow.Count == 0
                    ? 0
                    : TryCalculateStarRating(mvBeatmap, header, objectsInWindow);

            results.Add((windowEnd, starRating));
        }

        return results;
    }

    private static double TryCalculateStarRating(
        Beatmap mvBeatmap,
        string header,
        List<Objects.HitObject> objectsInWindow
    )
    {
        try
        {
            var sliceText =
                header
                + "\n"
                + string.Join("\n", objectsInWindow.Select(hitObject => hitObject.code));

            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(sliceText));
            using var reader = new LineBufferedReader(stream);

            var decoder = osu.Game.Beatmaps.Formats.Decoder.GetDecoder<osu.Game.Beatmaps.Beatmap>(
                reader
            );
            var slicedBeatmap = decoder.Decode(reader);

            var workingBeatmap = new FlatWorkingBeatmap(slicedBeatmap);
            var ruleset = workingBeatmap.BeatmapInfo.Ruleset.CreateInstance();

            DifficultyCalculator calculator = mvBeatmap.GeneralSettings.mode switch
            {
                Beatmap.Mode.Standard => new OsuDifficultyCalculator(
                    ruleset.RulesetInfo,
                    workingBeatmap
                ),
                Beatmap.Mode.Taiko => new TaikoDifficultyCalculator(
                    ruleset.RulesetInfo,
                    workingBeatmap
                ),
                Beatmap.Mode.Catch => new CatchDifficultyCalculator(
                    ruleset.RulesetInfo,
                    workingBeatmap
                ),
                Beatmap.Mode.Mania => new ManiaDifficultyCalculator(
                    ruleset.RulesetInfo,
                    workingBeatmap
                ),
                _ => throw new ArgumentException("Invalid mode"),
            };

            var attributes = calculator.Calculate(Array.Empty<Mod>(), UncancelledToken);
            return attributes.StarRating;
        }
        catch
        {
            // A slice can occasionally fail to decode/calculate (e.g. a lone spinner with no
            // preceding context); treat it as "no spike data" for this window rather than failing
            // the whole overview.
            return 0;
        }
    }

    private static string? GetHeaderUpToHitObjects(string code)
    {
        var lines = code.Split(["\n"], StringSplitOptions.None);
        var headerLines = new List<string>(lines.Length);

        foreach (var line in lines)
        {
            headerLines.Add(line);
            if (line.Contains("[HitObjects]"))
                return string.Join("\n", headerLines);
        }

        return null;
    }
}
