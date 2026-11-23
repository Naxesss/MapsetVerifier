using System.Globalization;
using System.Numerics;
using MapsetVerifier.Parser.Objects.Events;

namespace MapsetVerifier.Parser.Statics
{
    /// <summary> Reads the storyboard part of the events section, for both .osu and .osb files. </summary>
    public static class StoryboardStatic
    {
        /// <summary>
        ///     Returns the sprites and animations in the given lines of an events section, along with their commands.
        ///     Commands which cannot be read are left out.
        /// </summary>
        public static (List<Sprite> Sprites, List<Animation> Animations) ParseStoryboard(
            IEnumerable<string> eventLines
        )
        {
            var sprites = new List<Sprite>();
            var animations = new List<Animation>();

            Sprite? element = null;
            List<StoryboardCommand>? group = null;

            foreach (var line in eventLines)
            {
                var depth = GetDepth(line);

                if (depth == 0)
                {
                    // Comments can appear in the middle of the commands.
                    if (line.StartsWith("//", StringComparison.Ordinal))
                        continue;

                    element = null;
                    group = null;

                    if (IsEvent(line, "Sprite,", "4,"))
                    {
                        element = new Sprite(line.Split(','));
                        sprites.Add(element);
                    }
                    else if (IsEvent(line, "Animation,", "6,"))
                    {
                        var animation = new Animation(line.Split(','));
                        element = animation;
                        animations.Add(animation);
                    }

                    continue;
                }

                if (element == null)
                    continue;

                var args = line[depth..].TrimEnd().Split(',');

                // Only loops and triggers have something nested below them, so going back up always ends those.
                if (depth < 2)
                    group = null;

                switch (args[0])
                {
                    case "L":
                        group = AddLoop(element, args);
                        break;

                    case "T":
                        group = AddTrigger(element, args);
                        break;

                    default:
                        // Commands below a group which couldn't be read have no place to go.
                        if (depth >= 2 && group == null)
                            break;

                        AddCommands(group ?? element.commands, args);
                        break;
                }
            }

            return (sprites, animations);
        }

        private static bool IsEvent(string line, string name, string id) =>
            line.StartsWith(name, StringComparison.Ordinal)
            || line.StartsWith(id, StringComparison.Ordinal);

        private static int GetDepth(string line)
        {
            var depth = 0;

            while (depth < line.Length && line[depth] is ' ' or '_')
                ++depth;

            return depth;
        }

        private static List<StoryboardCommand>? AddLoop(Sprite element, string[] args)
        {
            if (
                args.Length < 3
                || !TryParse(args[1], out var startTime)
                || !int.TryParse(args[2], CultureInfo.InvariantCulture, out var count)
            )
                return null;

            var loop = new StoryboardLoop(startTime, count);
            element.commandLoops.Add(loop);

            return loop.commands;
        }

        private static List<StoryboardCommand>? AddTrigger(Sprite element, string[] args)
        {
            if (args.Length < 2)
                return null;

            double? startTime =
                args.Length > 2 && TryParse(args[2], out var parsedStart) ? parsedStart : null;
            double? endTime =
                args.Length > 3 && TryParse(args[3], out var parsedEnd) ? parsedEnd : null;

            var trigger = new StoryboardTrigger(args[1].Trim(), startTime, endTime);
            element.triggers.Add(trigger);

            return trigger.commands;
        }

        private static void AddCommands(List<StoryboardCommand> commands, string[] args)
        {
            if (
                args.Length < 5
                || GetKind(args[0]) is not { } kind
                || !int.TryParse(args[1], CultureInfo.InvariantCulture, out var easing)
                || !TryParse(args[2], out var startTime)
            )
                return;

            // The end time is left out when the command is instant.
            var endTime = startTime;

            if (!string.IsNullOrWhiteSpace(args[3]) && !TryParse(args[3], out endTime))
                return;

            if (kind == StoryboardCommand.Kind.Parameter)
            {
                if (args[4].Trim() is { Length: 1 } parameter)
                    commands.Add(
                        new StoryboardCommand(
                            kind,
                            easing,
                            startTime,
                            endTime,
                            Vector3.Zero,
                            Vector3.Zero,
                            parameter[0]
                        )
                    );

                return;
            }

            var size = GetValueCount(kind);
            var values = new List<Vector3>();

            for (var i = 4; i + size <= args.Length; i += size)
            {
                var value = Vector3.Zero;

                for (var j = 0; j < size; ++j)
                {
                    if (!TryParse(args[i + j], out var number))
                        return;

                    value[j] = (float)number;
                }

                values.Add(value);
            }

            if (values.Count == 0)
                return;

            // A single value stays the same. More than two are a sequence of commands directly after each other,
            // each taking as long as the first.
            if (values.Count == 1)
                values.Add(values[0]);

            var duration = endTime - startTime;

            for (var i = 0; i < values.Count - 1; ++i)
                commands.Add(
                    new StoryboardCommand(
                        kind,
                        easing,
                        startTime + duration * i,
                        endTime + duration * i,
                        values[i],
                        values[i + 1]
                    )
                );
        }

        private static StoryboardCommand.Kind? GetKind(string name) =>
            name switch
            {
                "F" => StoryboardCommand.Kind.Fade,
                "M" => StoryboardCommand.Kind.Move,
                "MX" => StoryboardCommand.Kind.MoveX,
                "MY" => StoryboardCommand.Kind.MoveY,
                "S" => StoryboardCommand.Kind.Scale,
                "V" => StoryboardCommand.Kind.VectorScale,
                "R" => StoryboardCommand.Kind.Rotate,
                "C" => StoryboardCommand.Kind.Colour,
                "P" => StoryboardCommand.Kind.Parameter,
                _ => null,
            };

        private static int GetValueCount(StoryboardCommand.Kind kind) =>
            kind switch
            {
                StoryboardCommand.Kind.Move or StoryboardCommand.Kind.VectorScale => 2,
                StoryboardCommand.Kind.Colour => 3,
                _ => 1,
            };

        private static bool TryParse(string text, out double value) =>
            double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value)
            && double.IsFinite(value);
    }
}
