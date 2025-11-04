
namespace MapsetVerifier.Parser.Objects.HitObjects.Catch;

public sealed class CatchHitObject : HitObject
{
    public CatchHitObject(string[] anArgs, Beatmap beatmap, CatchNoteType type, HitObject original) : base(anArgs, beatmap)
    {
        X = Position.X;
        NoteType = type;
        Original = original;
    }

    /// <summary>The underlying original hit object this catch object was derived from.</summary>
    public HitObject Original { get; }

    /// <summary>
    /// The x coordinate in the osu editor.
    /// </summary>
    public readonly float X;
    
    /// <summary>
    /// Distance margin until hyperdash. Negative implies hyperdash required.
    /// </summary>
    public float DistanceToHyper { get; set; }
    
    /// <summary>
    /// Distance margin until dash. Negative implies dash required.
    /// </summary>
    public float DistanceToDash { get; set; }
    
    /// <summary>
    /// Time between this object and its target in ms (after grace subtraction).
    /// </summary>
    public double TimeToTarget { get; set; }
    
    public CatchHitObject? SliderHead { get; init; }
    
    /// <summary>
    /// The target (next) catch hit object.
    /// </summary>
    public CatchHitObject Target { get; set; } = null!; // Set during distance calculation
    
    public bool IsEdgeMovement { get; set; }
    
    /// <summary>
    /// Movement classification required to reach the next object.
    /// </summary>
    public CatchMovementType MovementType { get; set; }
    
    /// <summary>
    /// Direction required to catch the next object.
    /// </summary>
    public CatchNoteDirection NoteDirection { get; set; }
    
    /// <summary>
    /// The type of the current note (circle, slider part, droplet, spinner).
    /// </summary>
    public CatchNoteType NoteType { get; }

    /// <summary>True if this object originated from a slider (any part except standalone circles/spinners).</summary>
    public bool IsSlider => SliderHead != null;

    /// <summary>Convenience: true if movement type is hyperdash.</summary>
    public bool IsHyperdash => MovementType == CatchMovementType.Hyperdash;

    /// <summary>Convenience: true if movement type is dash (but not hyperdash).</summary>
    public bool IsDash => MovementType == CatchMovementType.Dash;

    /// <summary>
    /// Helper function to get the name used in the RC for this hit object.
    /// </summary>
    public string GetNoteTypeName() => NoteType switch
    {
        CatchNoteType.Circle => "Fruit",
        CatchNoteType.Head => "Slider head",
        CatchNoteType.Repeat => "Slider repeat",
        CatchNoteType.Tail => "Slider tail",
        CatchNoteType.Droplet => "Droplet",
        CatchNoteType.Spinner => "Spinner",
        _ => "NULL"
    };
}