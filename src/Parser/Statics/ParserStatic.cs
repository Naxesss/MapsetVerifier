using System;
using System.Collections.Generic;
using System.Linq;

namespace MapsetVerifier.Parser.Statics
{
    public static class ParserStatic
    {
        /// <summary> Yields the result of the given function for each line in this section. </summary>
        public static IEnumerable<T> ParseSection<T>(string[] lines, string section, Func<string, T> func)
        {
            // Find the section, always from a line starting with [ and ending with ]
            // then ending on either end of file or an empty line.
            var read = false;

            foreach (var line in lines)
            {
                if (line.StartsWith("["))
                    read = false;

                if (read && line.Trim().Length != 0)
                    yield return func(line.Replace("\r", ""));

                // "[[TimingLines]]]]" works. Anything that doesn't work will be very obvious (map corrupted warnings etc).
                if (line.Contains("[" + section + "]"))
                    read = true;
            }
        }

        /// <summary>
        ///     Returns all the lines in this section ran through the given function, excluding
        ///     the section identifier (e.g. [HitObjects]).
        /// </summary>
        public static T GetSettings<T>(string[] lines, string section, Func<string[], T> func)
        {
            var parsedLines = ParseSection(lines, section, line => line);

            return func(parsedLines.ToArray());
        }

        /// <summary> Same as <see cref="GetSettings{T}" /> except does not return. </summary>
        public static void ApplySettings(string[] lines, string section, Action<string[]> action)
        {
            var parsedLines = ParseSection(lines, section, line => line);

            action(parsedLines.ToArray());
        }

        /// <summary>
        ///     Returns the first enum which has the same name as the given string,
        ///     or null if none match.
        /// </summary>
        public static T? GetEnumMatch<T>(string str) where T : struct, Enum
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