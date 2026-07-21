using MapsetVerifier.Parser.Objects;
using osu.Game.Rulesets.Difficulty.Skills;
using Xunit;

namespace MapsetVerifier.Parser.Tests.Difficulty;

/// <summary>
///     Canary for BeatmapAnalysisService's skill-sampling dispatch (GetSkillDifficultySamples):
///     every skill must expose either GetCurrentStrainPeaks (StrainSkill/VariableLengthStrainSkill)
///     or GetObjectDifficulties (all skills) so the overview charts have something to show. Rulesets
///     have previously moved skills off StrainSkill (e.g. osu!std's Speed/Reading onto a
///     HarmonicSkill base with no strain-peaks concept) without warning; this catches a future skill
///     type that loses both.
/// </summary>
public class SkillDifficultySourceTests
{
    [Theory]
    [InlineData(0)] // Standard
    [InlineData(1)] // Taiko
    [InlineData(2)] // Catch
    [InlineData(3)] // Mania
    public void EveryDifficultySkill_ExposesStrainPeaksOrObjectDifficulties(int mode)
    {
        var beatmap = CreateBeatmap((Beatmap.Mode)mode);

        beatmap.EnsureTimedAttributesCalculated();

        Assert.NotEmpty(beatmap.Skills);

        foreach (var skill in beatmap.Skills)
        {
            var hasStrainPeaks = skill is StrainSkill or VariableLengthStrainSkill;
            var hasObjectDifficulties = skill.GetObjectDifficulties().Count > 0;

            Assert.True(
                hasStrainPeaks || hasObjectDifficulties,
                $"Skill {skill.GetType().FullName} exposes neither strain peaks nor object difficulties - "
                    + "BeatmapAnalysisService.GetSkillDifficultySamples would silently show nothing for it."
            );
        }
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
        var header = string.Join(
            "\n",
            new[]
            {
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
                "OverallDifficulty:5",
                "ApproachRate:5",
                "SliderMultiplier:1.4",
                "SliderTickRate:1",
                "[TimingPoints]",
                "0,333.333333333333,4,2,1,50,1,0",
                "[HitObjects]",
            }
        );

        var hitObjects = new System.Text.StringBuilder(header);
        for (var time = 0; time < 5000; time += 300)
            hitObjects.Append($"\n{(time % 600 == 0 ? 100 : 300)},192,{time},1,0,0:0:0:0:");

        return hitObjects.ToString();
    }
}
