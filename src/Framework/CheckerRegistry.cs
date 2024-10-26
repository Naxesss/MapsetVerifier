using System.Collections.Generic;
using System.Linq;
using MapsetVerifier.Framework.Objects;

namespace MapsetVerifier.Framework
{
    public static class CheckerRegistry
    {
        private static readonly List<Check> checks = new();

        /// <summary> Adds the given check to the list of checks to process when checking for issues. </summary>
        public static void RegisterCheck(Check check)
        {
            if (check == null)
                return;

            checks.Add(check);
        }

        /// <summary> Returns all checks which are processed when checking for issues. </summary>
        public static List<Check> GetChecks() => new(checks);

        /// <summary>
        ///     Returns checks which are processed beatmap-wise when checking for issues.
        ///     These are isolated from the set for optimization purposes.
        /// </summary>
        public static IEnumerable<BeatmapCheck> GetBeatmapChecks() => checks.OfType<BeatmapCheck>();

        /// <summary>
        ///     Returns checks which are processed beatmapset-wise when checking for issues.
        ///     These are often checks which need to compare between difficulties in a set.
        /// </summary>
        public static IEnumerable<BeatmapSetCheck> GetBeatmapSetChecks() => checks.OfType<BeatmapSetCheck>();

        /// <summary>
        ///     Returns checks which are processed beatmapset-wise when checking for issues and stored in a seperate difficulty.
        ///     These are general checks which are independent from any specific difficulty, for example checking files.
        /// </summary>
        public static IEnumerable<GeneralCheck> GetGeneralChecks() => checks.OfType<GeneralCheck>();
    }
}