using System.Text;
using MapsetVerifier.Parser.Difficulty;
using MapsetVerifier.Parser.Objects;
using Xunit;

namespace MapsetVerifier.Parser.Tests.Difficulty;

/// <summary>
///     Regression cover for https://github.com/Naxesss/MapsetVerifier/issues/188 - strain charts that
///     didn't match what lazer/PerformanceCalculatorGUI show. The map used here ramps up over time and
///     has a long break in the middle, so a timeline that is ordered by value instead of time, that is
///     truncated, or that holds the last note's difficulty through breaks all fail visibly.
/// </summary>
public class SkillStrainTimelineTests
{
    private const double BreakStartMs = 30_000;
    private const double BreakEndMs = 45_000;
    private const double LastObjectMs = 70_000;

    [Theory]
    [InlineData(Beatmap.Mode.Standard)]
    [InlineData(Beatmap.Mode.Taiko)]
    [InlineData(Beatmap.Mode.Catch)]
    [InlineData(Beatmap.Mode.Mania)]
    public void Timeline_IsChronologicalAndContiguous(Beatmap.Mode mode)
    {
        foreach (var (skill, intervals) in BuildTimelines(mode))
        {
            Assert.NotEmpty(intervals);

            for (var index = 1; index < intervals.Count; index++)
            {
                Assert.True(
                    intervals[index].StartTime >= intervals[index - 1].StartTime,
                    $"{skill}: interval {index} starts at {intervals[index].StartTime}, before its "
                        + $"predecessor at {intervals[index - 1].StartTime}. Strain peaks sorted by "
                        + "value are being read as if they were a timeline."
                );

                Assert.Equal(intervals[index - 1].EndTime, intervals[index].StartTime, 3);
            }
        }
    }

    /// <summary>
    ///     osu!std's Aim (a <c>VariableLengthStrainSkill</c>) discards its lowest strain peaks once a
    ///     map runs past ~44s, so reading them as a timeline used to make the chart stop dead partway
    ///     through. Every skill's timeline must reach the end of the map.
    /// </summary>
    [Theory]
    [InlineData(Beatmap.Mode.Standard)]
    [InlineData(Beatmap.Mode.Taiko)]
    [InlineData(Beatmap.Mode.Catch)]
    [InlineData(Beatmap.Mode.Mania)]
    public void Timeline_ReachesEndOfMap(Beatmap.Mode mode)
    {
        foreach (var (skill, intervals) in BuildTimelines(mode))
            Assert.True(
                intervals[^1].EndTime >= LastObjectMs,
                $"{skill}: timeline ends at {intervals[^1].EndTime}ms but the map runs to {LastObjectMs}ms."
            );
    }

    /// <summary>
    ///     The sorted-peaks bug produced a timeline that started at the map's peak and only ever went
    ///     down. Real difficulty rises and falls with the map; no skill should decay monotonically
    ///     across one with a break and a dense second half in it.
    /// </summary>
    [Theory]
    [InlineData(Beatmap.Mode.Standard)]
    [InlineData(Beatmap.Mode.Taiko)]
    [InlineData(Beatmap.Mode.Catch)]
    [InlineData(Beatmap.Mode.Mania)]
    public void Timeline_DoesNotDecayMonotonically(Beatmap.Mode mode)
    {
        foreach (var (skill, intervals) in BuildTimelines(mode))
            Assert.True(
                intervals.Zip(intervals.Skip(1)).Any(pair => pair.Second.Value > pair.First.Value),
                $"{skill}: every interval is lower than the one before it - the timeline is ordered "
                    + "by value, not by time."
            );
    }

    /// <summary>
    ///     osu!std is the mode the mismatch was reported against, and Aim the skill that regressed.
    ///     The map's dense, jumpy half is at the end, so that is where every std skill should peak.
    /// </summary>
    [Fact]
    public void StandardTimeline_PeaksInTheHarderSecondHalf()
    {
        foreach (var (skill, intervals) in BuildTimelines(Beatmap.Mode.Standard))
        {
            var peak = intervals.MaxBy(interval => interval.Value);

            // The section straddling the break's end counts: skills that reward change legitimately
            // spike on the transition into the dense section.
            Assert.True(
                peak.EndTime > BreakEndMs,
                $"{skill}: peaks at {peak.StartTime}ms, in the sparse first half, even though the "
                    + $"dense section starts at {BreakEndMs}ms."
            );
        }
    }

