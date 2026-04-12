using MapsetVerifier.Parser.Difficulty;
using MapsetVerifier.Parser.Objects;
using osu.Game.Rulesets.Difficulty.Preprocessing;
using osu.Game.Rulesets.Difficulty.Skills;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Osu.Difficulty.Skills;
using osu.Game.Rulesets.Taiko.Difficulty.Skills;
using Xunit;

namespace MapsetVerifier.Parser.Tests.Difficulty
{
    public class SkillNameFormatterTests
    {
        [Fact]
        public void StandardAimSkillsKeepFriendlyNames()
        {
            var beatmap = CreateBeatmap(Beatmap.Mode.Standard);

            Assert.Equal("Aim (with sliders)", SkillNameFormatter.GetSkillName(new Aim(Array.Empty<Mod>(), true), beatmap));
            Assert.Equal("Aim (no sliders)", SkillNameFormatter.GetSkillName(new Aim(Array.Empty<Mod>(), false), beatmap));
        }

        [Fact]
        public void TaikoStaminaSkillsIncludeVariantQualifier()
        {
            var beatmap = CreateBeatmap(Beatmap.Mode.Taiko);

            Assert.Equal("Stamina", SkillNameFormatter.GetSkillName(new Stamina(Array.Empty<Mod>(), false, false), beatmap));
            Assert.Equal("Stamina (Single Colour Stamina)", SkillNameFormatter.GetSkillName(new Stamina(Array.Empty<Mod>(), true, false), beatmap));
        }

        [Fact]
        public void SkillsWithMatchingTypeNamesUseNamespaceContext()
        {
            var beatmap = CreateBeatmap(Beatmap.Mode.Taiko);

            Assert.Equal("Colour Stamina", SkillNameFormatter.GetSkillName(new TestSkills.Skills.Colour.Stamina(), beatmap));
            Assert.Equal("Rhythm Stamina", SkillNameFormatter.GetSkillName(new TestSkills.Skills.Rhythm.Stamina(), beatmap));
        }

        private static Beatmap CreateBeatmap(Beatmap.Mode mode)
        {
            var tempRoot = Path.Combine(Path.GetTempPath(), "MapsetVerifierTests", Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempRoot);

            const string mapPath = "Test.osu";
            var osuCode = BuildOsu(mode);

            File.WriteAllText(Path.Combine(tempRoot, mapPath), osuCode);

            return new Beatmap(osuCode, tempRoot, mapPath);
        }

        private static string BuildOsu(Beatmap.Mode mode) => string.Join("\n", new[]
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
            "[HitObjects]"
        });
    }
}

namespace MapsetVerifier.Parser.Tests.Difficulty.TestSkills.Skills.Colour
{
    public sealed class Stamina : Skill
    {
        public Stamina() : base(Array.Empty<Mod>()) { }

        public override void Process(DifficultyHitObject current) { }
        public override double DifficultyValue() => 0;
    }
}

namespace MapsetVerifier.Parser.Tests.Difficulty.TestSkills.Skills.Rhythm
{
    public sealed class Stamina : Skill
    {
        public Stamina() : base(Array.Empty<Mod>()) { }

        public override void Process(DifficultyHitObject current) { }
        public override double DifficultyValue() => 0;
    }
}