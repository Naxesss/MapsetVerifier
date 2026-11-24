using MapsetVerifier.Parser.Exceptions;

namespace MapsetVerifier.Parser.Statics
{
    public static class ParserStatic
    {
        /// <summary> Yields the result of the given function for each line in this section. </summary>
        public static IEnumerable<T> ParseSection<T>(
            string[] lines,
            string section,
            Func<string, T> func
        )
        {
            // Find the section, always from a line starting with [ and ending with ]
            // then ending on either end of file or an empty line.
            var read = false;
            var sectionHeader = "[" + section + "]";

            foreach (var line in lines)
            {
                if (line.StartsWith('['))
                    read = false;

                if (read && !string.IsNullOrWhiteSpace(line))
                    yield return Read(section, line.Replace("\r", ""), func);

                // "[[TimingLines]]]]" works. Anything that doesn't work will be very obvious (map corrupted warnings etc).
                if (line.Contains(sectionHeader))
                    read = true;
            }
        }

        /// <summary>
        ///     Returns all the lines in this section ran through the given function, excluding
        ///     the section identifier (e.g. [HitObjects]).
        /// </summary>
        public static T GetSettings<T>(string[] lines, string section, Func<string[], T> func)
        {
            var parsedLines = ParseSection(lines, section, line => line).ToArray();

            return Read(section, parsedLines, func);
        }

        /// <summary> Returns the result of the given function, saying what could not be read in case it fails. </summary>
        public static T Read<T>(string section, string line, Func<string, T> func)
        {
            const int maxLength = 100;

            try
            {
                return func(line);
            }
            catch (Exception exception) when (IsUnreadable(exception))
            {
                var shownLine = line.Length > maxLength ? line[..maxLength] + "..." : line;

                throw new InvalidBeatmapDataException(
                    $"Could not read \"{shownLine}\" in [{section}].",
                    exception
                );
            }
        }

        private static T Read<T>(string section, string[] lines, Func<string[], T> func)
        {
            try
            {
                return func(lines);
            }
            catch (Exception exception) when (IsUnreadable(exception))
            {
                throw new InvalidBeatmapDataException(
                    $"Could not read the settings in [{section}]. {exception.Message}",
                    exception
                );
            }
        }

        /// <summary> Whether an exception means that a value is missing or isn't what it should be. </summary>
        private static bool IsUnreadable(Exception exception) =>
            exception
                is FormatException
                    or OverflowException
                    or IndexOutOfRangeException
                    or ArgumentOutOfRangeException;

        /// <summary> Same as <see cref="GetSettings{T}" /> except does not return. </summary>
        public static void ApplySettings(string[] lines, string section, Action<string[]> action)
        {
            var parsedLines = ParseSection(lines, section, line => line);

            action(parsedLines.ToArray());
        }

        /// <summary> Returns the value of the first "key: value" line with exactly this key, if any, otherwise null. </summary>
        public static string? GetValue(IEnumerable<string> lines, string key)
        {
            foreach (var line in lines)
            {
                var separatorIndex = line.IndexOf(':');

                if (separatorIndex >= 0 && line.AsSpan(0, separatorIndex).Trim().SequenceEqual(key))
                    return line[(separatorIndex + 1)..].Trim();
            }

            return null;
        }

        /// <summary>
        ///     Returns the first enum which has the same name as the given string,
        ///     or null if none match.
        /// </summary>
        public static T? GetEnumMatch<T>(string str)
            where T : struct, Enum
        {
            foreach (T @enum in Enum.GetValues(typeof(T)))
            {
                var enumName = Enum.GetName(typeof(T), @enum);

                if (enumName == str)
                    return @enum;
            }

            return default;
        }
    }
}
