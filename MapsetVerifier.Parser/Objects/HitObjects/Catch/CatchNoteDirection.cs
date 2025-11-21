namespace MapsetVerifier.Parser.Objects.HitObjects.Catch;

/// <summary>
/// Horizontal direction required to move from one object to the next.
/// </summary>
public enum CatchNoteDirection
{
    /// <summary>
    /// Representing a standstill.
    /// </summary>
    None,
    
    /// <summary>
    /// A movement to the left is required to catch the next fruit.
    /// </summary>
    Left,
    
    /// <summary>
    /// A movement to the right is required to catch the next fruit.
    /// </summary>
    Right
}

