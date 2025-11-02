namespace MapsetVerifier.Parser.Objects.TimingLines
{
    public class InheritedLine : TimingLine
    {
        // This is the same as the base class, just a different name.
        public InheritedLine(string[] args, Beatmap beatmap) : base(args, beatmap) { }
    }
}