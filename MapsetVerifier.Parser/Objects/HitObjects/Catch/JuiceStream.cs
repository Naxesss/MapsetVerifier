namespace MapsetVerifier.Parser.Objects.HitObjects.Catch;

public class JuiceStream(string[] args, Beatmap beatmap, Slider original) : Slider(args, beatmap), ICatchHitObject
{
    public HitObject Original { get; } = original;
    public double Time => time;
    public float DistanceToHyper { get; set; } = float.PositiveInfinity;
    public ICatchHitObject? Target { get; set; } = null;
    public CatchMovementType MovementType { get; set; }
    public CatchNoteDirection NoteDirection { get; set; }
    public string GetNoteTypeName() => "Slider head";

    /// <summary>All parts belonging to this juice stream (head, repeats, tail, droplets).</summary>
    public List<JuiceStreamPart> Parts { get; } = [];

    /// <summary>Represents a slider component (repeat, tail, droplet) for catch movement.</summary>
    public class JuiceStreamPart(string[] args, Beatmap beatmap, HitObject original, JuiceStreamPart.PartKind kind)
        : HitObject(args, beatmap), ICatchHitObject
    {
        public enum PartKind { Repeat, Tail, Droplet }

        public HitObject Original { get; } = original;
        public PartKind Kind { get; } = kind;
        public double Time => time;
        public float DistanceToHyper { get; set; } = float.PositiveInfinity;
        public ICatchHitObject? Target { get; set; } = null;
        public CatchMovementType MovementType { get; set; }
        public CatchNoteDirection NoteDirection { get; set; }
        public string GetNoteTypeName() => Kind switch
        {
            PartKind.Repeat => "Slider repeat",
            PartKind.Tail => "Slider tail",
            PartKind.Droplet => "Droplet",
            _ => "Fruit"
        };
    }
}