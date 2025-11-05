using System.Numerics;

namespace MapsetVerifier.Parser.Objects.HitObjects.Catch;

public interface ICatchHitObject
{
    // TODO need some better way to reference the original hitobject, this would need a full rework of the class structure
    HitObject Original { get; }
    /// <summary>Start time of this catch object (same as underlying hit object's time).</summary>
    double Time { get; }
    Vector2 Position { get; }
    /// <summary>Amount of distance margin until a hyperdash is required. Negative implies hyperdash.</summary>
    float DistanceToHyper { get; set; }
    /// <summary>Amount of distance margin until a dash is required. Negative implies dash.</summary>
    float DistanceToDash { get; set; }
    /// <summary>Time difference to target object in milliseconds (after grace subtraction).</summary>
    double TimeToTarget { get; set; }
    /// <summary>The target (next) catch object in sequence.</summary>
    ICatchHitObject? Target { get; set; }
    /// <summary>Whether this movement qualifies as an edge movement (based on bpm scaling heuristics).</summary>
    bool IsEdgeMovement { get; set; }
    /// <summary>Movement classification required to reach the next object (Walk/Dash/Hyperdash).</summary>
    CatchMovementType MovementType { get; set; }
    /// <summary>Horizontal direction needed to catch the next object.</summary>
    CatchNoteDirection NoteDirection { get; set; }
    /// <summary>True if this object is part of a slider (JuiceStream or its parts).</summary>
    public bool IsSlider => this is JuiceStream || this is JuiceStream.JuiceStreamPart;
    /// <summary>Convenience: true if movement type is hyperdash.</summary>
    public bool IsHyperdash => MovementType == CatchMovementType.Hyperdash;
    /// <summary>Convenience: true if movement type is dash (but not hyperdash).</summary>
    public bool IsDash => MovementType == CatchMovementType.Dash;
    /// <summary>Display name used in RC / user-facing messages (Fruit, Slider head, Slider repeat, Slider tail, Droplet, Spinner).</summary>
    string GetNoteTypeName();
}
