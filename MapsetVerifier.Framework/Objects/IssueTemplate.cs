using System;
using System.Collections.Generic;
using System.Linq;

namespace MapsetVerifier.Framework.Objects
{
    public class IssueTemplate
    {
        private readonly object[] defaultArguments;

        private readonly string format;

        public string? Cause { get; private set; }

        /// <summary>
        ///     Constructs a new issue template with the given issue level, format and default arguments.
        /// </summary>
        /// <param name="level">The type and priority of the issue (e.g. minor/warning/unrankable).</param>
        /// <param name="format">The formatting string, use {0}, {1}, etc to insert arguments.</param>
        /// <param name="defaultArguments">
        ///     The default arguments for the format string, supply as many of these as you have {0},
        ///     {1}, etc.
        /// </param>
        public IssueTemplate(Issue.Level level, string format, params object[] defaultArguments)
        {
            Level = level;

            this.format = format;
            this.defaultArguments = defaultArguments;

            for (var i = 0; i < defaultArguments.Length; ++i)
                if (!format.Contains("{" + i + "}"))
                    throw new ArgumentException($"\"{format}\" There are {defaultArguments.Length} default arguments given, but the format string does not contain any \"{{{i}}}\", which makes the latter one(s) useless. Ensure there are an equal amount of {{0}}, {{1}}, etc as there are default arguments.");

            if (format.Contains("{" + defaultArguments.Length + "}"))
                throw new ArgumentException($"\"{format}\" There are {defaultArguments.Length} default arguments given, but the format string contains an unused argument place, \"{{{defaultArguments.Length}}}\". Ensure there are an equal amount of {{0}}, {{1}}, etc as there are default arguments.");

            Cause = null;
        }

        public Issue.Level Level { get; }

        /// <summary> Returns the template with a given cause, which will be shown below the issue template in the documentation. </summary>
        public IssueTemplate WithCause(string cause)
        {
            Cause = cause;

            return this;
        }

        /// <summary> Returns the format with {0}, {1}, etc. replaced with the respective given arguments. </summary>
        public string Format(object?[] arguments)
        {
            if (arguments.Length != defaultArguments.Length)
                throw new ArgumentException($"The format for a template is \"{format}\", which takes {defaultArguments.Length} arguments (according to the default argument amount), but was given the unexpected argument amount {arguments.Length}. Make sure that, when creating a new issue, you supply it with the correct amount of arguments for its template.");

            // Trimming format and args separately allows for "timestamp - " in "{0} /.../" without double spacing.
            // This way we still maintain any double space causing things like incorrect filenames within arguments.
            return string.Format(format.Trim(), arguments.Select(arg => arg?.ToString()?.Trim()).ToArray<object?>());
        }

        /// <summary> Returns the default arguments for this template. </summary>
        public IEnumerable<object> GetDefaultArguments() => defaultArguments;

        public override string ToString() => Format(defaultArguments);
    }
}