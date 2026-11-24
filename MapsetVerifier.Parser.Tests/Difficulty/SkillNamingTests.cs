using MapsetVerifier.Parser.Difficulty;
using osu.Game.Rulesets.Difficulty.Preprocessing;
using osu.Game.Rulesets.Difficulty.Skills;
using Xunit;

namespace MapsetVerifier.Parser.Tests.Difficulty
{
    public class SkillNamingTests
    {
        private static string Name(Skill skill, int mode = 0) =>
            SkillNameFormatter.GetSkillName(skill, TestBeatmap.Parse(TestBeatmap.Code(mode: mode)));

        [Fact]
        public void NameSpaceAfterSkills_IsPartOfTheName()
        {
            Assert.Equal("Reading Old Scroll", Name(new Skills.Reading.OldScrollSkill()));
        }

        [Fact]
        public void NameSpaceNamedLikeTheSkill_IsNotRepeated()
        {
            Assert.Equal("Colour", Name(new Skills.Colour.Colour()));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        public void NameIsSplitIntoWords_WithWhatSetsItApart(int mode)
        {
            Assert.Equal(
                "Tap BPM Speed low (Is Strict, Hand: Left Hand)",
                Name(new TapBPMSpeed_low(), mode)
            );
            Assert.Equal(
                "Tap BPM Speed low (Hand: Both)",
                Name(new TapBPMSpeed_low { IsStrict = false, Hand = HandKind.Both }, mode)
            );
        }

        [Fact]
        public void SkillAlone_KeepsTheWord()
        {
            Assert.Equal("Named Skill", Name(new Skills.Named.Skill()));
        }
    }

    public enum HandKind
    {
        LeftHand,
        Both,
    }

    public class TapBPMSpeed_low() : TestSkill
    {
        public bool IsStrict = true;
        public bool IsWide = false;
        public string? Note = null;
        public HandKind Hand { get; init; }
        public double Weight { get; init; } = 2;
        public int this[int index] => index;
        public int Hidden
        {
            set => _ = value;
        }
    }

    public abstract class TestSkill() : Skill([])
    {
        protected override double ProcessInternal(DifficultyHitObject current) => 0;

        public override double DifficultyValue() => 0;
    }
}

namespace MapsetVerifier.Parser.Tests.Difficulty.Skills.Reading
{
    public class OldScrollSkill : TestSkill;
}

namespace MapsetVerifier.Parser.Tests.Difficulty.Skills.Colour
{
    public class Colour : TestSkill;
}

namespace MapsetVerifier.Parser.Tests.Difficulty.Skills.Named
{
    public class Skill : TestSkill;
}
