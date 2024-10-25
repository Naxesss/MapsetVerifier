using System.Globalization;

namespace MapsetVerifier.Parser.Objects.HitObjects
{
    public class HoldNote : HitObject
    {
        // 448,192,243437,128,2,247861:0:0:0:0:
        // x, y, time, typeFlags, hitsound, endTime:extras

        public readonly double endTime;

        public HoldNote(string[] args, Beatmap beatmap) : base(args, beatmap) =>
            endTime = GetEndTime(args);

        private double GetEndTime(string[] args) => double.Parse(args[5].Split(':')[0], CultureInfo.InvariantCulture);
    }
}