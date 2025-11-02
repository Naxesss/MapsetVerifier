using System.Globalization;
using System.Linq;

namespace MapsetVerifier.Parser.Objects.HitObjects
{
    public class Spinner : HitObject
    {
        public readonly double endTime;

        public Spinner(string[] args, Beatmap beatmap) : base(args, beatmap)
        {
            endTime = GetEndTime(args);

            usedHitSamples = GetUsedHitSamples().ToList();
        }

        private double GetEndTime(string[] args) => double.Parse(args[5], CultureInfo.InvariantCulture);
    }
}