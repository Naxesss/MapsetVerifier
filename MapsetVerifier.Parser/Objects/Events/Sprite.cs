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
            Unknown,
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
            Unknown,
        }

        /// <summary> The groups of repeated commands on this sprite. </summary>
        public readonly List<StoryboardLoop> commandLoops = [];

        /// <summary> The commands directly on this sprite, so excluding those in loops and triggers. </summary>
        public readonly List<StoryboardCommand> commands = [];

        public readonly Layer layer;
        public readonly Vector2 offset;
        public readonly Origin origin;
        public readonly string? path;

        /// <summary> The path in lowercase without extension or quotationmarks. </summary>
        public readonly string? strippedPath;

        public readonly List<StoryboardTrigger> triggers = [];

        public Sprite(string[] args)
        {
            layer = GetLayer(args);
            origin = GetOrigin(args);
            path = GetPath(args);
            offset = GetOffset(args);

            strippedPath = PathStatic.ParsePath(path, true);
        }

        /// <summary> Returns the commands as they are grouped in the file: directly on the sprite, per loop and per trigger. </summary>
        public IEnumerable<List<StoryboardCommand>> GetCommandGroups()
        {
            yield return commands;

            foreach (var loop in commandLoops)
                yield return loop.commands;

            foreach (var trigger in triggers)
                yield return trigger.commands;
        }

        /// <summary>
        ///     Returns the time from the first command starting to the last command ending, or null if nothing happens
        ///     without a trigger. What triggers do is left out, since when that happens depends on gameplay.
        /// </summary>
        public (double StartTime, double EndTime)? GetActiveTime()
        {
            var usedLoops = commandLoops.Where(loop => loop.commands.Count > 0).ToList();

            if (commands.Count == 0 && usedLoops.Count == 0)
                return null;

            return (
                commands
                    .Select(command => command.StartTime)
                    .Concat(usedLoops.Select(loop => loop.GetStartTime()))
                    .Min(),
                commands
                    .Select(command => command.EndTime)
                    .Concat(usedLoops.Select(loop => loop.GetEndTime()))
                    .Max()
            );
        }

        /// <summary> Returns the layer which this sprite exists on (e.g. Foreground, Pass, or Overlay). </summary>
        private Layer GetLayer(string[] args) =>
            ParserStatic.GetEnumMatch<Layer>(args[1]) ?? Layer.Unknown;

        /// <summary>
        ///     Returns the local origin of the sprite, determining around which point it is transformed
        ///     (e.g. TopLeft, Center, or Bottom).
        /// </summary>
        private Origin GetOrigin(string[] args) =>
            ParserStatic.GetEnumMatch<Origin>(args[2]) ?? Origin.Unknown;

        /// <summary> Returns the file path which this sprite uses. Retains case sensitivity and extension. </summary>
        private string? GetPath(string[] args) => PathStatic.ParsePath(args[3], retainCase: true);

        /// <summary>
        ///     Returns the positional offset from the top left corner of the screen, if specified,
        ///     otherwise default (320, 240).
        /// </summary>
        private Vector2 GetOffset(string[] args)
        {
            if (args.Length > 4)
                return new Vector2(
                    float.Parse(args[4], CultureInfo.InvariantCulture),
                    float.Parse(args[5], CultureInfo.InvariantCulture)
                );

            // default coordinates
            return new Vector2(320, 240);
        }
    }
}
