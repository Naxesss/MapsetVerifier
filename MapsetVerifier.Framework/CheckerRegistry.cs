using MapsetVerifier.Framework.Objects;
using Serilog;

namespace MapsetVerifier.Framework
{
    public static class CheckerRegistry
    {
        private static readonly List<Check> Checks = [];

        /// <summary> Adds the given check to the list of checks to process when checking for issues. </summary>
        public static void RegisterCheck(Check? check)
        {
            if (check == null)
                return;

            Log.Information("Registering check {check}", check.GetType().ToString());

            Checks.Add(check);
        }

        /// <summary> 
        /// Returns all checks as a dictionary where the key is the index and the value is the check itself.
        /// </summary>
        public static Dictionary<int, Check> GetChecksWithId() =>
            Checks.Select((check, index) => new { check, index })
                  .ToDictionary(x => x.index, x => x.check);

        /// <summary>
        /// Returns all checks which are processed when checking for issues.
        /// </summary>
        public static List<Check> GetChecks() => [..Checks];

        /// <summary>
        ///     Returns checks which are processed beatmap-wise when checking for issues.
        ///     These are isolated from the set for optimization purposes.
        /// </summary>
        public static IEnumerable<BeatmapCheck> GetBeatmapChecks() => Checks.OfType<BeatmapCheck>();

        /// <summary>
        ///     Returns checks which are processed beatmapset-wise when checking for issues.
        ///     These are often checks which need to compare between difficulties in a set.
        /// </summary>
        public static IEnumerable<BeatmapSetCheck> GetBeatmapSetChecks() => Checks.OfType<BeatmapSetCheck>();

        /// <summary>
        ///     Returns checks which are processed beatmapset-wise when checking for issues and stored in a seperate difficulty.
        ///     These are general checks which are independent from any specific difficulty, for example checking files.
        /// </summary>
        public static IEnumerable<GeneralCheck> GetGeneralChecks() => Checks.OfType<GeneralCheck>();
    }
}