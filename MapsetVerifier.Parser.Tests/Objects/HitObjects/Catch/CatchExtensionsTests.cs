using System.Numerics;
using MapsetVerifier.Parser.Objects;
using MapsetVerifier.Parser.Objects.HitObjects.Catch;
using Xunit;

namespace MapsetVerifier.Parser.Tests.Objects.HitObjects.Catch
{
    public class CatchExtensionsTests
    {
        // Minimal stub for ICatchHitObject
        private class TestCatchHitObject : ICatchHitObject
        {
            public double Time { get; set; }
            public Vector2 Position { get; set; }
            public float DistanceToHyper { get; set; }
            public CatchMovementType MovementType { get; set; }
            public CatchNoteDirection NoteDirection { get; set; }
            public string GetNoteTypeName() => "Test";
        }

        [Theory]
        // Salad (Normal) - Dash
        [InlineData(250, CatchMovementType.Dash, Beatmap.Difficulty.Normal, false)] // basic-snapped
        [InlineData(249, CatchMovementType.Dash, Beatmap.Difficulty.Normal, true)]  // higher-snapped
        [InlineData(125, CatchMovementType.Dash, Beatmap.Difficulty.Normal, true)]  // higher-snapped
        [InlineData(124, CatchMovementType.Dash, Beatmap.Difficulty.Normal, false)] // below higher-snapped
        // Platter (Hard) - Dash
        [InlineData(125, CatchMovementType.Dash, Beatmap.Difficulty.Hard, false)] // basic-snapped
        [InlineData(124, CatchMovementType.Dash, Beatmap.Difficulty.Hard, true)]  // higher-snapped
        [InlineData(62, CatchMovementType.Dash, Beatmap.Difficulty.Hard, true)]   // higher-snapped
        [InlineData(61, CatchMovementType.Dash, Beatmap.Difficulty.Hard, false)]  // below higher-snapped
        // Platter (Hard) - Hyperdash
        [InlineData(250, CatchMovementType.Hyperdash, Beatmap.Difficulty.Hard, false)] // basic-snapped
        [InlineData(249, CatchMovementType.Hyperdash, Beatmap.Difficulty.Hard, true)]  // higher-snapped
        [InlineData(125, CatchMovementType.Hyperdash, Beatmap.Difficulty.Hard, true)]  // higher-snapped
        [InlineData(124, CatchMovementType.Hyperdash, Beatmap.Difficulty.Hard, false)] // below higher-snapped
        // Rain (Insane) - Dash
        [InlineData(125, CatchMovementType.Dash, Beatmap.Difficulty.Insane, false)] // basic-snapped
        [InlineData(124, CatchMovementType.Dash, Beatmap.Difficulty.Insane, true)]  // higher-snapped
        [InlineData(62, CatchMovementType.Dash, Beatmap.Difficulty.Insane, true)]   // higher-snapped
        [InlineData(61, CatchMovementType.Dash, Beatmap.Difficulty.Insane, false)]  // below higher-snapped
        // Rain (Insane) - Hyperdash
        [InlineData(125, CatchMovementType.Hyperdash, Beatmap.Difficulty.Insane, false)] // basic-snapped
        [InlineData(124, CatchMovementType.Hyperdash, Beatmap.Difficulty.Insane, true)]  // higher-snapped
        [InlineData(62, CatchMovementType.Hyperdash, Beatmap.Difficulty.Insane, true)]   // higher-snapped
        [InlineData(61, CatchMovementType.Hyperdash, Beatmap.Difficulty.Insane, false)]  // below higher-snapped
        // Cup (Easy) - all not applicable
        [InlineData(200, CatchMovementType.Dash, Beatmap.Difficulty.Easy, false)]
        [InlineData(200, CatchMovementType.Hyperdash, Beatmap.Difficulty.Easy, false)]
        // Overdose (Expert) - all not applicable
        [InlineData(200, CatchMovementType.Dash, Beatmap.Difficulty.Expert, false)]
        [InlineData(200, CatchMovementType.Hyperdash, Beatmap.Difficulty.Expert, false)]
        public void IsHigherSnapped_SnappingReferenceTable(float ms, CatchMovementType movementType, Beatmap.Difficulty difficulty, bool expected)
        {
            var current = new TestCatchHitObject { Time = ms, MovementType = movementType };
            var next = new TestCatchHitObject { Time = 0 };
            var result = current.IsHigherSnapped(next, difficulty);
            Assert.Equal(expected, result);
        }
    }
}
