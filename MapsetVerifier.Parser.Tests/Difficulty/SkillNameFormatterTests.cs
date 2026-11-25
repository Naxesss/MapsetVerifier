using MapsetVerifier.Parser.Difficulty;
using MapsetVerifier.Parser.Objects;
using osu.Game.Rulesets.Difficulty.Skills;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Osu.Difficulty.Skills;
using Xunit;

namespace MapsetVerifier.Parser.Tests.Difficulty;

public class SkillNameFormatterTests
{
    [Fact]
    public void StandardAimSkillsKeepFriendlyNames()
    {
        var beatmap = CreateBeatmap(Beatmap.Mode.Standard);

        Assert.Equal(
            "Aim (with sliders)",
            SkillNameFormatter.GetSkillName(new Aim(Array.Empty<Mod>(), true), beatmap)
        );
        Assert.Equal(
            "Aim (no sliders)",
            SkillNameFormatter.GetSkillName(new Aim(Array.Empty<Mod>(), false), beatmap)
        );
    }

    [Fact]
    public void TaikoStaminaSkillsIncludeVariantQualifier()
    {
        var beatmap = CreateBeatmap(Beatmap.Mode.Taiko);

        Assert.Equal(
            "Stamina",
            SkillNameFormatter.GetSkillName(
                new osu.Game.Rulesets.Taiko.Difficulty.Skills.Stamina(
                    Array.Empty<Mod>(),
                    false,
                    false
                ),
                beatmap
            )
        );
        Assert.Equal(
            "Stamina (Single Colour Stamina)",
            SkillNameFormatter.GetSkillName(
                new osu.Game.Rulesets.Taiko.Difficulty.Skills.Stamina(
                    Array.Empty<Mod>(),
                    true,
                    false
                ),
                beatmap
            )
        );
    }

    private enum Kind
    {
        FirstOne,
    }

    private sealed class CustomSkill() : Skill(Array.Empty<Mod>())
    {
        // Read by the formatter.
#pragma warning disable CA1822
        public Kind? Missing => null;
        public bool IsOff => false;
        public int Number => 3;
        public string this[int index] => index.ToString();
#pragma warning restore CA1822
        public readonly Kind Of = Kind.FirstOne;

        protected override double ProcessInternal(
            osu.Game.Rulesets.Difficulty.Preprocessing.DifficultyHitObject current
        ) => 0;

        public override double DifficultyValue() => 0;
    }

    [Fact]
    public void OtherSkills_AreNamedByTheirTypeAndOptions()
    {
        Assert.Equal(
            "Custom (Of: First One)",
            SkillNameFormatter.GetSkillName(new CustomSkill(), CreateBeatmap(Beatmap.Mode.Catch))
        );
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

    private static string BuildOsu(Beatmap.Mode mode) =>
        string.Join(
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
                "0,500,4,2,0,100,1,0",
                "[HitObjects]",
            }
        );
}
