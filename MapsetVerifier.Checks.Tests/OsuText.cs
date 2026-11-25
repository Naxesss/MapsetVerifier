namespace MapsetVerifier.Checks.Tests;

/// <summary> Adds lines to an already built <c>.osu</c> file, for settings without a builder method. </summary>
internal static class OsuText
{
    public static string General(string code, params string[] lines) =>
        code.Replace("[Metadata]", string.Join("\n", [.. lines, "[Metadata]"]));
}
