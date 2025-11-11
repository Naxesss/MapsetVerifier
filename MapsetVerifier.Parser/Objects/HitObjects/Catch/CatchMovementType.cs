namespace MapsetVerifier.Parser.Objects.HitObjects.Catch;

/// <summary>
/// Movement requirement between sequential catch objects.
/// </summary>
public enum CatchMovementType
{
    Walk,
    // TODO add support for dashes, they are hard to detect reliably
    // Dash,
    Hyperdash
}

