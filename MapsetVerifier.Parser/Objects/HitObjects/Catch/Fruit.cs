namespace MapsetVerifier.Parser.Objects.HitObjects.Catch;

public class Fruit(string[] args, Beatmap beatmap) : Circle(args, beatmap), ICatchHitObject
{
    public double Time => time;
    public float DistanceToHyper { get; set; } = float.PositiveInfinity;
    public CatchMovementType MovementType { get; set; }
    public CatchNoteDirection NoteDirection { get; set; }
    public string GetNoteTypeName() => "Fruit";
}
