using System.Globalization;
using System.Numerics;
using MapsetVerifier.Parser.Statics;

namespace MapsetVerifier.Parser.Objects.Events
{
    public class Sprite
    {
        // Sprite,Foreground,Centre,"SB\whitenamebar.png",320,240
        // Sprite, layer, origin, filename, x offset, y offset

        public enum Layer
        {
            Background = 0,
            Fail = 1,
            Pass = 2,
            Foreground = 3,
            Overlay = 4,
            Unknown
        }

        public enum Origin
        {
            TopLeft = 0,
            Centre = 1,
            CentreLeft = 2,
            TopRight = 3,
            BottomCentre = 4,
            TopCentre = 5,
            Custom = 6,
            CentreRight = 7,
            BottomLeft = 8,
            BottomRight = 9,
            Unknown
        }

        public readonly Layer layer;
        public readonly Vector2 offset;
        public readonly Origin origin;
        public readonly string path;

        /// <summary> The path in lowercase without extension or quotationmarks. </summary>
        public readonly string strippedPath;

        public Sprite(string[] args)
        {
            layer = GetLayer(args);
            origin = GetOrigin(args);
            path = GetPath(args);
            offset = GetOffset(args);

            strippedPath = PathStatic.ParsePath(path, true);
        }

        /// <summary> Returns the layer which this sprite exists on (e.g. Foreground, Pass, or Overlay). </summary>
        private Layer GetLayer(string[] args) => ParserStatic.GetEnumMatch<Layer>(args[1]) ?? Layer.Unknown;

        /// <summary>
        ///     Returns the local origin of the sprite, determining around which point it is transformed
        ///     (e.g. TopLeft, Center, or Bottom).
        /// </summary>
        private Origin GetOrigin(string[] args) => ParserStatic.GetEnumMatch<Origin>(args[2]) ?? Origin.Unknown;

        /// <summary> Returns the file path which this sprite uses. Retains case sensitivity and extension. </summary>
        private string GetPath(string[] args) => PathStatic.ParsePath(args[3], retainCase: true);

        /// <summary>
        ///     Returns the positional offset from the top left corner of the screen, if specified,
        ///     otherwise default (320, 240).
        /// </summary>
        private Vector2 GetOffset(string[] args)
        {
            if (args.Length > 4)
                return new Vector2(float.Parse(args[4], CultureInfo.InvariantCulture), float.Parse(args[5], CultureInfo.InvariantCulture));

            // default coordinates
            return new Vector2(320, 240);
        }
    }
}