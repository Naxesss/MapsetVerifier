using System.Numerics;
using System.Text.RegularExpressions;
using MapsetVerifier.Parser.Objects;
using MapsetVerifier.Parser.Objects.Events;
using MapsetVerifier.Parser.Statics;

namespace MapsetVerifier.Checks.Utils
{
    /// <summary> Finds things in storyboards that the ranking criteria advises against. </summary>
    public static partial class StoryboardPatterns
    {
        /// <summary> How many loop repetitions are looked at individually, keeping loops of extreme lengths cheap. </summary>
        private const int MaxLoopRepetitions = 1000;

        /// <summary> The longest pattern, in commands of one type, that is looked for when finding repeats. </summary>
        private const int MaxRepeatPattern = 4;

        // The visible area with widescreen support enabled, in osu!pixels.
        private const float ScreenLeft = -107;
        private const float ScreenRight = 747;
        private const float ScreenTop = 0;
        private const float ScreenBottom = 480;

        /// <summary>
        ///     Returns the sprites and animations of every storyboard in the set. The difficulty is null for the .osb file.
        /// </summary>
        public static IEnumerable<(Beatmap? Beatmap, List<Sprite> Elements)> GetStoryboards(
            BeatmapSet beatmapSet
        )
        {
            foreach (var beatmap in beatmapSet.Beatmaps)
                if (beatmap.Sprites.Count > 0 || beatmap.Animations.Count > 0)
                    yield return (beatmap, [.. beatmap.Sprites, .. beatmap.Animations]);

            if (beatmapSet.Osb is { } osb && (osb.sprites.Count > 0 || osb.animations.Count > 0))
                yield return (null, [.. osb.sprites, .. osb.animations]);
        }

        /// <summary>
        ///     Returns the first few places as a readable list, with the amount of other places at the end. A sprite is
        ///     only listed once, as are sprites with the same image and time.
        /// </summary>
        public static string Describe(
            IReadOnlyCollection<(Sprite Element, double? Time)> places,
            int limit = 3
        )
        {
            var sprites = places
                .DistinctBy(place => place.Element)
                .DistinctBy(place => (place.Element.path, place.Time))
                .ToList();

            var text = string.Join(
                ", ",
                sprites
                    .Take(limit)
                    .Select(place =>
                        $"\"{place.Element.path}\""
                        + (
                            place.Time is { } time
                                ? $" at {Timestamp.Get(time).TrimEnd(' ', '-')}"
                                : ""
                        )
                    )
            );

            return sprites.Count > limit ? $"{text} and {sprites.Count - limit} more" : text;
        }

        /// <summary> Returns the commands that end before they start. </summary>
        public static IEnumerable<StoryboardCommand> GetBackwardsCommands(Sprite element) =>
            element
                .GetCommandGroups()
                .SelectMany(group => group)
                .Where(command => command.EndTime < command.StartTime);

        /// <summary>
        ///     Returns the commands that start while another command of the same type is still in progress. Does not
        ///     include what triggers do at the same time, since when that happens depends on gameplay.
        /// </summary>
        /// <param name="element"> The sprite or animation to look through. </param>
        /// <param name="tolerance"> How long commands can overlap before they are included, in ms. </param>
        public static IEnumerable<StoryboardCommand> GetOverlappingCommands(
            Sprite element,
            double tolerance
        )
        {
            // Loops play alongside the other commands, so these share the same timeline.
            var timeline = element
                .commands.Concat(element.commandLoops.SelectMany(ExpandLoop))
                .ToList();

            return element
                .triggers.Select(trigger => trigger.commands)
                .Append(timeline)
                .SelectMany(commands => GetOverlaps(commands, tolerance));
        }

        private static IEnumerable<StoryboardCommand> ExpandLoop(StoryboardLoop loop)
        {
            var duration = loop.GetIterationDuration();

            for (var i = 0; i < Math.Min(loop.count, MaxLoopRepetitions); ++i)
                foreach (var command in loop.commands)
                    yield return command with
                    {
                        StartTime = loop.startTime + duration * i + command.StartTime,
                        EndTime = loop.startTime + duration * i + command.EndTime,
                    };
        }

