using System.Numerics;

namespace MapsetVerifier.Parser.Objects.HitObjects
{
    public class Stackable : HitObject
    {
        public bool isOnSlider;
        public int stackIndex;

        protected Stackable(string[] args, Beatmap beatmap) : base(args, beatmap) { }

        public Vector2 UnstackedPosition => base.Position;
        public override Vector2 Position => GetStackedPosition(base.Position);

        private Vector2 GetStackedPosition(Vector2 position) => new(position.X + GetStackOffset(), position.Y + GetStackOffset());

        private float GetStackOffset() => stackIndex * (beatmap?.DifficultySettings.GetCircleRadius() ?? 0) * -0.1f;
    }
}