namespace MapsetVerifier.Parser.Difficulty;

/// <summary>
///     Timespan of a single <c>DifficultyHitObject</c> as the ruleset processed it, in map time.
/// </summary>
public readonly record struct DifficultyObjectTiming(double StartTime, double EndTime);
