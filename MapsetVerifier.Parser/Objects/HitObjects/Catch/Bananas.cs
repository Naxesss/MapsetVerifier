namespace MapsetVerifier.Parser.Objects.HitObjects.Catch;

/// <summary>Spinner object translated to bananas in osu!catch.</summary>
public class Bananas(string[] args, Beatmap beatmap, Spinner original) : Spinner(args, beatmap), ICatchHitObject
{
    public HitObject Original { get; } = original;
    public double Time => time;
    public float DistanceToHyper { get; set; } = float.PositiveInfinity;
    public ICatchHitObject? Target { get; set; } = null;
    public CatchMovementType MovementType { get; set; }
    public CatchNoteDirection NoteDirection { get; set; }
    public string GetNoteTypeName() => "Spinner";
}
