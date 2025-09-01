using System.Globalization;
using MapsetVerifier.Parser.Statics;

namespace MapsetVerifier.Parser.Objects.Events
{
    public class Sample
    {
        /// <summary> The layer the hit sound is audible on, for example only when passing a section if "Pass". </summary>
        public enum Layer
        {
            Background = 0,
            Fail = 1,
            Pass = 2,
            Foreground = 3,
            Unknown
        }

        public readonly Layer layer;
        public readonly string path;

        /// <summary> The path in lowercase without extension or quotationmarks. </summary>
        public readonly string strippedPath;
        // Sample,15707,0,"drum-hitnormal.wav",60
        // Sample, time, layer, path, volume

        public readonly double time;
        public readonly float volume;

        public Sample(string[] args)
        {
            time = GetTime(args);
            layer = GetLayer(args);
            path = GetPath(args);
            volume = GetVolume(args);

            strippedPath = PathStatic.ParsePath(path, true);
        }

        /// <summary> Returns after how many miliseconds this storyboard sample will play. </summary>
        private double GetTime(string[] args) => double.Parse(args[1], CultureInfo.InvariantCulture);

        /// <summary> Returns on which layer the storyboard sample will play (e.g. Fail or Pass). </summary>
        private Layer GetLayer(string[] args) => ParserStatic.GetEnumMatch<Layer>(args[2]) ?? Layer.Unknown;

        /// <summary> Returns the file path which this sample uses. Retains case and extension. </summary>
        private string GetPath(string[] args) => PathStatic.ParsePath(args[3], retainCase: true);

        /// <summary> Returns the volume percentage (0-100) that this sample will play at. </summary>
        private float GetVolume(string[] args)
        {
            // Does not exist in file version 5.
            if (args.Length > 4)
                return float.Parse(args[4], CultureInfo.InvariantCulture);

            // 100% volume is default.
            return 100.0f;
        }
    }
}