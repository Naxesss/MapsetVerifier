namespace MapsetVerifier.Parser.Objects.HitObjects.Taiko
{
    public static class TaikoExtensions
    {
        public static bool IsDon(this Circle circle) => circle.hitSound != HitObject.HitSounds.Clap && circle.hitSound != HitObject.HitSounds.Whistle;

        public static bool IsFinisher(this HitObject hitObject) => hitObject.HasHitSound(HitObject.HitSounds.Finish);
    }
}