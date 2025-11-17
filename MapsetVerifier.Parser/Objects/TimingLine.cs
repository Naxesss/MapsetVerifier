using System;
using System.Globalization;
using System.Linq;
using MathNet.Numerics;

namespace MapsetVerifier.Parser.Objects
{
    public class TimingLine
    {
        [Flags]
        public enum Types
        {
            Kiai = 1,
            OmitBarLine = 8
        }

        public int CustomIndex { get; }
        public bool Kiai { get; }
        public int Meter { get; } // this exists for both green and red lines but only red uses it

        public double Offset { get; }
        public bool OmitsBarLine { get; }

        public HitSample.SamplesetType Sampleset { get; }

        private Types Type { get; }
        public bool Uninherited { get; }

        public float Volume { get; }
        // 440,476.190476190476,4,2,1,40,1,0
        // offset, msPerBeat, meter, sampleset, customIndex, volume, inherited, kiai

        private Beatmap Beatmap { get; }
        public string Code { get; }

        // might not be explicit (depending on inherited or not)
        public float SvMult { get; }
        private int TimingLineIndex { get; set; }

        public TimingLine(string[] args, Beatmap beatmap)
        {
            Beatmap = beatmap;
            Code = string.Join(",", args);

            Offset = GetOffset(args);
            Meter = GetMeter(args);
            Sampleset = GetSampleset(args);
            CustomIndex = GetCustomIndex(args);
            Volume = GetVolume(args);
            Uninherited = IsUninherited(args);

            Type = GetType(args);
            Kiai = Type.HasFlag(Types.Kiai);
            OmitsBarLine = Type.HasFlag(Types.OmitBarLine);

            // may not be explicit
            SvMult = GetSvMult(args);
        }

        /// <summary> Returns the offset of the line. </summary>
        private double GetOffset(string[] args) => double.Parse(args[0], CultureInfo.InvariantCulture);

        /// <summary> Returns the meter (i.e. timing signature) of the line. </summary>
        private int GetMeter(string[] args) => int.Parse(args[2]);

        /// <summary> Returns the sampleset which this line applies to any sample set to Auto sampleset. </summary>
        private HitSample.SamplesetType GetSampleset(string[] args) => (HitSample.SamplesetType)int.Parse(args[3]);

        /// <summary> Returns the custom sample index of the line. </summary>
        private int GetCustomIndex(string[] args) => int.Parse(args[4]);

        /// <summary> Returns the sample volume of the line. </summary>
        private float GetVolume(string[] args) => float.Parse(args[5], CultureInfo.InvariantCulture);

        /// <summary> Returns whether a line of code representing a timing line is uninherited or inherited. </summary>
        public static bool IsUninherited(string[] args)
        {
            // Does not exist in file version 5.
            if (args.Length > 6)
                return args[6] == "1";

            return true;
        }

        /// <summary> Returns whether kiai is enabled for this line. </summary>
        private Types GetType(string[] args)
        {
            // Does not exist in file version 5.
            if (args.Length > 7)
                return (Types)int.Parse(args[7]);

            return 0;
        }

        /// <summary> Returns the slider velocity multiplier (1 for uninherited lines). Fit into range 0.1 - 10 before returning. </summary>
        public float GetSvMult(string[] args)
        {
            if (!IsUninherited(args))
            {
                var svMult = 1 / (float.Parse(args[1], CultureInfo.InvariantCulture) * -0.01f);

                // Min 0.1x, max 10x.
                if (svMult > 10f) svMult = 10f;
                if (svMult < 0.1f) svMult = 0.1f;

                return svMult;
            }

            return 1;
        }

        /// <summary> Returns the index of this timing line in the beatmap's timing line list, O(1). </summary>
        public int GetTimingLineIndex() => TimingLineIndex;

        /// <summary>
        ///     Sets the index of this timing line. This should reflect the index in the timing line list of the beatmap.
        ///     Only use this if you're changing the order of lines or adding new ones after parsing.
        /// </summary>
        public void SetTimingLineIndex(int index) => TimingLineIndex = index;

        /// <summary>
        ///     Returns the next timing line in the timing line list, if any,
        ///     otherwise null, O(1). Optionally skips concurrent lines.
        /// </summary>
        public TimingLine? Next(bool skipConcurrent = false)
        {
            TimingLine? next = null;

            for (var i = TimingLineIndex + 1; i < Beatmap.TimingLines.Count; ++i)
            {
                next = Beatmap.TimingLines[i];

                if (!skipConcurrent || !next.Offset.AlmostEqual(Offset))
                    break;
            }

            return next;
        }

        /// <summary>
        ///     Returns the previous timing line in the timing line list, if any,
        ///     otherwise null, O(1). Optionally skips concurrent objects.
        /// </summary>
        public TimingLine? Prev(bool skipConcurrent = false)
        {
            TimingLine? prev = null;

            for (var i = TimingLineIndex - 1; i >= 0; --i)
            {
                prev = Beatmap.TimingLines[i];

                if (!skipConcurrent || !prev.Offset.AlmostEqual(Offset))
                    break;
            }

            return prev;
        }
    }
}