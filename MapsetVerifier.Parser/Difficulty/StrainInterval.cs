namespace MapsetVerifier.Parser.Difficulty;

/// <summary>
///     A skill's difficulty over one span of map time. Intervals are contiguous and chronological;
///     gaps the skill contributes nothing to (breaks, spinner sections) are present as explicit
///     zero-valued intervals rather than being left out.
/// </summary>
public readonly record struct StrainInterval(double StartTime, double EndTime, double Value)
{
    public double Length => EndTime - StartTime;
}