        private static IEnumerable<StoryboardCommand> GetOverlaps(
            IEnumerable<StoryboardCommand> commands,
            double tolerance
        )
        {
            // Flipping and additive blending is either on or off, so those don't conflict with themselves.
            var types = commands
                .Where(command =>
                    command.Type != StoryboardCommand.Kind.Parameter
                    && command.EndTime >= command.StartTime
                )
                .GroupBy(command => command.Type);

            foreach (var type in types)
            {
                StoryboardCommand? latest = null;

                foreach (
                    var command in type.OrderBy(command => command.StartTime)
                        .ThenBy(command => command.EndTime)
                )
                {
                    // An instant command at the edge of another just sets a value there.
                    if (
                        latest is { } other
                        && other.EndTime - command.StartTime > tolerance
                        && (command.Duration > 0 || command.StartTime > other.StartTime)
                    )
                        yield return command;

                    if (latest == null || command.EndTime >= latest.Value.EndTime)
                        latest = command;
                }
            }
        }

        /// <summary>
        ///     Returns the first command of each unbroken run where at least the given amount of commands start less than
        ///     the given time after the previous one of the same type.
        /// </summary>
        public static IEnumerable<StoryboardCommand> GetDenseRuns(
            Sprite element,
            double minInterval,
            int minCommands
        )
        {
            foreach (var group in element.GetCommandGroups())
            foreach (var type in group.GroupBy(command => command.Type))
            {
                StoryboardCommand? first = null;
                double? previous = null;
                var count = 0;

                foreach (var command in type.OrderBy(command => command.StartTime))
                {
                    // Commands at the exact same time set up or conflict with each other, rather than being a smoother
                    // transition than needed, so these neither extend nor end a run.
                    if (command.StartTime == previous)
                        continue;

                    if (first == null || command.StartTime - previous >= minInterval)
                    {
                        first = command;
                        count = 0;
                    }
                    else if (++count == minCommands)
                        yield return first.Value;

                    previous = command.StartTime;
                }
            }
        }

        /// <summary> Returns the horizontal moves that also have a vertical move with the same timing and easing. </summary>
        public static IEnumerable<StoryboardCommand> GetSplitMoves(Sprite element)
        {
            foreach (var group in element.GetCommandGroups())
            {
                var vertical = group
                    .Where(command => command.Type == StoryboardCommand.Kind.MoveY)
                    .Select(command => (command.StartTime, command.EndTime, command.Easing))
                    .ToHashSet();

                foreach (var command in group)
                    if (
                        command.Type == StoryboardCommand.Kind.MoveX
                        && vertical.Contains((command.StartTime, command.EndTime, command.Easing))
                    )
                        yield return command;
            }
        }

        /// <summary>
        ///     Returns the first command of each series that repeats itself with a constant time in between, along with how
        ///     many commands the series has and how many would be left if it was a loop.
        /// </summary>
        public static IEnumerable<(
            StoryboardCommand Command,
            int Repeats,
            int Saved
        )> GetRepeatedCommands(Sprite element, int minRepeats)
        {
            foreach (var type in element.commands.GroupBy(command => command.Type))
            {
                var commands = type.OrderBy(command => command.StartTime).ToList();
                var i = 0;

                while (i < commands.Count)
                {
                    var (length, repeats) = GetRepeats(commands, i);

                    if (repeats < Math.Max(2, minRepeats))
                    {
                        ++i;

                        continue;
                    }

                    // What remains is the loop itself and one series, and possibly a command to fill up the gap until
                    // the next one.
                    var last = commands[i + length - 1];
                    var filler = commands[i + length].StartTime > last.EndTime ? 1 : 0;

                    yield return (commands[i], repeats, length * (repeats - 1) - 1 - filler);

                    i += length * repeats;
                }
            }
        }

        /// <summary> Returns the pattern starting at the index that covers the most commands when repeated. </summary>
        private static (int Length, int Repeats) GetRepeats(
            List<StoryboardCommand> commands,
            int index
        )
        {
            var best = (Length: 1, Repeats: 1);

            for (var length = 1; length <= MaxRepeatPattern; ++length)
            {
                if (index + length >= commands.Count)
                    break;

                var period = commands[index + length].StartTime - commands[index].StartTime;

                // The repetitions of a loop cannot overlap.
                if (
                    period <= 0
                    || commands[index + length - 1].EndTime > commands[index + length].StartTime
                )
                    continue;

                var matches = 0;

                while (
                    index + matches + length < commands.Count
                    && IsRepeat(
                        commands[index + matches],
                        commands[index + matches + length],
                        period
                    )
                )
                    ++matches;

                var repeats = matches / length + 1;

                if (repeats > 1 && length * repeats > best.Length * best.Repeats)
                    best = (length, repeats);
            }

            return best;
        }

