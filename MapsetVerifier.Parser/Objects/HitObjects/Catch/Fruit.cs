namespace MapsetVerifier.Parser.Objects.HitObjects.Catch;

public class Fruit(string[] args, Beatmap beatmap, HitObject original) : Circle(args, beatmap), ICatchHitObject
{
    public HitObject Original { get; } = original;
    public double Time => time;
    public float DistanceToHyper { get; set; }
    public float DistanceToDash { get; set; }
    public double TimeToTarget { get; set; }
    public ICatchHitObject? Target { get; set; } = null;
    public bool IsEdgeMovement { get; set; }
    public CatchMovementType MovementType { get; set; }
    public CatchNoteDirection NoteDirection { get; set; }
    public string GetNoteTypeName() => "Fruit";
}
