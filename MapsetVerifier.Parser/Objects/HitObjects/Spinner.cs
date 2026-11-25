namespace MapsetVerifier.Parser.Objects.HitObjects
{
    public class Spinner : HitObject
    {
        public readonly double endTime;

        public Spinner(string[] args, Beatmap beatmap)
            : base(args, beatmap)
        {
            endTime = GetEndTime(args);

            usedHitSamples = GetUsedHitSamples().ToList();
        }

        private double GetEndTime(string[] args) => ParseTime(args[5]);
    }
}
