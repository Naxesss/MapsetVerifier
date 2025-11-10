namespace MapsetVerifier.Parser.Objects.HitObjects.Catch;

public class JuiceStream(string[] args, Beatmap beatmap, Slider original) : Slider(args, beatmap), ICatchHitObject
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
    public bool IsSlider => true;
    public bool IsHyperdash => MovementType == CatchMovementType.Hyperdash;
    public bool IsDash => MovementType == CatchMovementType.Dash;
    public string GetNoteTypeName() => "Slider head";

    /// <summary>All parts belonging to this juice stream (head, repeats, tail, droplets).</summary>
    public List<JuiceStreamPart> Parts { get; } = [];

    /// <summary>Represents a slider component (repeat, tail, droplet) for catch movement.</summary>
    public class JuiceStreamPart : HitObject, ICatchHitObject
    {
        public enum PartKind { Repeat, Tail, Droplet }
        public JuiceStreamPart(string[] args, Beatmap beatmap, HitObject original, PartKind kind) : base(args, beatmap)
        {
            Original = original;
            Kind = kind;
        }

        public HitObject Original { get; }
        public PartKind Kind { get; }
        public double Time => time;
        public float DistanceToHyper { get; set; }
        public float DistanceToDash { get; set; }
        public double TimeToTarget { get; set; }
        public ICatchHitObject? Target { get; set; } = null;
        public bool IsEdgeMovement { get; set; }
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