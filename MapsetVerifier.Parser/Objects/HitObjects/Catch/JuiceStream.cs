using System.Globalization;

namespace MapsetVerifier.Parser.Objects.HitObjects.Catch;

public class JuiceStream : Slider, ICatchHitObject
{
    public JuiceStream(string[] args, Beatmap beatmap) : base(args, beatmap)
    {
        // Determine all parts of the slider as they can all have hyperdashes while not representing a line in the osu file
        var parts = new List<JuiceStreamPart>();

        // Repeats & tail
        var edgeTimes = GetEdgeTimes().ToList();
        for (var i = 0; i < edgeTimes.Count; i++)
        {
            var edgeTime = edgeTimes[i];
            var partKind = i + 1 == edgeTimes.Count ? JuiceStreamPart.PartKind.Tail : JuiceStreamPart.PartKind.Repeat;
            parts.Add(CreateJuiceStreamPart(edgeTime, this, partKind));
        }

        // Droplets
        foreach (var tickTime in SliderTickTimes)
            parts.Add(CreateJuiceStreamPart(tickTime, this, JuiceStreamPart.PartKind.Droplet));

        var sortedParts = parts.OrderBy(part => part.Time).ToList();
        Parts.AddRange(sortedParts);
    }
    
    public double Time => time;
    public float DistanceToHyper { get; set; } = float.PositiveInfinity;
    public ICatchHitObject? Target { get; set; } = null;
    public CatchMovementType MovementType { get; set; }
    public CatchNoteDirection NoteDirection { get; set; }
    public string GetNoteTypeName() => "Slider head";

    private JuiceStreamPart CreateJuiceStreamPart(double edgeTime, JuiceStream juiceStream, JuiceStreamPart.PartKind kind)
    {
        // Make sure we make a copy to not modify the original juice stream code
        var objectCodeCopy = (string) code.Clone();
        var codeClone =objectCodeCopy.Split(',');
        codeClone[0] = GetPathPosition(edgeTime).X.ToString(CultureInfo.InvariantCulture);
        codeClone[2] = edgeTime.ToString(CultureInfo.InvariantCulture);
        return new JuiceStreamPart(codeClone, beatmap, juiceStream, kind);
    }

    /// <summary>All parts belonging to this juice stream (head, repeats, tail, droplets).</summary>
    public List<JuiceStreamPart> Parts { get; } = [];

    /// <summary>Represents a slider component (repeat, tail, droplet) for catch movement.</summary>
    public class JuiceStreamPart(string[] args, Beatmap beatmap, JuiceStream parent, JuiceStreamPart.PartKind kind)
        : HitObject(args, beatmap), ICatchHitObject
    {
        public enum PartKind { Repeat, Tail, Droplet }

        public readonly JuiceStream Parent = parent;
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