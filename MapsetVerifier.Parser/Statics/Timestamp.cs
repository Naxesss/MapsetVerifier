using System.Globalization;
using MapsetVerifier.Parser.Objects;
using MathNet.Numerics;

namespace MapsetVerifier.Parser.Statics
{
    public static class Timestamp
    {
        /// <summary> Returns the given time as an integer in the way the game rounds time values. </summary>
        /// <remarks>
        ///     Interestingly, the game currently does not round, but rather cast to integer. This may
        ///     change in future versions of the game to fix issues such as 1 ms rounding errors when
        ///     copying objects, however.
        /// </remarks>
        public static int Round(double time) => (int)time;

        /// <summary> Returns the timestamp of a given time. If decimal, is rounded in the same way the game rounds. </summary>
        public static string Get(double time) => GetTimestamp(time);

        /// <summary> Returns the timestamp of given hit objects, so the timestamp includes the object(s). </summary>
        public static string Get(params HitObject[] hitObjects) => GetTimestamp(hitObjects[0].beatmap, hitObjects);

        private static string GetTimestamp(double time)
        {
            double miliseconds = Round(time);

            // For negative timestamps we simply post the raw offset (e.g. "-14 -").
            if (miliseconds < 0)
                return miliseconds + " - ";

            double minutes = 0;

            while (miliseconds >= 60000)
            {
                miliseconds -= 60000;
                ++minutes;
            }

            double seconds = 0;

            while (miliseconds >= 1000)
            {
                miliseconds -= 1000;
                ++seconds;
            }

            var minuteString = minutes >= 10 ? minutes.ToString(CultureInfo.InvariantCulture) : "0" + minutes;
            var secondString = seconds >= 10 ? seconds.ToString(CultureInfo.InvariantCulture) : "0" + seconds;

            var milisecondsString = miliseconds >= 100 ? miliseconds.ToString(CultureInfo.InvariantCulture) :
                miliseconds >= 10 ? "0" + miliseconds : "00" + miliseconds;

            return minuteString + ":" + secondString + ":" + milisecondsString + " - ";
        }

        private static string GetTimestamp(Beatmap beatmap, params HitObject[] hitObjects)
        {
            var timestamp = GetTimestamp(hitObjects[0].time);
            timestamp = timestamp.Substring(0, timestamp.Length - 3);

            var objects = "";

            foreach (var hitObject in hitObjects)
            {
                string objectRef;

                if (beatmap.GeneralSettings.mode == Beatmap.Mode.Mania)
                {
                    var row = hitObject.Position.X.AlmostEqual(64) ? 0 :
                        hitObject.Position.X.AlmostEqual(192) ? 1 :
                        hitObject.Position.X.AlmostEqual(320) ? 2 :
                        hitObject.Position.X.AlmostEqual(448) ? 3 : -1;

                    objectRef = hitObject.time + "|" + row;
                }
                else
                {
                    objectRef = beatmap.GetCombo(hitObject).ToString();
                }

                objects += (objects.Length > 0 ? "," : "") + objectRef;
            }

            return timestamp + " (" + objects + ") - ";
        }
    }
}