        private static bool IsRepeat(
            StoryboardCommand first,
            StoryboardCommand second,
            double period
        ) =>
            second
            == first with
            {
                StartTime = first.StartTime + period,
                EndTime = first.EndTime + period,
            };

        /// <summary>
        ///     Returns whether no part of an image of the given size can be seen at any time with the commands of this
        ///     sprite. Errs on the side of being visible.
        /// </summary>
        public static bool IsNeverOnScreen(Sprite element, int width, int height)
        {
            var timed = element
                .commands.Concat(element.commandLoops.SelectMany(loop => loop.commands))
                .ToList();

            if (timed.Count == 0)
                return false;

            var all = element.GetCommandGroups().SelectMany(group => group).ToList();

            var (left, right) = GetPosition(
                timed,
                all,
                StoryboardCommand.Kind.MoveX,
                value => value.X,
                element.offset.X
            );
            var (top, bottom) = GetPosition(
                timed,
                all,
                StoryboardCommand.Kind.MoveY,
                value => value.Y,
                element.offset.Y
            );

            var scale = GetMaxScale(all, StoryboardCommand.Kind.Scale, value => value.X);
            var scaledWidth =
                width * scale * GetMaxScale(all, StoryboardCommand.Kind.VectorScale, v => v.X);
            var scaledHeight =
                height * scale * GetMaxScale(all, StoryboardCommand.Kind.VectorScale, v => v.Y);

            var isMirroredOrRotated = all.Any(command =>
                command.Type is StoryboardCommand.Kind.Rotate
                || command is { Type: StoryboardCommand.Kind.Parameter, Parameter: 'H' or 'V' }
                || command.Type
                    is StoryboardCommand.Kind.Scale
                        or StoryboardCommand.Kind.VectorScale
                    && (
                        Math.Min(command.StartValue.X, command.StartValue.Y) < 0
                        || Math.Min(command.EndValue.X, command.EndValue.Y) < 0
                    )
            );

            if (!isMirroredOrRotated && GetAnchor(element.origin) is { } anchor)
            {
                left -= scaledWidth * anchor.X;
                right += scaledWidth * (1 - anchor.X);
                top -= scaledHeight * anchor.Y;
                bottom += scaledHeight * (1 - anchor.Y);
            }
            else
            {
                // The image can reach this far in any direction.
                var reach = new Vector2(scaledWidth, scaledHeight).Length();

                left -= reach;
                right += reach;
                top -= reach;
                bottom += reach;
            }

            return right < ScreenLeft
                || left > ScreenRight
                || bottom < ScreenTop
                || top > ScreenBottom;
        }

        private static (float Min, float Max) GetPosition(
            List<StoryboardCommand> timed,
            List<StoryboardCommand> all,
            StoryboardCommand.Kind axis,
            Func<Vector3, float> getValue,
            float initial
        )
        {
            // Commands for a single axis have their only value as X.
            var ranges = all.Where(command =>
                    command.Type == axis || command.Type == StoryboardCommand.Kind.Move
                )
                .Select(command =>
                    command.Type == axis
                        ? GetRange(command, value => value.X)
                        : GetRange(command, getValue)
                )
                .ToList();

            // The initial position only matters as long as nothing else has set it.
            if (
                !timed.Any(command =>
                    command.Type == axis || command.Type == StoryboardCommand.Kind.Move
                )
            )
                ranges.Add((initial, initial));

            return (ranges.Min(range => range.Min), ranges.Max(range => range.Max));
        }

        private static float GetMaxScale(
            List<StoryboardCommand> all,
            StoryboardCommand.Kind kind,
            Func<Vector3, float> getValue
        )
        {
            var ranges = all.Where(command => command.Type == kind)
                .Select(command => GetRange(command, getValue))
                .ToList();

            return ranges.Count == 0
                ? 1
                : ranges.Max(range => Math.Max(Math.Abs(range.Min), Math.Abs(range.Max)));
        }

