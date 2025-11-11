using System.Globalization;

namespace MapsetVerifier.Parser.Objects.TimingLines
{
    public class UninheritedLine : TimingLine
    {
        public readonly double bpm;
        public readonly double msPerBeat;

        public UninheritedLine(string[] args, Beatmap beatmap) : base(args, beatmap)
        {
            msPerBeat = GetMsPerBeat(args);

            bpm = GetBpm();
        }

        /// <summary> Returns the miliseconds per beat of the uninherited line. </summary>
        private double GetMsPerBeat(string[] args) => double.Parse(args[1], CultureInfo.InvariantCulture);

        /// <summary> Returns the beats per minute (BPM) of the uninherited line. </summary>
        private double GetBpm() => 60000 / msPerBeat;

        /// <summary>
        /// Scales the bpm in accordance to https://osu.ppy.sh/help/wiki/Ranking_Criteria/osu!/Scaling_BPM,
        /// where 180 bpm is 1, 120 bpm is 0.5, and 240 bpm is 2.
        /// </summary>
        public double GetScaledBpm() => Math.Pow(bpm, 2) / 14400 - bpm / 80 + 1;
    }
}