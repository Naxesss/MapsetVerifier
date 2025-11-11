using System.Numerics;

namespace MapsetVerifier.Parser.Objects.HitObjects.Catch;

public interface ICatchHitObject
{
    /// <summary>
    /// Start time of this catch object (same as underlying hit object's time).
    /// </summary>
    double Time { get; }
    
    Vector2 Position { get; }
    
    /// <summary>
    /// Amount of distance margin until a hyperdash is required. Negative implies hyperdash.
    /// </summary>
    float DistanceToHyper { get; set; }
    
    /// <summary>
    /// Movement classification required to reach the next object (Walk/Dash/Hyperdash).
    /// </summary>
    CatchMovementType MovementType { get; set; }
    
    /// <summary>
    /// Horizontal direction needed to catch the next object.
    /// </summary>
    CatchNoteDirection NoteDirection { get; set; }
    
    /// <summary>
    /// Display name used in RC / user-facing messages (Fruit, Slider head, Slider repeat, Slider tail, Droplet, Spinner).
    /// </summary>
    string GetNoteTypeName();
}