    [Theory]
    [InlineData(Beatmap.Mode.Standard)]
    [InlineData(Beatmap.Mode.Taiko)]
    [InlineData(Beatmap.Mode.Catch)]
    [InlineData(Beatmap.Mode.Mania)]
    public void Timeline_ReadsZeroThroughBreaks(Beatmap.Mode mode)
    {
        // Only the per-object skills can express a break; section-based strain skills decay across
        // one instead, which is the behaviour lazer itself shows.
        foreach (var (skill, intervals) in BuildTimelines(mode))
        {
            if (intervals.All(interval => interval.Value > 0))
                continue;

            var midBreak = intervals.FirstOrDefault(interval =>
                interval.StartTime <= (BreakStartMs + BreakEndMs) / 2
                && interval.EndTime > (BreakStartMs + BreakEndMs) / 2
            );

            Assert.True(
                midBreak.Value == 0,
                $"{skill}: mid-break difficulty is {midBreak.Value}, not 0 - the last note before the "
                    + "break is being held through it."
            );
        }
    }

    private static List<(string Skill, List<StrainInterval> Intervals)> BuildTimelines(
        Beatmap.Mode mode
    )
    {
        var beatmap = CreateBeatmap(mode);
        beatmap.EnsureTimedAttributesCalculated();

        Assert.NotEmpty(beatmap.Skills);
        Assert.NotEmpty(beatmap.DifficultyObjectTimings);

        return beatmap
            .Skills.Select(skill =>
                (
                    SkillNameFormatter.GetSkillName(skill, beatmap),
                    SkillStrainTimeline.Build(skill, beatmap)
                )
            )
            .Where(entry => entry.Item2.Count > 0)
            .ToList();
    }

    private static Beatmap CreateBeatmap(Beatmap.Mode mode)
    {
        var tempRoot = Path.Combine(
            Path.GetTempPath(),
            "MapsetVerifierTests",
            Guid.NewGuid().ToString()
        );
        Directory.CreateDirectory(tempRoot);

        const string mapPath = "Test.osu";
        var osuCode = BuildOsu(mode);

        File.WriteAllText(Path.Combine(tempRoot, mapPath), osuCode);

        return new Beatmap(osuCode, tempRoot, mapPath);
    }

    private static string BuildOsu(Beatmap.Mode mode)
    {
        var builder = new StringBuilder(
            string.Join(
                "\n",
                "osu file format v14",
                "[General]",
                "AudioFilename: audio.mp3",
                "Mode: " + (int)mode,
                "[Metadata]",
                "Title:Title",
                "Artist:Artist",
                "Creator:Creator",
                "Version:Diff",
                "[Difficulty]",
                "HPDrainRate:5",
                "CircleSize:4",
                "OverallDifficulty:8",
                "ApproachRate:9",
                "SliderMultiplier:1.4",
                "SliderTickRate:1",
                "[Events]",
                $"2,{BreakStartMs},{BreakEndMs}",
                "[TimingPoints]",
                "0,250,4,2,1,50,1,0",
                "[HitObjects]"
            )
        );

        // Sparse, stationary and monotonous up to the break, then a dense section of alternating
        // jumps and alternating taiko colours, so difficulty unambiguously belongs to the second
        // half in every ruleset.
        for (var time = 0d; time < BreakStartMs; time += 1000)
            AppendCircle(builder, mode, time, index: 0);

        for (var time = BreakEndMs; time <= LastObjectMs; time += 125)
            AppendCircle(builder, mode, time, index: (int)(time / 125));

        return builder.ToString();
    }

    private static void AppendCircle(
        StringBuilder builder,
        Beatmap.Mode mode,
        double time,
        int index
    )
    {
        // In mania, x maps to the column; elsewhere it's a position, and alternating it creates the
        // movement the aim/movement skills read.
        var x =
            mode == Beatmap.Mode.Mania ? (index % 4) * 128 + 64
            : index % 2 == 0 ? 64
            : 448;
        // Clap makes a taiko note a kat, so alternating it gives taiko's Colour skill something to
        // read; harmless in the other rulesets.
        var hitSound = index % 2 == 0 ? 0 : 8;
        builder.Append($"\n{x},192,{(int)time},1,{hitSound},0:0:0:0:");
    }
}
