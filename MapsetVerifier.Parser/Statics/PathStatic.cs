using System;
using System.Linq;

namespace MapsetVerifier.Parser.Statics
{
    public static class PathStatic
    {
        /// <summary>
        ///     Returns the file path in its base form as seen by the game, optionally allowing
        ///     extensions to be stripped or maintaining case.
        /// </summary>
        public static string? ParsePath(string? filePath, bool withoutExtension = false, bool retainCase = false)
        {
            if (filePath == null)
                return null;

            var trimmedPath = filePath.Replace("\"", "").Replace("\\", "/").Trim();

            if (!retainCase)
                trimmedPath = trimmedPath.ToLower();

            if (!withoutExtension)
                return trimmedPath;

            var strippedPath = trimmedPath.LastIndexOf(".", StringComparison.Ordinal) != -1 ? trimmedPath.Substring(0, trimmedPath.LastIndexOf(".", StringComparison.Ordinal)) : trimmedPath;

            return strippedPath;
        }

        /// <summary> Returns the file or folder name rather than its path. Takes the last split of "\\" and "/". </summary>
        public static string CutPath(string filePath) => filePath.Split(new[] { '\\', '/' }).Last();

        /// <summary> Returns the file path relative to another path, usually song path in most cases. </summary>
        public static string RelativePath(string filePath, string songPath) => filePath.Replace("\\", "/").Replace(songPath.Replace("\\", "/") + "/", "");
    }
}