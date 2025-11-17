using System.Globalization;
using System.Numerics;
using MapsetVerifier.Parser.Statics;

namespace MapsetVerifier.Parser.Objects.Events
{
    public class Background
    {
        public readonly Vector2? offset;
        // 0,0,"apple is oral.jpg",0,0
        // Background, offset (unused), filename, x offset, y offset

        public readonly string? path;

        /// <summary> The path in lowercase without extension or quotationmarks. </summary>
        public readonly string? strippedPath;

        public Background(string[] args)
        {
            path = GetPath(args);
            offset = GetOffset(args);

            strippedPath = PathStatic.ParsePath(path, true);
        }

        /// <summary> Returns the file path which this background uses. Retains case and extension. </summary>
        private string? GetPath(string[] args) => PathStatic.ParsePath(args[2], retainCase: true);

        /// <summary>
        ///     Returns the positional offset from the top left corner of the screen, if specified, otherwise null.
        ///     This value is currently unused by the game.
        /// </summary>
        private Vector2? GetOffset(string[] args)
        {
            // Does not exist in file version 9.
            if (args.Length > 4)
                return new Vector2(float.Parse(args[3], CultureInfo.InvariantCulture), float.Parse(args[4], CultureInfo.InvariantCulture));

            return null;
        }
    }
}