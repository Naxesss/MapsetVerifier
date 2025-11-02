using System.Globalization;

namespace MapsetVerifier.Parser.Objects.Events
{
    public class Break
    {
        public readonly double endTime;
        // 2,66281,71774
        // 2, start, end

        /*  Notes:
         *  - Assuming no extensions of pre- and post break times or rounding errors,
         *      - pre break is 200 ms long
         *      - post break is 525 ms long
         *  - Saving the beatmap corrects any abnormal break times
         *  - Abnormal break times do not show up in the editor, but do in gameplay.
         */

        public readonly double time;

        public Break(string[] args)
        {
            time = GetTime(args);
            endTime = GetEndTime(args);
        }

        /// <summary>
        ///     Returns the visual start time of the break.
        ///     See <see cref="GetRealStart(Beatmap)" /> for where HP stops draining.
        /// </summary>
        private double GetTime(string[] args) => double.Parse(args[1], CultureInfo.InvariantCulture);

        /// <summary>
        ///     Returns the visual end time of the break.
        ///     See <see cref="GetRealEnd(Beatmap)" /> for where HP starts draining again.
        /// </summary>
        private double GetEndTime(string[] args) => double.Parse(args[2], CultureInfo.InvariantCulture);

        /// <summary>
        ///     Returns the duration between the end of the object before the break and the start of the
        ///     object after it. During this time, no health will be drained.
        /// </summary>
        public double GetDuration(Beatmap beatmap) => GetRealEnd(beatmap) - GetRealStart(beatmap);

        /// <summary> Returns the end time of the object before the break. </summary>
        public double GetRealStart(Beatmap beatmap) => beatmap.GetPrevHitObject(time).GetEndTime();

        /// <summary> Returns the start time of the object after the break, if any, otherwise the end of the map. </summary>
        public double GetRealEnd(Beatmap beatmap) => beatmap.GetNextHitObject(endTime)?.time ?? beatmap.GetPlayTime();
    }
}