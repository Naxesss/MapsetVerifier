using System.Collections.Generic;

namespace MapsetVerifier.Framework.Objects.Metadata
{
    public class CheckMetadata
    {
        /// <summary>
        ///     Can be initialized like this:
        ///     <para />
        ///     new CheckMetadata() { Category = "", Message = "", Author = "", ...  }
        /// </summary>
        public CheckMetadata() { }

        /// <summary> The name of the category this check falls under, by default "Other". </summary>
        public string Category { get; set; } = "Other";

        /// <summary>
        ///     A message explaining what went wrong in, preferably, one sentence. By default
        ///     "Custom check returned one or more issues."
        ///     <para>
        ///         "No" is used as prefix in the application if there were no issues, so make sure
        ///         adding "No" in front of the message makes sense.
        ///     </para>
        /// </summary>
        public string Message { get; set; } = "Custom check returned one or more issues.";

        /// <summary> The user(s) who developed the check. By default "Unknown". </summary>
        public string Author { get; set; } = "Unknown";

        /// <summary>
        ///     A list of title-description pairs used to document the intent and reasoning behind the check. By default empty.
        ///     <para>
        ///         Checks should not be followed blindly; if someone doubts that the check is worth enforcing,
        ///         this should convince them it is.
        ///     </para>
        /// </summary>
        public Dictionary<string, string> Documentation { get; set; } = new Dictionary<string, string>();

        public virtual string GetMode() => "General";
    }
}