        private static (float Min, float Max) GetRange(
            StoryboardCommand command,
            Func<Vector3, float> getValue
        )
        {
            var start = getValue(command.StartValue);
            var end = getValue(command.EndValue);

            // Elastic and back easings go past their values.
            var overshoot = command.Easing is >= 24 and <= 31 ? Math.Abs(end - start) : 0;

            return (Math.Min(start, end) - overshoot, Math.Max(start, end) + overshoot);
        }

        /// <summary> Returns where the origin is within the image, from 0 to 1, or null if that isn't known. </summary>
        private static Vector2? GetAnchor(Sprite.Origin origin) =>
            origin switch
            {
                Sprite.Origin.TopLeft => new Vector2(0, 0),
                Sprite.Origin.TopCentre => new Vector2(0.5f, 0),
                Sprite.Origin.TopRight => new Vector2(1, 0),
                Sprite.Origin.CentreLeft => new Vector2(0, 0.5f),
                Sprite.Origin.Centre => new Vector2(0.5f, 0.5f),
                Sprite.Origin.CentreRight => new Vector2(1, 0.5f),
                Sprite.Origin.BottomLeft => new Vector2(0, 1),
                Sprite.Origin.BottomCentre => new Vector2(0.5f, 1),
                Sprite.Origin.BottomRight => new Vector2(1, 1),
                _ => null,
            };

        /// <summary> Returns whether the sprite has triggers of which none leave it invisible again. </summary>
        public static bool StaysVisibleAfterTrigger(Sprite element)
        {
            var triggers = element.triggers.Where(trigger => trigger.commands.Count > 0).ToList();

            if (triggers.Count == 0)
                return false;

            // One trigger can also be what hides the sprite after another, e.g. failing after passing.
            return !triggers.Any(trigger => IsHiddenAfter(trigger.commands))
                && triggers.Any(trigger => !IsHiddenByOwnCommands(element, trigger));
        }

        /// <summary> Returns whether the commands directly on the sprite hide what the trigger may have shown. </summary>
        private static bool IsHiddenByOwnCommands(Sprite element, StoryboardTrigger trigger)
        {
            if (!IsHiddenAfter(element.commands))
                return false;

            // A trigger that doesn't fade leaves the sprite as hidden as it was.
            if (trigger.commands.All(command => command.Type != StoryboardCommand.Kind.Fade))
                return true;

            return trigger.endTime == null
                || element.commands.Any(command =>
                    command.Type == StoryboardCommand.Kind.Fade
                    && command.EndTime >= trigger.endTime
                );
        }

        private static bool IsHiddenAfter(List<StoryboardCommand> commands)
        {
            foreach (
                var kind in new[]
                {
                    StoryboardCommand.Kind.Fade,
                    StoryboardCommand.Kind.Scale,
                    StoryboardCommand.Kind.VectorScale,
                }
            )
            {
                var ofKind = commands.Where(command => command.Type == kind).ToList();

                if (ofKind.Count == 0)
                    continue;

                var value = ofKind
                    .OrderBy(command => command.EndTime)
                    .ThenBy(command => command.StartTime)
                    .Last()
                    .EndValue;

                if (value.X == 0 || kind == StoryboardCommand.Kind.VectorScale && value.Y == 0)
                    return true;
            }

            return false;
        }

        /// <summary>
        ///     Returns whether the trigger can never happen, given the time ranges in which the difficulties using the
        ///     storyboard have objects.
        /// </summary>
        public static bool IsImpossibleTrigger(
            StoryboardTrigger trigger,
            IReadOnlyCollection<Beatmap> beatmaps
        )
        {
            if (trigger.endTime < trigger.startTime)
                return true;

            if (trigger.name is "Passing" or "Failing")
                return false;

            if (!HitSoundTrigger().IsMatch(trigger.name))
                return true;

            return beatmaps.All(beatmap =>
                !beatmap.HitObjects.Any(hitObject =>
                    !(hitObject.GetEndTime() < trigger.startTime)
                    && !(hitObject.time > trigger.endTime)
                )
            );
        }

        // HitSound[sample set][addition sample set][addition][custom sample index]
        [GeneratedRegex(
            "^HitSound(All|Normal|Soft|Drum)?(All|Normal|Soft|Drum)?(Whistle|Finish|Clap)?[0-9]*$",
            RegexOptions.CultureInvariant
        )]
        private static partial Regex HitSoundTrigger();
    }
}
