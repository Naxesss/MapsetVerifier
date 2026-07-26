namespace MapsetVerifier.Framework;

/// <summary> How long a single check invocation took. Difficulty is null for general/beatmapset-wide checks. </summary>
public readonly record struct CheckTiming(string CheckName, string? Difficulty, long ElapsedMs);
